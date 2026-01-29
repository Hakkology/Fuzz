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
            var voiceId = string.IsNullOrWhiteSpace(configData.ModelId) ? "21m00Tcm4TlvDq8ikWAM" : configData.ModelId; // Default Rachel voice
            var baseUrl = string.IsNullOrWhiteSpace(configData.ApiBase) ? "https://api.elevenlabs.io/v1" : configData.ApiBase.TrimEnd('/');

            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("xi-api-key", apiKey);

            var requestBody = new
            {
                text = prompt,
                model_id = "eleven_monolingual_v1",
                voice_settings = new
                {
                    stability = 0.5,
                    similarity_boost = 0.5
                }
            };

            var response = await client.PostAsJsonAsync($"{baseUrl}/text-to-speech/{voiceId}", requestBody);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return new FuzzResponse { Answer = $"ElevenLabs Error: {response.StatusCode} - {error}" };
            }

            var audioBytes = await response.Content.ReadAsByteArrayAsync();
            var base64Audio = Convert.ToBase64String(audioBytes);
            
            // Return as data URL format so frontend can play it easily
            return new FuzzResponse { Answer = $"data:audio/mpeg;base64,{base64Audio}" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ElevenLabs Service Error");
            return new FuzzResponse { Answer = $"A technical error occurred: {ex.Message}" };
        }
    }

    public void ClearHistory() { } // TTS is stateless usually
}
