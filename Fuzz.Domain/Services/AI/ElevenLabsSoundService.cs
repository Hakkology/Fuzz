using Fuzz.Domain.Entities;
using Fuzz.Domain.Models;
using Fuzz.Domain.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Fuzz.Domain.Services.AI;

public class ElevenLabsSoundService : ISoundAgentService
{
    private readonly IAiConfigService _configService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ElevenLabsSoundService> _logger;

    public ElevenLabsSoundService(
        IAiConfigService configService,
        IHttpClientFactory httpClientFactory,
        ILogger<ElevenLabsSoundService> logger)
    {
        _configService = configService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<FuzzResponse> GenerateMusicAsync(string prompt, string userId)
    {
        try
        {
            var configData = await _configService.GetActiveConfigAsync(userId, AiProvider.ElevenLabs, mode: AiCapabilities.Sound);
            if (configData == null)
                return new FuzzResponse { Answer = "⚠️ Please configure an active ElevenLabs API Key in Settings." };

            var apiKey = configData.ApiKey.Trim();
            var baseUrl = string.IsNullOrWhiteSpace(configData.ApiBase) 
                ? "https://api.elevenlabs.io/v1" 
                : configData.ApiBase.TrimEnd('/');

            // Get duration from parameters (MaxTokens used as milliseconds / 1000)
            var parameters = await _configService.GetParametersAsync(configData.Id);
            var durationMs = (parameters?.MaxTokens ?? 10) * 1000; // Default 10 seconds
            if (durationMs > 60000) durationMs = 60000; // Cap at 60 seconds
            if (durationMs < 5000) durationMs = 10000;

            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("xi-api-key", apiKey);
            client.Timeout = TimeSpan.FromMinutes(3); // Music generation takes time

            var requestBody = new
            {
                prompt = prompt,
                music_length_ms = durationMs
            };

            var response = await client.PostAsJsonAsync($"{baseUrl}/music/compose", requestBody);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("ElevenLabs Music API error: {StatusCode} - {Error}", response.StatusCode, error);
                return new FuzzResponse { Answer = $"ElevenLabs Error: {response.StatusCode} - {error}" };
            }

            // Response is streamed audio chunks
            using var stream = await response.Content.ReadAsStreamAsync();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            
            var audioBytes = memoryStream.ToArray();
            var base64Audio = Convert.ToBase64String(audioBytes);
            
            return new FuzzResponse { Answer = $"data:audio/mpeg;base64,{base64Audio}" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ElevenLabs Music Service Error");
            return new FuzzResponse { Answer = $"A technical error occurred: {ex.Message}" };
        }
    }

    public void ClearHistory() { }
}
