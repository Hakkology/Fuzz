using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Fuzz.Domain.Data;
using Fuzz.Domain.Ai;
using Fuzz.Domain.Entities;
using System.Text.Json;

namespace Fuzz.Domain.Services;

public interface IFuzzAgentService
{
    Task<FuzzResponse> ProcessCommandAsync(string input, string userId);
    void ClearHistory();
    string? LastSql { get; }
}

public class GeminiAgentService : IFuzzAgentService
{
    private readonly IDbContextFactory<FuzzDbContext> _dbFactory;
    private readonly ILogger<GeminiAgentService> _logger;
    private readonly IEnumerable<IAiTool> _tools;
    private readonly List<Content> _history = new();

    public string? LastSql => _tools.OfType<Ai.Tools.SqlAiTool>().FirstOrDefault()?.LastQuery;

    public GeminiAgentService(
        IDbContextFactory<FuzzDbContext> dbFactory, 
        ILogger<GeminiAgentService> logger,
        IEnumerable<IAiTool> tools)
    {
        _dbFactory = dbFactory;
        _logger = logger;
        _tools = tools;
    }

    private async Task<FuzzAiConfig?> GetActiveConfigAsync(string userId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        return await db.AiConfigurations
            .FirstOrDefaultAsync(c => c.UserId == userId && c.IsActive && c.Provider == AiProvider.Gemini);
    }

    public async Task<FuzzResponse> ProcessCommandAsync(string input, string userId)
    {
        try 
        {
            var configData = await GetActiveConfigAsync(userId);
            if (configData == null || string.IsNullOrWhiteSpace(configData.ApiKey))
            {
                return new FuzzResponse { Answer = "⚠️ Lütfen 'AI Ayarları' sayfasından aktif bir Gemini yapılandırması seçin." };
            }

            var client = new Client(apiKey: configData.ApiKey.Trim());
            string modelId = string.IsNullOrWhiteSpace(configData.ModelId) ? "gemini-3-flash-preview" : configData.ModelId;
            
            // Initialization Logic
            if (_history.Count == 0 || (_history[0].Parts.Count > 0 && _history[0].Parts[0].Text != null && !_history[0].Parts[0].Text!.Contains(userId)))
            {
                _history.Clear();
                _history.Add(new Content { Role = "user", Parts = new List<Part> { new Part { Text = $@"Sen Fuzz Agent'sın. PostgreSQL uzmanısın.
KULLANICI_ID: '{userId}'
TABLO: ""FuzzTodos"" (""Id"", ""Title"", ""IsCompleted"", ""UserId"")
KURALLAR: 
1. Tablo/kolon adları çift tırnakta: ""FuzzTodos"".
2. Filtre: ""UserId"" = '{userId}'
3. Araçları kullanarak işlemi yap ve sonucu Türkçe özetle." } } });
                _history.Add(new Content { Role = "model", Parts = new List<Part> { new Part { Text = "Hazırım." } } });
            }

            _history.Add(new Content { Role = "user", Parts = new List<Part> { new Part { Text = input } } });

            // Tools Orchestration (DRY)
            var toolDefinitions = new Tool { FunctionDeclarations = _tools.Select(t => t.GetDefinition()).ToList() };
            var chatConfig = new GenerateContentConfig
            {
                Tools = new List<Tool> { toolDefinitions },
                Temperature = 0.1f,
                MaxOutputTokens = 512
            };

            string finalAnswer = "";
            bool continueLoop = true;
            int maxIterations = 5;

            while (continueLoop && maxIterations-- > 0)
            {
                var response = await client.Models.GenerateContentAsync(
                    model: modelId,
                    contents: _history,
                    config: chatConfig);

                if (response.Candidates == null || response.Candidates.Count == 0) break;

                var candidate = response.Candidates[0];
                if (candidate.Content == null) break;

                _history.Add(candidate.Content);
                var functionCalls = candidate.Content.Parts?.Where(p => p.FunctionCall != null).ToList();

                if (functionCalls != null && functionCalls.Any())
                {
                    var responseParts = new List<Part>();
                    foreach (var part in functionCalls)
                    {
                        var call = part.FunctionCall;
                        var tool = _tools.FirstOrDefault(t => t.GetDefinition().Name == call?.Name);

                        if (tool != null && call != null)
                        {
                            var args = call.Args?.ToDictionary(k => k.Key, v => v.Value) ?? new();
                            var result = await tool.ExecuteAsync(args!, userId);
                            
                            responseParts.Add(new Part
                            {
                                FunctionResponse = new FunctionResponse { Name = call.Name, Response = new Dictionary<string, object> { { "result", result } } }
                            });
                        }
                    }
                    _history.Add(new Content { Role = "user", Parts = responseParts });
                }
                else
                {
                    finalAnswer = candidate.Content.Parts?.FirstOrDefault(p => !string.IsNullOrEmpty(p.Text))?.Text ?? "";
                    continueLoop = false;
                }
            }

            if (_history.Count > 10) _history.RemoveRange(2, 2);

            return new FuzzResponse { Answer = finalAnswer, LastSql = LastSql };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent hatası");
            return new FuzzResponse { Answer = $"Bir teknik hata oluştu: {ex.Message}" };
        }
    }

    public void ClearHistory() => _history.Clear();
}
