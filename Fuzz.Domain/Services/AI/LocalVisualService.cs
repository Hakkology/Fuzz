using Fuzz.Domain.Entities;
using Fuzz.Domain.Models;
using Fuzz.Domain.Services.Interfaces;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace Fuzz.Domain.Services.AI;

public class LocalVisualService : IVisualAgentService
{
    private readonly IAiConfigService _configService;
    private readonly ILogger<LocalVisualService> _logger;
    private readonly List<ChatMessage> _history = new();

    public LocalVisualService(
        IAiConfigService configService,
        ILogger<LocalVisualService> logger)
    {
        _configService = configService;
        _logger = logger;
    }

    public async Task<FuzzResponse> ProcessImageAsync(byte[] imageData, string prompt, string userId)
    {
        try
        {
            var configData = await _configService.GetActiveConfigAsync(userId, AiProvider.Local, mode: AiCapabilities.Visual);
            if (configData == null)
                return new FuzzResponse { Answer = "⚠️ Please configure an active Local Visual AI (LLaVA) in Settings." };

            var modelId = string.IsNullOrWhiteSpace(configData.ModelId) ? "llava:7b" : configData.ModelId;
            var apiBase = string.IsNullOrWhiteSpace(configData.ApiBase) ? "http://localhost:11434/v1" : configData.ApiBase;
            var apiKey = string.IsNullOrWhiteSpace(configData.ApiKey) ? "ollama" : configData.ApiKey.Trim();

            var client = new ChatClient(
                model: modelId,
                credential: new ApiKeyCredential(apiKey),
                options: new OpenAIClientOptions { Endpoint = new Uri(apiBase) });

            var imagePart = ChatMessageContentPart.CreateImagePart(
                new BinaryData(imageData),
                "image/jpeg");
            var textPart = ChatMessageContentPart.CreateTextPart(prompt);

            _history.Clear();
            _history.Add(new UserChatMessage(new List<ChatMessageContentPart> { imagePart, textPart }));

            var parameters = await _configService.GetParametersAsync(configData.Id);
            var options = new ChatCompletionOptions
            {
                Temperature = parameters != null ? (float)parameters.Temperature : 0.4f,
                MaxOutputTokenCount = parameters?.MaxTokens ?? 2048
            };

            var result = await client.CompleteChatAsync(_history, options);
            var completion = result.Value;

            var answer = completion.Content?.FirstOrDefault()?.Text ?? "No response generated.";
            return new FuzzResponse { Answer = answer };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Local Visual Service Error");
            return new FuzzResponse { Answer = $"A technical error occurred: {ex.Message}" };
        }
    }

    public void ClearHistory() => _history.Clear();
}
