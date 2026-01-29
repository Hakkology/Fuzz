using Fuzz.Domain.Entities;

namespace Fuzz.Domain.Services;

public interface IAiConfigService
{
    Task<FuzzAiConfig?> GetActiveConfigAsync(string userId, AiProvider provider);
    Task<List<FuzzAiConfig>> GetUserConfigsAsync(string userId);
    Task<List<FuzzAiModel>> GetModelsAsync(AiProvider? provider = null);
    Task<List<FuzzAiModel>> GetLocalModelsAsync(string? apiBase = null);
    Task AddConfigAsync(FuzzAiConfig config);
    Task DeleteConfigAsync(int id);
    Task SetActiveConfigAsync(int id, string userId);
    Task AddCustomModelAsync(AiProvider provider, string modelId);
    
    // Admin Model Management
    Task AddModelAsync(FuzzAiModel model);
    Task DeleteModelAsync(int id);

    // AI Parameters
    Task<FuzzAiParameters?> GetParametersAsync(int configId);
    Task SaveParametersAsync(FuzzAiParameters parameters);
}
