using Fuzz.Domain.Entities;
using Fuzz.Domain.Models;
using Fuzz.Domain.Services.Interfaces;
using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Logging;

namespace Fuzz.Domain.Services.AI;

public class GeminiVisualService : IVisualAgentService
{
    private readonly IAiConfigService _configService;
    private readonly ILogger<GeminiVisualService> _logger;
    private readonly List<Content> _history = new();

    public GeminiVisualService(
        IAiConfigService configService,
        ILogger<GeminiVisualService> logger)
    {
        _configService = configService;
        _logger = logger;
    }

    public async Task<FuzzResponse> ProcessImageAsync(byte[] imageData, string prompt, string userId)
    {
        try
        {
            var configData = await _configService.GetActiveConfigAsync(userId, AiProvider.Gemini, isVisual: true);
            if (configData == null || string.IsNullOrWhiteSpace(configData.ApiKey))
                return new FuzzResponse { Answer = "⚠️ Please configure an active Gemini Visual AI in Settings." };

            var client = new Client(apiKey: configData.ApiKey.Trim());
            var modelId = string.IsNullOrWhiteSpace(configData.ModelId) ? "gemini-2.5-flash" : configData.ModelId;

            var imagePart = new Part
            {
                InlineData = new Blob
                {
                    MimeType = "image/jpeg",
                    Data = imageData
                }
            };

            var textPart = new Part { Text = prompt };

            _history.Clear();
            _history.Add(new Content
            {
                Role = "user",
                Parts = new List<Part> { imagePart, textPart }
            });

            var parameters = await _configService.GetParametersAsync(configData.Id);
            var config = new GenerateContentConfig
            {
                Temperature = parameters != null ? (float)parameters.Temperature : 0.4f,
                MaxOutputTokens = parameters?.MaxTokens ?? 2048
            };

            var response = await client.Models.GenerateContentAsync(
                model: modelId,
                contents: _history,
                config: config);

            var answer = response.Candidates?.FirstOrDefault()?.Content?.Parts?
                .FirstOrDefault(p => !string.IsNullOrEmpty(p.Text))?.Text ?? "No response generated.";

            return new FuzzResponse { Answer = answer };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini Visual Service Error");
            return new FuzzResponse { Answer = $"A technical error occurred: {ex.Message}" };
        }
    }

    public void ClearHistory() => _history.Clear();
}
