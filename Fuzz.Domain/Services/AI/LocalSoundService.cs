using Fuzz.Domain.Entities;
using Fuzz.Domain.Models;
using Fuzz.Domain.Services.Interfaces;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Text.RegularExpressions;

namespace Fuzz.Domain.Services.AI;

public class LocalSoundService : ISoundAgentService
{
    private readonly IAiConfigService _configService;
    private readonly ILogger<LocalSoundService> _logger;
    private readonly List<ChatMessage> _history = new();

    public LocalSoundService(
        IAiConfigService configService,
        ILogger<LocalSoundService> logger)
    {
        _configService = configService;
        _logger = logger;
    }

    public async Task<FuzzResponse> GenerateMusicAsync(string prompt, string userId)
    {
        try
        {
            var configData = await _configService.GetActiveConfigAsync(userId, AiProvider.Local, mode: AiCapabilities.Sound);
            if (configData == null)
                return new FuzzResponse { Answer = "⚠️ Please configure an active Local Sound AI (llamusic) in Settings." };

            var modelId = string.IsNullOrWhiteSpace(configData.ModelId) ? "llamusic/llamusic:3b" : configData.ModelId;
            var apiBase = string.IsNullOrWhiteSpace(configData.ApiBase) ? "http://localhost:11434/v1" : configData.ApiBase;
            var apiKey = string.IsNullOrWhiteSpace(configData.ApiKey) ? "ollama" : configData.ApiKey.Trim();

            var client = new ChatClient(
                model: modelId,
                credential: new ApiKeyCredential(apiKey),
                options: new OpenAIClientOptions { Endpoint = new Uri(apiBase) });

            var systemContext = @"You are a music composer AI. 
YOUR GOAL: Generate a valid ABC Music Notation block based on the user's request.
RULES:
- OUTPUT ONLY the ABC notation. No conversational text, no explanations, no decorations at the end.
- Use EXACT format for headers: 'X:1', 'T:Title', 'M:4/4', 'L:1/4', 'K:C' (always use colon ':'). 
- Valid keys are letters like C, G, D, Am, etc.
- Only return the raw ABC code.";

            _history.Clear();
            _history.Add(new SystemChatMessage(systemContext));
            _history.Add(new UserChatMessage(prompt));

            var parameters = await _configService.GetParametersAsync(configData.Id);
            var options = new ChatCompletionOptions
            {
                Temperature = parameters != null ? (float)parameters.Temperature : 0.7f,
                MaxOutputTokenCount = parameters?.MaxTokens ?? 2048
            };

            var result = await client.CompleteChatAsync(_history, options);
            var completion = result.Value;
            var rawResponse = completion.Content[0].Text;

            return new FuzzResponse { Answer = ExtractAbcBlock(rawResponse) };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Local Sound Service Error");
            return new FuzzResponse { Answer = $"A technical error occurred: {ex.Message}" };
        }
    }

    private string ExtractAbcBlock(string text)
    {
        var match = Regex.Match(text, @"X:\s*[0-9]+.*", RegexOptions.Singleline);
        if (match.Success)
        {
            var abc = match.Value;
            abc = abc.Replace("```abc", "").Replace("```", "").Trim();
            return abc;
        }
        return text;
    }

    public void ClearHistory() => _history.Clear();
}
