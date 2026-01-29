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
            _logger.LogInformation("🔄 Veritabanı bağlantısı kontrol ediliyor...");
            
            // PostgreSQL erişilebilir mi kontrol et
            var canConnect = await _dbContext.Database.CanConnectAsync();
            
            if (canConnect)
            {
                _logger.LogInformation("✅ PostgreSQL bağlantısı başarılı!");
                
                // Bekleyen migration var mı?
                var pendingMigrations = await _dbContext.Database.GetPendingMigrationsAsync();
                
                if (pendingMigrations.Any())
                {
                    _logger.LogInformation("📦 {Count} bekleyen migration bulundu. Uygulanıyor...", pendingMigrations.Count());
                    
                    await _dbContext.Database.MigrateAsync();
                    
                    _logger.LogInformation("✅ Tüm migration'lar başarıyla uygulandı!");
                }
                else
                {
                    _logger.LogInformation("✅ Veritabanı güncel, migration gerekmiyor.");
                }
            }
            else
            {
                _logger.LogWarning("⚠️ PostgreSQL bağlantısı kurulamadı. Veritabanı işlemleri atlanıyor.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Veritabanı migration hatası: {Message}", ex.Message);
        }
    }

    public async Task SeedDataAsync()
    {
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync();
            if (!canConnect)
            {
                _logger.LogWarning("⚠️ Seed işlemi atlanıyor - veritabanı bağlantısı yok.");
                return;
            }

            // 1. Rolleri ve Admin'i oluştur
            await SeedRolesAndAdminAsync();

            // 2. Varsayılan modelleri ekle
            if (!await _dbContext.AiModels.AnyAsync(m => !m.IsCustom))
            {
                _logger.LogInformation("🌱 Varsayılan AI modelleri ekleniyor...");
                
                var defaultModels = new List<Entities.FuzzAiModel>();

                // Gemini Modelleri
                defaultModels.Add(new Entities.FuzzAiModel { Provider = Entities.AiProvider.Gemini, ModelId = "gemini-3-flash", DisplayName = "Gemini 3 Flash" });
                defaultModels.Add(new Entities.FuzzAiModel { Provider = Entities.AiProvider.Gemini, ModelId = "gemini-2.1-flash", DisplayName = "Gemini 2.1 Flash" });
                defaultModels.Add(new Entities.FuzzAiModel { Provider = Entities.AiProvider.Gemini, ModelId = "gemini-robotics-er-1.5-preview", DisplayName = "Gemini Robotics ER 1.5 Preview" });
                defaultModels.Add(new Entities.FuzzAiModel { Provider = Entities.AiProvider.Gemini, ModelId = "gemma-3-27b", DisplayName = "Gemma 3 27B" });

                // OpenAI Modelleri
                defaultModels.Add(new Entities.FuzzAiModel { Provider = Entities.AiProvider.OpenAI, ModelId = "gpt-4o", DisplayName = "GPT-4o" });
                defaultModels.Add(new Entities.FuzzAiModel { Provider = Entities.AiProvider.OpenAI, ModelId = "gpt-4o-mini", DisplayName = "GPT-4o-mini" });
                defaultModels.Add(new Entities.FuzzAiModel { Provider = Entities.AiProvider.OpenAI, ModelId = "o1-preview", DisplayName = "o1 Preview" });

                _dbContext.AiModels.AddRange(defaultModels);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("✅ {Count} varsayılan model başarıyla eklendi.", defaultModels.Count);
            }
            
            _logger.LogInformation("🌱 Seed işlemi tamamlandı.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Seed hatası: {Message}", ex.Message);
        }
    }

    private async Task SeedRolesAndAdminAsync()
    {
        // Rolü oluştur
        if (!await _roleManager.RoleExistsAsync("Admin"))
        {
            _logger.LogInformation("🔑 'Admin' rolü oluşturuluyor...");
            await _roleManager.CreateAsync(new IdentityRole("Admin"));
        }

        // Admin kullanıcısını oluştur
        var adminEmail = "admin@fuzz.com";
        var adminUser = await _userManager.FindByEmailAsync(adminEmail);
        
        if (adminUser == null)
        {
            _logger.LogInformation("👤 Varsayılan admin kullanıcısı oluşturuluyor...");
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
                _logger.LogInformation("✅ Admin kullanıcısı oluşturuldu ve 'Admin' rolüne atandı.");
            }
        }
    }
}
