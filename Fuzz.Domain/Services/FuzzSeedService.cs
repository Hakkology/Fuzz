using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using Fuzz.Domain.Data;
using Fuzz.Domain.Entities;

namespace Fuzz.Domain.Services;

public interface IFuzzSeedService
{
    Task ApplyMigrationsAsync();
    Task SeedDataAsync();
}

public class FuzzSeedService : IFuzzSeedService
{
    private readonly FuzzDbContext _dbContext;
    private readonly UserManager<FuzzUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<FuzzSeedService> _logger;

    public FuzzSeedService(
        FuzzDbContext dbContext, 
        UserManager<FuzzUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<FuzzSeedService> logger)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task ApplyMigrationsAsync()
    {
        try
        {
            _logger.LogInformation("🔄 Checking database connection...");
            
            var canConnect = await _dbContext.Database.CanConnectAsync();
            
            if (canConnect)
            {
                _logger.LogInformation("✅ PostgreSQL connection successful!");
                
                var pendingMigrations = await _dbContext.Database.GetPendingMigrationsAsync();
                
                if (pendingMigrations.Any())
                {
                    _logger.LogInformation("📦 {Count} pending migrations found. Applying...", pendingMigrations.Count());
                    
                    await _dbContext.Database.MigrateAsync();
                    
                    _logger.LogInformation("✅ All migrations applied successfully!");
                }
                else
                {
                    _logger.LogInformation("✅ Database is up to date.");
                }
            }
            else
            {
                _logger.LogWarning("⚠️ Could not connect to PostgreSQL. Skipping database operations.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Database migration error: {Message}", ex.Message);
        }
    }

    public async Task SeedDataAsync()
    {
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync();
            if (!canConnect)
            {
                _logger.LogWarning("⚠️ Skipping seed - no database connection.");
                return;
            }

            // 1. Create Roles and Admin
            await SeedRolesAndAdminAsync();

            // 2. Add Default Models
            if (!await _dbContext.FuzzAiModels.AnyAsync(m => !m.IsCustom))
            {
                _logger.LogInformation("🌱 Seeding default AI models...");
                
                var defaultModels = new List<Entities.FuzzAiModel>();

                // Gemini Models
                defaultModels.Add(new Entities.FuzzAiModel { Provider = Entities.AiProvider.Gemini, ModelId = "gemini-1.5-flash", DisplayName = "Gemini 1.5 Flash" });
                defaultModels.Add(new Entities.FuzzAiModel { Provider = Entities.AiProvider.Gemini, ModelId = "gemini-1.5-pro", DisplayName = "Gemini 1.5 Pro" });
                defaultModels.Add(new Entities.FuzzAiModel { Provider = Entities.AiProvider.Gemini, ModelId = "gemma-2-9b", DisplayName = "Gemma 2 9B" });

                // OpenAI Models
                defaultModels.Add(new Entities.FuzzAiModel { Provider = Entities.AiProvider.OpenAI, ModelId = "gpt-4o", DisplayName = "GPT-4o" });
                defaultModels.Add(new Entities.FuzzAiModel { Provider = Entities.AiProvider.OpenAI, ModelId = "gpt-4o-mini", DisplayName = "GPT-4o-mini" });
                defaultModels.Add(new Entities.FuzzAiModel { Provider = Entities.AiProvider.OpenAI, ModelId = "o1-preview", DisplayName = "o1 Preview" });

                _dbContext.FuzzAiModels.AddRange(defaultModels);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("✅ {Count} default models added successfully.", defaultModels.Count);
            }
            
            _logger.LogInformation("🌱 Seeding completed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Seed error: {Message}", ex.Message);
        }
    }

    private async Task SeedRolesAndAdminAsync()
    {
        if (!await _roleManager.RoleExistsAsync("Admin"))
        {
            _logger.LogInformation("🔑 Creating 'Admin' role...");
            await _roleManager.CreateAsync(new IdentityRole("Admin"));
        }

        var adminEmail = "admin@fuzz.com";
        var adminUser = await _userManager.FindByEmailAsync(adminEmail);
        
        if (adminUser == null)
        {
            _logger.LogInformation("👤 Creating default admin user...");
            adminUser = new FuzzUser 
            { 
                UserName = adminEmail, 
                Email = adminEmail, 
                EmailConfirmed = true 
            };
            
            var result = await _userManager.CreateAsync(adminUser, "Admin123!");
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(adminUser, "Admin");
                _logger.LogInformation("✅ Admin user created and assigned to 'Admin' role.");
            }
        }
    }
}
