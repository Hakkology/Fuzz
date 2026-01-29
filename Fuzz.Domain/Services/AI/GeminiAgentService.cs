using Fuzz.Domain.Entities;
using Fuzz.Domain.Models;
using Fuzz.Domain.Services.AI;
using Fuzz.Domain.Services.Interfaces;
using Fuzz.Domain.Services.Tools;
using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Logging;

namespace Fuzz.Domain.Services;

public class GeminiAgentService : IFuzzAgentService
{
    private readonly IAiConfigService _configService;
    private readonly ILogger<GeminiAgentService> _logger;
    private readonly IEnumerable<IAiTool> _tools;
    private readonly List<Content> _history = new();

    public string? LastSql => _tools.OfType<SchemaAiTool>().FirstOrDefault()?.LastQuery;

    public GeminiAgentService(
        IAiConfigService configService, 
        ILogger<GeminiAgentService> logger,
        IEnumerable<IAiTool> tools)
    {
        _configService = configService;
        _logger = logger;
        _tools = tools;
    }

    public async Task<FuzzResponse> ProcessCommandAsync(string input, string userId, bool useTools = true)
    {
        try 
        {
            var configData = await _configService.GetActiveConfigAsync(userId, AiProvider.Gemini);
            if (configData == null || string.IsNullOrWhiteSpace(configData.ApiKey))
                return new FuzzResponse { Answer = "⚠️ Please configure an active Gemini API key in the 'AI Settings' page." };

            var client = new Client(apiKey: configData.ApiKey.Trim());
            var modelId = string.IsNullOrWhiteSpace(configData.ModelId) ? "gemini-1.5-flash" : configData.ModelId;
            
            InitializeHistory(userId, useTools);
            _history.Add(new Content { Role = "user", Parts = new List<Part> { new Part { Text = input } } });

            var chatConfig = await BuildConfigAsync(configData.Id, useTools);
            var finalAnswer = await ExecuteAgentLoopAsync(client, modelId, chatConfig, userId);

            TrimHistory();
            return new FuzzResponse { Answer = finalAnswer, LastSql = LastSql };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini Agent Execution Error");
            return new FuzzResponse { Answer = $"A technical error occurred: {ex.Message}" };
        }
    }

    public void ClearHistory() => _history.Clear();

    #region Private Methods

    private void InitializeHistory(string userId, bool useTools)
    {
        // Simple check if first message exists. A more robust check might verify system prompt content.
        if (_history.Count == 0)
        {
            _history.Clear();
            // Gemini requires alternating user/model roles, start with model
            _history.Add(new Content { Role = "model", Parts = new List<Part> { new Part { Text = "Ready." } } });
            
            string prompt = useTools 
                ? AgentPrompts.GetTaskManagerPrompt(userId)
                : "You are a helpful AI assistant named Fuzz. You can answer questions and chat with the user.";
            _history.Add(new Content { Role = "user", Parts = new List<Part> { new Part { Text = prompt } } });
        }
    }

    private async Task<GenerateContentConfig> BuildConfigAsync(int configId, bool useTools)
    {
        var parameters = await _configService.GetParametersAsync(configId);
        
        var config = new GenerateContentConfig
        {
            Temperature = parameters != null ? (float)parameters.Temperature : AgentPrompts.DefaultTemperature,
            MaxOutputTokens = parameters?.MaxTokens ?? AgentPrompts.DefaultMaxTokens,
            TopP = parameters != null ? (float)parameters.TopP : AgentPrompts.DefaultTopP
        };

        if (useTools)
        {
            var toolDefinitions = new Tool { FunctionDeclarations = _tools.Select(t => t.GetDefinition()).ToList() };
            config.Tools = new List<Tool> { toolDefinitions };
        }

        return config;
    }

    private async Task<string> ExecuteAgentLoopAsync(Client client, string modelId, GenerateContentConfig config, string userId)
    {
        int iterations = AgentPrompts.MaxIterations;

        while (iterations-- > 0)
        {
            var response = await client.Models.GenerateContentAsync(model: modelId, contents: _history, config: config);

            if (response.Candidates == null || response.Candidates.Count == 0) break;
            var candidate = response.Candidates[0];
            if (candidate.Content == null) break;

            _history.Add(candidate.Content);
            var functionCalls = candidate.Content.Parts?.Where(p => p.FunctionCall != null).ToList();

            if (functionCalls != null && functionCalls.Any())
            {
                var responseParts = await ProcessFunctionCallsAsync(functionCalls, userId);
                _history.Add(new Content { Role = "user", Parts = responseParts });
            }
            else
            {
                return candidate.Content.Parts?.FirstOrDefault(p => !string.IsNullOrEmpty(p.Text))?.Text ?? "";
            }
        }

        return "İşlem zaman aşımına uğradı.";
    }

    private async Task<List<Part>> ProcessFunctionCallsAsync(List<Part> functionCalls, string userId)
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
                    FunctionResponse = new FunctionResponse 
                    { 
                        Name = call.Name, 
                        Response = new Dictionary<string, object> { { "result", result } } 
                    }
                });
            }
        }

        return responseParts;
    }

    private void TrimHistory()
    {
        if (_history.Count > AgentPrompts.MaxHistoryCount)
        {
            var systemPrompts = _history.Take(2).ToList(); // model "Ready" + user system prompt
            var recentHistory = _history.TakeLast(AgentPrompts.MaxHistoryCount - 2).ToList();
            _history.Clear();
            _history.AddRange(systemPrompts);
            _history.AddRange(recentHistory);
        }
    }

    #endregion
}
