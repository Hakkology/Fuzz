using Microsoft.EntityFrameworkCore;
using Fuzz.Domain.Data;
using Fuzz.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Fuzz.Domain.Services;

public class AiConfigService : IAiConfigService
{
    private readonly IDbContextFactory<FuzzDbContext> _dbFactory;
    private readonly ILogger<AiConfigService> _logger;

    public AiConfigService(IDbContextFactory<FuzzDbContext> dbFactory, ILogger<AiConfigService> logger)
    {
        _dbFactory = dbFactory;
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
        var query = db.AiModels.AsQueryable();
        if (provider.HasValue)
        {
            query = query.Where(m => m.Provider == provider.Value);
        }
        return await query.OrderBy(m => m.DisplayName).ToListAsync();
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
        var exists = await db.AiModels.AnyAsync(m => m.Provider == provider && m.ModelId == modelId);
        if (!exists)
        {
            db.AiModels.Add(new FuzzAiModel
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
        db.AiModels.Add(model);
        await db.SaveChangesAsync();
    }

    public async Task DeleteModelAsync(int id)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var model = await db.AiModels.FindAsync(id);
        if (model != null)
        {
            db.AiModels.Remove(model);
            await db.SaveChangesAsync();
        }
    }
}
