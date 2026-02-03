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
    private const int OllamaTimeoutSeconds = 10;

    public AiConfigService(
        IDbContextFactory<FuzzDbContext> dbFactory, 
        IHttpClientFactory httpClientFactory,
        ILogger<AiConfigService> logger)
    {
        _dbFactory = dbFactory;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }



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





    public async Task<FuzzAiConfig?> GetActiveConfigAsync(string userId, AiProvider? provider = null, AiCapabilities mode = AiCapabilities.Text)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        
        if (provider.HasValue)
        {
            return await db.AiConfigurations
                .FirstOrDefaultAsync(c => c.UserId == userId && c.IsActive && c.Provider == provider.Value && (c.Mode & mode) == mode);
        }
        
        // No provider specified - just find active config for this mode
        return await db.AiConfigurations
            .FirstOrDefaultAsync(c => c.UserId == userId && c.IsActive && (c.Mode & mode) == mode);
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

        // Deactivate other configs of the same MODE (e.g. deactivate other Text agents)
        var sameModeConfigs = await db.AiConfigurations
            .Where(c => c.UserId == userId && (c.Mode & targetConfig.Mode) != 0)
            .ToListAsync();
            
        sameModeConfigs.ForEach(c => c.IsActive = false);
        
        targetConfig.IsActive = true;
        await db.SaveChangesAsync();
    }





    public async Task<List<FuzzAiModel>> GetModelsAsync(AiProvider? provider = null, AiCapabilities? capability = null)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var query = db.FuzzAiModels.AsQueryable();
        if (provider.HasValue)
            query = query.Where(m => m.Provider == provider.Value);
        
        if (capability.HasValue)
        {
            // Simple bitwise check: (Capabilities & TestedCap) == TestedCap
            // EF Core might need raw SQL or supported translation.
            // For now, attempting standard Enum usage which EF Core 7+ supports better.
            var cap = capability.Value;
            query = query.Where(m => m.Capabilities.HasFlag(cap)); 
        }
            
        return await query.OrderBy(m => m.DisplayName).ToListAsync();
    }

    public async Task<List<FuzzAiModel>> GetLocalModelsAsync(string? apiBase = null)
    {
        var ollamaModels = await FetchOllamaModelsAsync(apiBase);
        if (ollamaModels == null) return new();

        using var db = await _dbFactory.CreateDbContextAsync();
        
        // Use a group by to handle potential duplicates in DB gracefully
        var existingList = await db.FuzzAiModels
            .Where(m => m.Provider == AiProvider.Local)
            .ToListAsync();

        var existingModels = existingList
            .GroupBy(m => m.ModelId.ToLower().Trim())
            .ToDictionary(g => g.Key, g => g.First());

        var result = new List<FuzzAiModel>();

        foreach (var model in ollamaModels)
        {
            var modelId = model.Name.ToLower().Trim();
            
            // Detect capabilities
            var caps = AiCapabilities.Text; 
            if (IsVisualModel(model)) caps |= AiCapabilities.Visual;
            if (IsSoundModel(model)) caps |= AiCapabilities.Sound;

            if (!existingModels.TryGetValue(modelId, out var entity))
            {
                entity = new FuzzAiModel
                {
                    Provider = AiProvider.Local,
                    ModelId = model.Name,
                    DisplayName = $"{model.Name} (Local)",
                    IsCustom = true,
                    Capabilities = caps 
                };
                db.FuzzAiModels.Add(entity);
                _logger.LogInformation("🆕 Discovered new local model: {ModelId} with caps {Caps}", model.Name, caps);
            }
            else
            {
                // Ensure name and capabilities are synced
                entity.Capabilities = caps;
                if (string.IsNullOrEmpty(entity.DisplayName))
                {
                    entity.DisplayName = $"{model.Name} (Local)";
                }
            }

            result.Add(entity);
        }

        await db.SaveChangesAsync();
        return result;
    }




    private static bool IsVisualModel(OllamaModel model)
    {
        // Check metadata families for 'clip' or 'vision'
        if (model.Details?.Families != null && model.Details.Families.Any(f => f.Contains("clip") || f.Contains("vision")))
        {
            return true;
        }
        
        // Fallback to name heuristic
        var lower = model.Name.ToLower();
        return lower.Contains("llava") || lower.Contains("vision") || lower.Contains("moondream") || lower.Contains("bakllava");
    }

    private static bool IsSoundModel(OllamaModel model)
    {
        var lowerName = model.Name.ToLower();
        var lowerFamily = model.Details?.Family?.ToLower() ?? "";
        var families = model.Details?.Families?.Select(f => f.ToLower()) ?? Enumerable.Empty<string>();
        
        bool match = lowerName.Contains("music") || 
                     lowerName.Contains("sound") || 
                     lowerName.Contains("audio") ||
                     lowerName.Contains("voice") ||
                     lowerName.Contains("llamusic") ||
                     lowerFamily.Contains("music") ||
                     lowerFamily.Contains("sound") ||
                     lowerFamily.Contains("audio") ||
                     families.Any(f => f.Contains("music") || f.Contains("sound") || f.Contains("audio"));

        return match;
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

    public async Task SaveSqlLogAsync(FuzzSqlLog log)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        db.SqlLogs.Add(log);
        await db.SaveChangesAsync();
    }

    public async Task<List<FuzzSqlLog>> GetSqlLogsAsync(string userId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        return await db.SqlLogs
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();
    }
}
