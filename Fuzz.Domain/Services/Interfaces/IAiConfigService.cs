using Fuzz.Domain.Entities;

namespace Fuzz.Domain.Services.Interfaces;

public interface IAiConfigService
{
    Task<FuzzAiConfig?> GetActiveConfigAsync(string userId, AiProvider? provider = null, AiCapabilities mode = AiCapabilities.Text);
    Task<List<FuzzAiConfig>> GetUserConfigsAsync(string userId);
    Task<List<FuzzAiModel>> GetModelsAsync(AiProvider? provider = null, AiCapabilities? capability = null);
    Task<List<FuzzAiModel>> GetLocalModelsAsync(string? apiBase = null);
    Task AddConfigAsync(FuzzAiConfig config);
    Task DeleteConfigAsync(int id);
    Task SetActiveConfigAsync(int id, string userId);

    
    // Admin Model Management
    Task AddModelAsync(FuzzAiModel model);
    Task DeleteModelAsync(int id);
    Task<int> CleanupMissingLocalModelsAsync(string userId, string? apiBase = null);

    // AI Parameters
    Task<FuzzAiParameters?> GetParametersAsync(int configId);
    Task SaveParametersAsync(FuzzAiParameters parameters);
}
