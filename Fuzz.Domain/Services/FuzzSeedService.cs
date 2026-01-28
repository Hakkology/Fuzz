using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Fuzz.Domain.Data;

namespace Fuzz.Domain.Services;

public interface IFuzzSeedService
{
    Task ApplyMigrationsAsync();
    Task SeedDataAsync();
}

public class FuzzSeedService : IFuzzSeedService
{
    private readonly FuzzDbContext _dbContext;
    private readonly ILogger<FuzzSeedService> _logger;

    public FuzzSeedService(FuzzDbContext dbContext, ILogger<FuzzSeedService> logger)
    {
        _dbContext = dbContext;
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

            // Burada örnek veri eklenebilir
            // Örn: Varsayılan roller, admin kullanıcı vb.
            
            _logger.LogInformation("🌱 Seed işlemi tamamlandı.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Seed hatası: {Message}", ex.Message);
        }
    }
}
