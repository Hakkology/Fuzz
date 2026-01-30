using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Fuzz.Web.Client.Pages;
using Fuzz.Web.Components;
using Fuzz.Web.Components.Account;
using Fuzz.Domain.Data;
using Fuzz.Domain.Entities;
using Fuzz.Domain.Services;
using MudBlazor.Services;
using Fuzz.Domain.Services.Interfaces;
using Fuzz.Domain.Services.Tools;
using Fuzz.Domain.Services.AI;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMudServices();
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();

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

builder.Services.AddDbContextFactory<FuzzDbContext>(options =>
    options.UseNpgsql(connectionString));
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
builder.Services.AddScoped<IAiTool, TimeAiTool>();
builder.Services.AddScoped<IAiTool, WebScraperAiTool>();
builder.Services.AddScoped<IAiTool, SchemaAiTool>();

// AI Services
builder.Services.AddScoped<IAiConfigService, AiConfigService>();
builder.Services.AddScoped<IAiChatValidationService, AiChatValidationService>();
builder.Services.AddScoped<IFuzzAgentService, AgentDispatcherService>();
builder.Services.AddKeyedScoped<IFuzzAgentService, GeminiAgentService>(AiProvider.Gemini);
builder.Services.AddKeyedScoped<IFuzzAgentService, OpenAiAgentService>(AiProvider.OpenAI);
builder.Services.AddKeyedScoped<IFuzzAgentService, LocalAgentService>(AiProvider.Local);

// Visual AI Services
builder.Services.AddKeyedScoped<IVisualAgentService, GeminiVisualService>(AiProvider.Gemini);
builder.Services.AddKeyedScoped<IVisualAgentService, OpenAiVisualService>(AiProvider.OpenAI);
builder.Services.AddKeyedScoped<IVisualAgentService, LocalVisualService>(AiProvider.Local);
builder.Services.AddScoped<IVisualAgentService, VisualAgentDispatcherService>();

// Sound AI Services
builder.Services.AddKeyedScoped<ISoundAgentService, LocalSoundService>(AiProvider.Local);
builder.Services.AddKeyedScoped<ISoundAgentService, ElevenLabsSoundService>(AiProvider.ElevenLabs);
builder.Services.AddKeyedScoped<ISoundAgentService, ReplicateSoundService>(AiProvider.Replicate);
builder.Services.AddScoped<ISoundAgentService, SoundAgentDispatcherService>();

builder.Services.AddScoped<IFuzzSeedService, FuzzSeedService>();

var app = builder.Build();
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
