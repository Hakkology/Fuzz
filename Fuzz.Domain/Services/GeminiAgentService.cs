using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Fuzz.Domain.Data;
using Fuzz.Domain.Models;
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
    private readonly IAiConfigService _configService;
    private readonly ILogger<GeminiAgentService> _logger;
    private readonly IEnumerable<IAiTool> _tools;
    private readonly List<Content> _history = new();

    public string? LastSql => _tools.OfType<Ai.Tools.SqlAiTool>().FirstOrDefault()?.LastQuery;

    public GeminiAgentService(
        IAiConfigService configService, 
        ILogger<GeminiAgentService> logger,
        IEnumerable<IAiTool> tools)
    {
        _configService = configService;
        _logger = logger;
        _tools = tools;
    }

    public async Task<FuzzResponse> ProcessCommandAsync(string input, string userId)
    {
        try 
        {
            var configData = await _configService.GetActiveConfigAsync(userId, AiProvider.Gemini);
            if (configData == null || string.IsNullOrWhiteSpace(configData.ApiKey))
            {
                return new FuzzResponse { Answer = "⚠️ Please configure an active Gemini API key in the 'AI Settings' page." };
            }

            var client = new Client(apiKey: configData.ApiKey.Trim());
            string modelId = string.IsNullOrWhiteSpace(configData.ModelId) ? "gemini-1.5-flash" : configData.ModelId;
            
            if (_history.Count == 0 || (_history[0].Parts.Count > 0 && _history[0].Parts[0].Text != null && !_history[0].Parts[0].Text!.Contains(userId)))
            {
                _history.Clear();
                _history.Add(new Content { Role = "user", Parts = new List<Part> { new Part { Text = $@"You are Fuzz Agent, a PostgreSQL expert.
USER_ID: '{userId}'
TABLE: ""FuzzTodos"" (""Id"", ""Title"", ""IsCompleted"", ""UserId"")
RULES: 
1. Use double quotes for table/column names: ""FuzzTodos"".
2. Always filter by ""UserId"" = '{userId}'.
3. Perform the requested operation and summarize the results in Turkish." } } });
                _history.Add(new Content { Role = "model", Parts = new List<Part> { new Part { Text = "Ready." } } });
            }

            _history.Add(new Content { Role = "user", Parts = new List<Part> { new Part { Text = input } } });

            var parameters = await _configService.GetParametersAsync(configData.Id);

            var toolDefinitions = new Tool { FunctionDeclarations = _tools.Select(t => t.GetDefinition()).ToList() };
            var chatConfig = new GenerateContentConfig
            {
                Tools = new List<Tool> { toolDefinitions },
                Temperature = parameters != null ? (float)parameters.Temperature : 0.1f,
                MaxOutputTokens = parameters != null ? parameters.MaxTokens : 1024,
                TopP = parameters != null ? (float)parameters.TopP : 1.0f
            };
            
            // Note: Frequency/Presence penalty not directly supported in GenAI SDK yet

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
            _logger.LogError(ex, "Agent Execution Error");
            return new FuzzResponse { Answer = $"A technical error occurred: {ex.Message}" };
        }
    }

    public void ClearHistory() => _history.Clear();
}
