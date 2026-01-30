using Fuzz.Domain.Entities;
using Fuzz.Domain.Models;
using Fuzz.Domain.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fuzz.Domain.Services.AI;

public class ReplicateSoundService : ISoundAgentService
{
    private readonly IAiConfigService _configService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ReplicateSoundService> _logger;

    public ReplicateSoundService(
        IAiConfigService configService,
        IHttpClientFactory httpClientFactory,
        ILogger<ReplicateSoundService> logger)
    {
        _configService = configService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<FuzzResponse> GenerateMusicAsync(string prompt, string userId)
    {
        try
        {
            var configData = await _configService.GetActiveConfigAsync(userId, mode: AiCapabilities.Sound);
            if (configData == null)
                return new FuzzResponse { Answer = "⚠️ Please configure a Replicate API Key in Settings." };

            var apiKey = configData.ApiKey.Trim();
            var modelVersion = string.IsNullOrWhiteSpace(configData.ModelId) 
                ? "meta/musicgen:stereo-melody-large" 
                : configData.ModelId;
            var baseUrl = string.IsNullOrWhiteSpace(configData.ApiBase) 
                ? "https://api.replicate.com/v1" 
                : configData.ApiBase.TrimEnd('/');

            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            client.DefaultRequestHeaders.Add("Prefer", "wait"); // Wait for result

            // Get parameters for duration
            var parameters = await _configService.GetParametersAsync(configData.Id);
            var duration = parameters?.MaxTokens ?? 8; // Use MaxTokens as duration (seconds)
            if (duration > 30) duration = 30; // Cap at 30 seconds
            if (duration < 1) duration = 8;

            var requestBody = new
            {
                version = "671ac645ce5e552cc63a54a2bbff63fcf798043055d2dac5fc9e36a837eedcfb", // musicgen stereo-large
                input = new
                {
                    prompt = prompt,
                    duration = duration,
                    top_k = 250,
                    top_p = 0,
                    temperature = 1,
                    model_version = "stereo-large",
                    output_format = "mp3",
                    continuation = false,
                    multi_band_diffusion = false,
                    normalization_strategy = "peak",
                    classifier_free_guidance = 3
                }
            };

            // Create prediction
            var response = await client.PostAsJsonAsync($"{baseUrl}/predictions", requestBody);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Replicate API error: {StatusCode} - {Error}", response.StatusCode, error);
                return new FuzzResponse { Answer = $"Replicate Error: {response.StatusCode}" };
            }

            var prediction = await response.Content.ReadFromJsonAsync<ReplicatePrediction>();
            
            if (prediction == null)
                return new FuzzResponse { Answer = "Failed to create prediction" };

            // Poll for result if not using "Prefer: wait"
            var maxAttempts = 60;
            var attempt = 0;
            
            while (prediction.Status != "succeeded" && prediction.Status != "failed" && attempt < maxAttempts)
            {
                await Task.Delay(1000);
                
                var pollResponse = await client.GetAsync(prediction.Urls?.Get ?? $"{baseUrl}/predictions/{prediction.Id}");
                if (pollResponse.IsSuccessStatusCode)
                {
                    prediction = await pollResponse.Content.ReadFromJsonAsync<ReplicatePrediction>();
                }
                attempt++;
            }

            if (prediction?.Status == "failed")
            {
                return new FuzzResponse { Answer = $"Music generation failed: {prediction.Error}" };
            }

            if (prediction?.Output == null)
            {
                return new FuzzResponse { Answer = "No audio output received" };
            }

            // Download the audio file and convert to base64
            var audioUrl = prediction.Output.ToString();
            if (string.IsNullOrEmpty(audioUrl))
                return new FuzzResponse { Answer = "Invalid audio URL" };

            var audioResponse = await client.GetAsync(audioUrl);
            if (!audioResponse.IsSuccessStatusCode)
                return new FuzzResponse { Answer = "Failed to download audio" };

            var audioBytes = await audioResponse.Content.ReadAsByteArrayAsync();
            var base64Audio = Convert.ToBase64String(audioBytes);

            return new FuzzResponse { Answer = $"data:audio/mpeg;base64,{base64Audio}" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Replicate Service Error");
            return new FuzzResponse { Answer = $"A technical error occurred: {ex.Message}" };
        }
    }

    public void ClearHistory() { }
}
