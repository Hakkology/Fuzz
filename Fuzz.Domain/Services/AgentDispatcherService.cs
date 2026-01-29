using Microsoft.EntityFrameworkCore;
using Fuzz.Domain.Data;
using Fuzz.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Fuzz.Domain.Services;

public class AgentDispatcherService : IFuzzAgentService
{
    private readonly IDbContextFactory<FuzzDbContext> _dbFactory;
    private readonly IFuzzAgentService _geminiService;
    private readonly IFuzzAgentService _openaiService;
    private readonly IFuzzAgentService _localService;
    private readonly ILogger<AgentDispatcherService> _logger;

    public string? LastSql => _geminiService.LastSql ?? _openaiService.LastSql ?? _localService.LastSql;

    public AgentDispatcherService(
        IDbContextFactory<FuzzDbContext> dbFactory,
        [FromKeyedServices(AiProvider.Gemini)] IFuzzAgentService geminiService,
        [FromKeyedServices(AiProvider.OpenAI)] IFuzzAgentService openaiService,
        [FromKeyedServices(AiProvider.Local)] IFuzzAgentService localService,
        ILogger<AgentDispatcherService> logger)
    {
        _dbFactory = dbFactory;
        _geminiService = geminiService;
        _openaiService = openaiService;
        _localService = localService;
        _logger = logger;
    }

    private async Task<AiProvider?> GetActiveProviderAsync(string userId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var active = await db.AiConfigurations
            .FirstOrDefaultAsync(c => c.UserId == userId && c.IsActive);
        return active?.Provider;
    }

    public async Task<FuzzResponse> ProcessCommandAsync(string input, string userId)
    {
        var provider = await GetActiveProviderAsync(userId);
        
        return provider switch
        {
            AiProvider.Gemini => await _geminiService.ProcessCommandAsync(input, userId),
            AiProvider.OpenAI => await _openaiService.ProcessCommandAsync(input, userId),
            AiProvider.Local => await _localService.ProcessCommandAsync(input, userId),
            _ => new FuzzResponse { Answer = "⚠️ Lütfen 'AI Ayarları' sayfasından aktif bir sağlayıcı seçin." }
        };
    }

    public void ClearHistory()
    {
        _geminiService.ClearHistory();
        _openaiService.ClearHistory();
        _localService.ClearHistory();
    }
}
