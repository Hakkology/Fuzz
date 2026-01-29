using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Fuzz.Domain.Data;
using Fuzz.Domain.Entities;
using Fuzz.Domain.Models;
using System.Net.Http;
using System.Net.Http.Json;

namespace Fuzz.Domain.Services;

public class AiConfigService : IAiConfigService
{
    private readonly IDbContextFactory<FuzzDbContext> _dbFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AiConfigService> _logger;

    public AiConfigService(
        IDbContextFactory<FuzzDbContext> dbFactory, 
        IHttpClientFactory httpClientFactory,
        ILogger<AiConfigService> logger)
    {
        _dbFactory = dbFactory;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<FuzzAiConfig?> GetActiveConfigAsync(string userId, AiProvider provider)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        return await db.AiConfigurations
            .FirstOrDefaultAsync(c => c.UserId == userId && c.IsActive && c.Provider == provider);
    }

    public async Task<List<FuzzAiConfig>> GetUserConfigsAsync(string userId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        return await db.AiConfigurations
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.IsActive)
            .ThenBy(c => c.Provider)
            .ToListAsync();
    }

    public async Task<List<FuzzAiModel>> GetModelsAsync(AiProvider? provider = null)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var query = db.FuzzAiModels.AsQueryable();
        if (provider.HasValue)
        {
            query = query.Where(m => m.Provider == provider.Value);
        }
        return await query.OrderBy(m => m.DisplayName).ToListAsync();
    }

    public async Task<List<FuzzAiModel>> GetLocalModelsAsync(string? apiBase = null)
    {
        var localModels = new List<FuzzAiModel>();
        try
        {
            var baseUrl = string.IsNullOrWhiteSpace(apiBase) ? "http://localhost:11434" : apiBase.TrimEnd('/');
            if (baseUrl.EndsWith("/v1")) baseUrl = baseUrl.Substring(0, baseUrl.Length - 3);

            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(3);
            
            var response = await client.GetFromJsonAsync<OllamaTagsResponse>($"{baseUrl}/api/tags");
            if (response?.Models != null)
            {
                using var db = await _dbFactory.CreateDbContextAsync();
                var existingModelIds = await db.FuzzAiModels
                    .Where(m => m.Provider == AiProvider.Local)
                    .Select(m => m.ModelId.ToLower().Trim())
                    .ToListAsync();

                var processedModels = new HashSet<string>();

                foreach (var model in response.Models)
                {
                    var cleanId = model.Name.ToLower().Trim();
                    
                    // Respondaki mükerrerleri veya zaten DB'de olanları atla
                    if (processedModels.Contains(cleanId) || existingModelIds.Contains(cleanId))
                        continue;

                    var modelEntity = new FuzzAiModel 
                    { 
                        Provider = AiProvider.Local, 
                        ModelId = model.Name, 
                        DisplayName = $"{model.Name} (Local)",
                        IsCustom = true
                    };
                    localModels.Add(modelEntity);
                    db.FuzzAiModels.Add(modelEntity);
                    processedModels.Add(cleanId);
                }
                await db.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Ollama modelleri çekilemedi veya kaydedilemedi: {Message}", ex.Message);
        }
        return localModels;
    }

    public async Task AddConfigAsync(FuzzAiConfig config)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        db.AiConfigurations.Add(config);
        await db.SaveChangesAsync();
    }

    public async Task DeleteConfigAsync(int id)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var config = await db.AiConfigurations.FindAsync(id);
        if (config != null)
        {
            db.AiConfigurations.Remove(config);
            await db.SaveChangesAsync();
        }
    }

    public async Task SetActiveConfigAsync(int id, string userId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var all = await db.AiConfigurations.Where(c => c.UserId == userId).ToListAsync();
        foreach (var config in all)
        {
            config.IsActive = (config.Id == id);
        }
        await db.SaveChangesAsync();
    }

    public async Task AddCustomModelAsync(AiProvider provider, string modelId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var exists = await db.FuzzAiModels.AnyAsync(m => m.Provider == provider && m.ModelId == modelId);
        if (!exists)
        {
            db.FuzzAiModels.Add(new FuzzAiModel
            {
                Provider = provider,
                ModelId = modelId,
                DisplayName = $"{modelId} (Özel)",
                IsCustom = true
            });
            await db.SaveChangesAsync();
        }
    }

    public async Task AddModelAsync(FuzzAiModel model)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        db.FuzzAiModels.Add(model);
        await db.SaveChangesAsync();
    }

    public async Task DeleteModelAsync(int id)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var model = await db.FuzzAiModels.FindAsync(id);
        if (model != null)
        {
            db.FuzzAiModels.Remove(model);
            await db.SaveChangesAsync();
        }
    }
}
