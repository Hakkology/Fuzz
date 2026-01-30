using Fuzz.Domain.Data;
using Fuzz.Domain.Entities;
using Fuzz.Domain.Models;
using Fuzz.Domain.Services.AI;
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
    private readonly IAiChatValidationService _validationService;

    public string? LastSql => _geminiService.LastSql ?? _openaiService.LastSql ?? _localService.LastSql;

    public AgentDispatcherService(
        IDbContextFactory<FuzzDbContext> dbFactory,
        IAiChatValidationService validationService,
        [FromKeyedServices(AiProvider.Gemini)] IFuzzAgentService geminiService,
        [FromKeyedServices(AiProvider.OpenAI)] IFuzzAgentService openaiService,
        [FromKeyedServices(AiProvider.Local)] IFuzzAgentService localService)
    {
        _dbFactory = dbFactory;
        _validationService = validationService;
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

    public async Task<FuzzResponse> ProcessCommandAsync(string input, string userId, bool useTools = true)
    {
        var validation = await _validationService.ValidateAndSanitizeAsync(input);
        if (!validation.IsValid)
        {
            return new FuzzResponse { Answer = $"Validation Error: {validation.ErrorMessage}" };
        }

        var provider = await GetActiveProviderAsync(userId);
        
        return provider switch
        {
            AiProvider.Gemini => await _geminiService.ProcessCommandAsync(validation.SanitizedInput, userId, useTools),
            AiProvider.OpenAI => await _openaiService.ProcessCommandAsync(validation.SanitizedInput, userId, useTools),
            AiProvider.Local => await _localService.ProcessCommandAsync(validation.SanitizedInput, userId, useTools),
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
