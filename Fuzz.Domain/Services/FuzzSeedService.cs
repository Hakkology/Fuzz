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

            // Varsayılan modelleri ekle
            if (!await _dbContext.AiModels.AnyAsync(m => !m.IsCustom))
            {
                _logger.LogInformation("🌱 Varsayılan AI modelleri ekleniyor...");
                
                var defaultModels = new List<Entities.FuzzAiModel>();

                // Gemini Modelleri
                defaultModels.Add(new Entities.FuzzAiModel { Provider = Entities.AiProvider.Gemini, ModelId = "gemini-3-flash", DisplayName = "Gemini 3 Flash" });
                defaultModels.Add(new Entities.FuzzAiModel { Provider = Entities.AiProvider.Gemini, ModelId = "gemini-2.5-flash-lite", DisplayName = "Gemini 2.5 Flash Lite" });
                defaultModels.Add(new Entities.FuzzAiModel { Provider = Entities.AiProvider.Gemini, ModelId = "gemini-2.5-flash-tts", DisplayName = "Gemini 2.5 Flash TTS" });
                defaultModels.Add(new Entities.FuzzAiModel { Provider = Entities.AiProvider.Gemini, ModelId = "gemini-2.5-flash", DisplayName = "Gemini 2.5 Flash" });
                defaultModels.Add(new Entities.FuzzAiModel { Provider = Entities.AiProvider.Gemini, ModelId = "gemini-robotics-er-1.5-preview", DisplayName = "Gemini Robotics ER 1.5 Preview" });
                defaultModels.Add(new Entities.FuzzAiModel { Provider = Entities.AiProvider.Gemini, ModelId = "gemma-3-12b", DisplayName = "Gemma 3 12B" });
                defaultModels.Add(new Entities.FuzzAiModel { Provider = Entities.AiProvider.Gemini, ModelId = "gemma-3-1b", DisplayName = "Gemma 3 1B" });
                defaultModels.Add(new Entities.FuzzAiModel { Provider = Entities.AiProvider.Gemini, ModelId = "gemma-3-27b", DisplayName = "Gemma 3 27B" });
                defaultModels.Add(new Entities.FuzzAiModel { Provider = Entities.AiProvider.Gemini, ModelId = "gemma-3-2b", DisplayName = "Gemma 3 2B" });
                defaultModels.Add(new Entities.FuzzAiModel { Provider = Entities.AiProvider.Gemini, ModelId = "gemma-3-4b", DisplayName = "Gemma 3 4B" });
                defaultModels.Add(new Entities.FuzzAiModel { Provider = Entities.AiProvider.Gemini, ModelId = "gemini-embedding-1", DisplayName = "Gemini Embedding 1" });
                defaultModels.Add(new Entities.FuzzAiModel { Provider = Entities.AiProvider.Gemini, ModelId = "gemini-2.5-flash-native-audio-dialog", DisplayName = "Gemini 2.5 Flash Native Audio Dialog" });

                // OpenAI Modelleri
                defaultModels.Add(new Entities.FuzzAiModel { Provider = Entities.AiProvider.OpenAI, ModelId = "gpt-4o", DisplayName = "GPT-4o" });
                defaultModels.Add(new Entities.FuzzAiModel { Provider = Entities.AiProvider.OpenAI, ModelId = "gpt-4o-mini", DisplayName = "GPT-4o-mini" });
                defaultModels.Add(new Entities.FuzzAiModel { Provider = Entities.AiProvider.OpenAI, ModelId = "o1", DisplayName = "o1" });
                defaultModels.Add(new Entities.FuzzAiModel { Provider = Entities.AiProvider.OpenAI, ModelId = "o1-mini", DisplayName = "o1-mini" });

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
}
