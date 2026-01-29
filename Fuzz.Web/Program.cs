using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Fuzz.Web.Client.Pages;
using Fuzz.Web.Components;
using Fuzz.Web.Components.Account;
using Fuzz.Domain.Data;
using Fuzz.Domain.Entities;
using Fuzz.Domain.Services;
using Fuzz.Domain.Ai;
using Fuzz.Domain.Ai.Tools;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// MudBlazor
builder.Services.AddMudServices();
builder.Services.AddHttpClient();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Configure FuzzDbContext with Factory (Recommended for Blazor)
builder.Services.AddDbContextFactory<FuzzDbContext>(options =>
    options.UseNpgsql(connectionString));

// Also register the Scoped DbContext for services that don't use the factory (like Identity and SeedService)
builder.Services.AddScoped(p => p.GetRequiredService<IDbContextFactory<FuzzDbContext>>().CreateDbContext());

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<FuzzUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<FuzzDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<FuzzUser>, IdentityNoOpEmailSender>();

// Domain Services
builder.Services.AddScoped<IAiTool, SqlAiTool>();
builder.Services.AddScoped<IAiTool, TimeAiTool>();

// AI Services
builder.Services.AddScoped<IAiConfigService, AiConfigService>();
builder.Services.AddKeyedScoped<IFuzzAgentService, GeminiAgentService>(AiProvider.Gemini);
builder.Services.AddKeyedScoped<IFuzzAgentService, OpenAiAgentService>(AiProvider.OpenAI);
builder.Services.AddKeyedScoped<IFuzzAgentService, LocalAgentService>(AiProvider.Local);
builder.Services.AddScoped<IFuzzAgentService, AgentDispatcherService>();

builder.Services.AddScoped<IFuzzSeedService, FuzzSeedService>();

var app = builder.Build();

// Automatic Database Migration & Seeding
await InitializeDatabaseAsync(app);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(Fuzz.Web.Client._Imports).Assembly);

app.MapAdditionalIdentityEndpoints();

app.Run();

// Database Initialization
static async Task InitializeDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var seedService = scope.ServiceProvider.GetRequiredService<IFuzzSeedService>();
    
    await seedService.ApplyMigrationsAsync();
    await seedService.SeedDataAsync();
}
