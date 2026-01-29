using Fuzz.Domain.Entities;
using Fuzz.Domain.Models;
using Fuzz.Domain.Services.Interfaces;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;

namespace Fuzz.Domain.Services.AI;

public class OpenAiVisualService : IVisualAgentService
{
    private readonly IAiConfigService _configService;
    private readonly ILogger<OpenAiVisualService> _logger;
    private readonly List<ChatMessage> _history = new();

    public OpenAiVisualService(
        IAiConfigService configService,
        ILogger<OpenAiVisualService> logger)
    {
        _configService = configService;
        _logger = logger;
    }

    public async Task<FuzzResponse> ProcessImageAsync(byte[] imageData, string prompt, string userId)
    {
        try
        {
            var configData = await _configService.GetActiveConfigAsync(userId, AiProvider.OpenAI, isVisual: true);
            if (configData == null || string.IsNullOrWhiteSpace(configData.ApiKey))
                return new FuzzResponse { Answer = "⚠️ Please configure an active OpenAI Visual AI (GPT-4o) in Settings." };

            var modelId = string.IsNullOrWhiteSpace(configData.ModelId) ? "gpt-4o" : configData.ModelId;
            var client = new ChatClient(model: modelId, apiKey: configData.ApiKey.Trim());

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
            _logger.LogError(ex, "OpenAI Visual Service Error");
            return new FuzzResponse { Answer = $"A technical error occurred: {ex.Message}" };
        }
    }

    public void ClearHistory() => _history.Clear();
}
