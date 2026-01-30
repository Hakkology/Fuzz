using Fuzz.Domain.Data;
using Fuzz.Domain.Entities;
using Fuzz.Domain.Models;
using Fuzz.Domain.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Fuzz.Domain.Services.AI;

public class VisualAgentDispatcherService : IVisualAgentService
{
    private readonly IDbContextFactory<FuzzDbContext> _dbFactory;
    private readonly IAiChatValidationService _validationService;
    private readonly IVisualAgentService _geminiService;
    private readonly IVisualAgentService _openaiService;
    private readonly IVisualAgentService _localService;

    public VisualAgentDispatcherService(
        IDbContextFactory<FuzzDbContext> dbFactory,
        IAiChatValidationService validationService,
        [FromKeyedServices(AiProvider.Gemini)] IVisualAgentService geminiService,
        [FromKeyedServices(AiProvider.OpenAI)] IVisualAgentService openaiService,
        [FromKeyedServices(AiProvider.Local)] IVisualAgentService localService)
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
            .FirstOrDefaultAsync(c => c.UserId == userId && c.IsActive && c.Mode == AiCapabilities.Visual);
        return active?.Provider;
    }

    public async Task<FuzzResponse> ProcessImageAsync(byte[] imageData, string prompt, string userId)
    {
        var validation = await _validationService.ValidateAndSanitizeAsync(prompt);
        if (!validation.IsValid)
        {
            return new FuzzResponse { Answer = $"Validation Error: {validation.ErrorMessage}" };
        }

        var provider = await GetActiveProviderAsync(userId);
        
        return provider switch
        {
            AiProvider.Gemini => await _geminiService.ProcessImageAsync(imageData, validation.SanitizedInput, userId),
            AiProvider.OpenAI => await _openaiService.ProcessImageAsync(imageData, validation.SanitizedInput, userId),
            AiProvider.Local => await _localService.ProcessImageAsync(imageData, validation.SanitizedInput, userId),
            _ => new FuzzResponse { Answer = "Please select an active Visual AI provider from the 'AI Settings' page." }
        };
    }

    public void ClearHistory()
    {
        _geminiService.ClearHistory();
        _openaiService.ClearHistory();
        _localService.ClearHistory();
    }
}
