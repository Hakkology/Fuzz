using Fuzz.Domain.Data;
using Fuzz.Domain.Entities;
using Fuzz.Domain.Models;
using Fuzz.Domain.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace Fuzz.Domain.Services;

public class AiConfigService : IAiConfigService
{
    private readonly IDbContextFactory<FuzzDbContext> _dbFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AiConfigService> _logger;

    private const string DefaultOllamaUrl = "http://localhost:11434";
    private const int OllamaTimeoutSeconds = 3;

    public AiConfigService(
        IDbContextFactory<FuzzDbContext> dbFactory, 
        IHttpClientFactory httpClientFactory,
        ILogger<AiConfigService> logger)
    {
        _dbFactory = dbFactory;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    #region Helper Methods

    private static string NormalizeOllamaUrl(string? apiBase)
    {
        var url = string.IsNullOrWhiteSpace(apiBase) ? DefaultOllamaUrl : apiBase.TrimEnd('/');
        return url.EndsWith("/v1") ? url[..^3] : url;
    }

    private async Task<List<OllamaModel>?> FetchOllamaModelsAsync(string? apiBase)
    {
        try
        {
            var baseUrl = NormalizeOllamaUrl(apiBase);
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(OllamaTimeoutSeconds);

            var response = await client.GetFromJsonAsync<OllamaTagsResponse>($"{baseUrl}/api/tags");
            return response?.Models;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to fetch Ollama models: {Message}", ex.Message);
            return null;
        }
    }

    #endregion

    #region Configuration Methods

    public async Task<FuzzAiConfig?> GetActiveConfigAsync(string userId, AiProvider provider, bool isVisual = false)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        return await db.AiConfigurations
            .FirstOrDefaultAsync(c => c.UserId == userId && c.IsActive && c.Provider == provider && c.IsVisualRecognition == isVisual);
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
        var targetConfig = await db.AiConfigurations.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
        if (targetConfig == null) return;

        // Deactivate other configs of the same type (Text vs Visual)
        var sameTypeConfigs = await db.AiConfigurations
            .Where(c => c.UserId == userId && c.IsVisualRecognition == targetConfig.IsVisualRecognition)
            .ToListAsync();
            
        sameTypeConfigs.ForEach(c => c.IsActive = false);
        
        targetConfig.IsActive = true;
        await db.SaveChangesAsync();
    }

    #endregion

    #region Model Methods

    public async Task<List<FuzzAiModel>> GetModelsAsync(AiProvider? provider = null, bool? isVisual = null)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var query = db.FuzzAiModels.AsQueryable();
        if (provider.HasValue)
            query = query.Where(m => m.Provider == provider.Value);
        if (isVisual.HasValue)
            query = query.Where(m => m.IsVisualRecognition == isVisual.Value);
        return await query.OrderBy(m => m.DisplayName).ToListAsync();
    }

    public async Task<List<FuzzAiModel>> GetLocalModelsAsync(string? apiBase = null)
    {
        var newModels = new List<FuzzAiModel>();
        
        var ollamaModels = await FetchOllamaModelsAsync(apiBase);
        if (ollamaModels == null) return newModels;

        using var db = await _dbFactory.CreateDbContextAsync();
        var existingIds = await db.FuzzAiModels
            .Where(m => m.Provider == AiProvider.Local)
            .Select(m => m.ModelId.ToLower().Trim())
            .ToHashSetAsync();

        foreach (var model in ollamaModels)
        {
            var modelId = model.Name.ToLower().Trim();
            if (existingIds.Contains(modelId)) continue;

            bool isVisual = IsVisualModel(model);

            var entity = new FuzzAiModel
            {
                Provider = AiProvider.Local,
                ModelId = model.Name,
                DisplayName = $"{model.Name} (Local)",
                IsCustom = true,
                IsVisualRecognition = isVisual,
                IsTextCapable = !isVisual // Strict separation for local: if it's visual, assume it's NOT for text only (or at least hide it)
            };
            newModels.Add(entity);
            db.FuzzAiModels.Add(entity);
        }

        if (newModels.Any()) await db.SaveChangesAsync();
        return newModels;
    }

    private static bool IsVisualModel(OllamaModel model)
    {
        // 1. Check metadata families for 'clip' or 'vision'
        if (model.Details?.Families != null && model.Details.Families.Any(f => f.Contains("clip") || f.Contains("vision")))
        {
            return true;
        }
        
        // 2. Fallback to name heuristic
        var lower = model.Name.ToLower();
        return lower.Contains("llava") || lower.Contains("vision") || lower.Contains("moondream") || lower.Contains("bakllava");
    }

    public async Task AddModelAsync(FuzzAiModel model)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        db.FuzzAiModels.Add(model);
        await db.SaveChangesAsync();
    }

    public async Task AddCustomModelAsync(AiProvider provider, string modelId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        if (!await db.FuzzAiModels.AnyAsync(m => m.Provider == provider && m.ModelId == modelId))
        {
            db.FuzzAiModels.Add(new FuzzAiModel
            {
                Provider = provider,
                ModelId = modelId,
                DisplayName = $"{modelId} (Custom)",
                IsCustom = true
            });
            await db.SaveChangesAsync();
        }
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

    public async Task<int> CleanupMissingLocalModelsAsync(string userId, string? apiBase = null)
    {
        var ollamaModels = await FetchOllamaModelsAsync(apiBase);
        if (ollamaModels == null) return 0;
        
        var availableIds = ollamaModels.Select(m => m.Name.ToLower().Trim()).ToHashSet();

        using var db = await _dbFactory.CreateDbContextAsync();
        int deletedCount = 0;

        // Cleanup user configs for missing models
        var localConfigs = await db.AiConfigurations
            .Where(c => c.UserId == userId && c.Provider == AiProvider.Local)
            .ToListAsync();

        foreach (var config in localConfigs.Where(c => !availableIds.Contains(c.ModelId.ToLower().Trim())))
        {
            db.AiConfigurations.Remove(config);
            deletedCount++;
        }

        // Cleanup orphaned model entries
        var localModels = await db.FuzzAiModels
            .Where(m => m.Provider == AiProvider.Local)
            .ToListAsync();

        db.FuzzAiModels.RemoveRange(localModels.Where(m => !availableIds.Contains(m.ModelId.ToLower().Trim())));

        await db.SaveChangesAsync();
        return deletedCount;
    }

    #endregion

    #region Parameters Methods

    public async Task<FuzzAiParameters?> GetParametersAsync(int configId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        return await db.FuzzAiParameters.FirstOrDefaultAsync(p => p.FuzzAiConfigId == configId);
    }

    public async Task SaveParametersAsync(FuzzAiParameters parameters)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var existing = await db.FuzzAiParameters.FirstOrDefaultAsync(p => p.FuzzAiConfigId == parameters.FuzzAiConfigId);

        if (existing == null)
        {
            db.FuzzAiParameters.Add(parameters);
        }
        else
        {
            existing.Temperature = parameters.Temperature;
            existing.MaxTokens = parameters.MaxTokens;
            existing.TopP = parameters.TopP;
            existing.FrequencyPenalty = parameters.FrequencyPenalty;
            existing.PresencePenalty = parameters.PresencePenalty;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
    }

    #endregion
}
