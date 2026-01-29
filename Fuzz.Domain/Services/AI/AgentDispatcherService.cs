using Fuzz.Domain.Data;
using Fuzz.Domain.Entities;
using Fuzz.Domain.Models;
using Fuzz.Domain.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Fuzz.Domain.Services;

public class AgentDispatcherService : IFuzzAgentService
{
    private readonly IDbContextFactory<FuzzDbContext> _dbFactory;
    private readonly IFuzzAgentService _geminiService;
    private readonly IFuzzAgentService _openaiService;
    private readonly IFuzzAgentService _localService;

    public string? LastSql => _geminiService.LastSql ?? _openaiService.LastSql ?? _localService.LastSql;

    public AgentDispatcherService(
        IDbContextFactory<FuzzDbContext> dbFactory,
        [FromKeyedServices(AiProvider.Gemini)] IFuzzAgentService geminiService,
        [FromKeyedServices(AiProvider.OpenAI)] IFuzzAgentService openaiService,
        [FromKeyedServices(AiProvider.Local)] IFuzzAgentService localService)
    {
        _dbFactory = dbFactory;
        _geminiService = geminiService;
        _openaiService = openaiService;
        _localService = localService;
    }

    private async Task<AiProvider?> GetActiveProviderAsync(string userId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var active = await db.AiConfigurations
            .FirstOrDefaultAsync(c => c.UserId == userId && c.IsActive && c.Mode == AiCapabilities.Text);
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
            _ => new FuzzResponse { Answer = "Please select an active AI provider from the 'AI Settings' page." }
        };
    }

    public void ClearHistory()
    {
        _geminiService.ClearHistory();
        _openaiService.ClearHistory();
        _localService.ClearHistory();
    }
}
