using Fuzz.Domain.Data;
using Fuzz.Domain.Entities;
using Fuzz.Domain.Models;
using Fuzz.Domain.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Fuzz.Domain.Services.AI;

public class SoundAgentDispatcherService : ISoundAgentService
{
    private readonly IDbContextFactory<FuzzDbContext> _dbFactory;
    private readonly IAiChatValidationService _validationService;
    private readonly ISoundAgentService _localService;
    private readonly ISoundAgentService _elevenLabsService;
    private readonly ISoundAgentService _replicateService;


    public SoundAgentDispatcherService(
        IDbContextFactory<FuzzDbContext> dbFactory,
        IAiChatValidationService validationService,
        [FromKeyedServices(AiProvider.Local)] ISoundAgentService localService,
        [FromKeyedServices(AiProvider.ElevenLabs)] ISoundAgentService elevenLabsService,
        [FromKeyedServices(AiProvider.Replicate)] ISoundAgentService replicateService)
    {
        _dbFactory = dbFactory;
        _validationService = validationService;
        _localService = localService;
        _elevenLabsService = elevenLabsService;
        _replicateService = replicateService;
    }

    private async Task<AiProvider?> GetActiveProviderAsync(string userId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var active = await db.AiConfigurations
            .FirstOrDefaultAsync(c => c.UserId == userId && c.IsActive && c.Mode == AiCapabilities.Sound);
        return active?.Provider;
    }

    public async Task<FuzzResponse> GenerateMusicAsync(string prompt, string userId)
    {
        var validation = await _validationService.ValidateAndSanitizeAsync(prompt);
        if (!validation.IsValid)
        {
            return new FuzzResponse { Answer = $"Validation Error: {validation.ErrorMessage}" };
        }

        var provider = await GetActiveProviderAsync(userId);
        
        return provider switch
        {
            AiProvider.Local => await _localService.GenerateMusicAsync(validation.SanitizedInput, userId),
            AiProvider.ElevenLabs => await _elevenLabsService.GenerateMusicAsync(validation.SanitizedInput, userId),
            AiProvider.Replicate => await _replicateService.GenerateMusicAsync(validation.SanitizedInput, userId),
            _ => new FuzzResponse { Answer = "Please select an active Sound AI provider from the 'AI Settings' page." }
        };
    }

    public void ClearHistory()
    {
        _localService.ClearHistory();
        _elevenLabsService.ClearHistory();
        _replicateService.ClearHistory();
    }
}
