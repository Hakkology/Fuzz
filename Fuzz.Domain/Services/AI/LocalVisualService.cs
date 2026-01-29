using Fuzz.Domain.Entities;
using Fuzz.Domain.Models;
using Fuzz.Domain.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fuzz.Domain.Services.AI;

public class LocalVisualService : IVisualAgentService
{
    private readonly IAiConfigService _configService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<LocalVisualService> _logger;

    public LocalVisualService(
        IAiConfigService configService,
        IHttpClientFactory httpClientFactory,
        ILogger<LocalVisualService> logger)
    {
        _configService = configService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<FuzzResponse> ProcessImageAsync(byte[] imageData, string prompt, string userId)
    {
        try
        {
            var configData = await _configService.GetActiveConfigAsync(userId, mode: AiCapabilities.Visual);
            if (configData == null)
                return new FuzzResponse { Answer = "⚠️ Please configure an active Visual AI in Settings." };

            var modelId = string.IsNullOrWhiteSpace(configData.ModelId) ? "moondream:latest" : configData.ModelId;
            var apiBase = string.IsNullOrWhiteSpace(configData.ApiBase) ? "http://localhost:11434" : configData.ApiBase.TrimEnd('/');
            
            // Remove /v1 suffix if present (we use native Ollama API)
            if (apiBase.EndsWith("/v1"))
                apiBase = apiBase[..^3];

            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(2);

            // Convert image to base64
            var imageBase64 = Convert.ToBase64String(imageData);

            // Use native Ollama API for vision models
            var requestBody = new
            {
                model = modelId,
                prompt = prompt,
                images = new[] { imageBase64 },
                stream = false
            };

            var response = await client.PostAsJsonAsync($"{apiBase}/api/generate", requestBody);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Ollama Vision API error: {StatusCode} - {Error}", response.StatusCode, error);
                return new FuzzResponse { Answer = $"Ollama Error: {response.StatusCode}" };
            }

            var result = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>();
            return new FuzzResponse { Answer = result?.Response ?? "No response generated." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Local Visual Service Error");
            return new FuzzResponse { Answer = $"A technical error occurred: {ex.Message}" };
        }
    }

    public void ClearHistory() { }

    private class OllamaGenerateResponse
    {
        [JsonPropertyName("response")]
        public string? Response { get; set; }
    }
}
