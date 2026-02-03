using Fuzz.Domain.Entities;
using Fuzz.Domain.Models;
using Fuzz.Domain.Services.AI;
using Fuzz.Domain.Services.Interfaces;
using Fuzz.Domain.Services.Tools;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Text.Json;

namespace Fuzz.Domain.Services;

public class LocalAgentService : IFuzzAgentService
{
    private readonly IAiConfigService _configService;
    private readonly ILogger<LocalAgentService> _logger;
    private readonly IEnumerable<IAiTool> _tools;
    private readonly List<ChatMessage> _history = new();

    public string? LastSql => _tools.OfType<SchemaAiTool>().FirstOrDefault()?.LastQuery 
                              ?? _tools.OfType<SqlGeneratorAiTool>().FirstOrDefault()?.LastQuery;

    public LocalAgentService(
        IAiConfigService configService, 
        ILogger<LocalAgentService> logger,
        IEnumerable<IAiTool> tools)
    {
        _configService = configService;
        _logger = logger;
        _tools = tools;
    }

    public async Task<FuzzResponse> ProcessCommandAsync(string input, string userId, bool useTools = true, string? systemPrompt = null)
    {
        try 
        {
            var configData = await _configService.GetActiveConfigAsync(userId, AiProvider.Local);
            if (configData == null)
                return new FuzzResponse { Answer = "⚠️ Please configure an active Local AI (Ollama) endpoint in the 'AI Settings' page." };

            var client = CreateClient(configData);

            InitializeHistory(userId, useTools, systemPrompt);
            _history.Add(new UserChatMessage(input));

            var options = await BuildOptionsAsync(configData.Id, useTools, forceToolCall: useTools);
            var finalAnswer = await ExecuteAgentLoopAsync(client, options, userId);

            TrimHistory();
            return new FuzzResponse { Answer = finalAnswer, LastSql = LastSql };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Local Agent Execution Error");
            return new FuzzResponse { Answer = $"A technical error occurred: {ex.Message}" };
        }
    }

    public void ClearHistory() => _history.Clear();



    private static ChatClient CreateClient(FuzzAiConfig config)
    {
        var modelId = string.IsNullOrWhiteSpace(config.ModelId) ? "llama3" : config.ModelId;
        var apiBase = string.IsNullOrWhiteSpace(config.ApiBase) ? "http://localhost:11434/v1" : config.ApiBase;
        var apiKey = string.IsNullOrWhiteSpace(config.ApiKey) ? "ollama" : config.ApiKey.Trim();

        return new ChatClient(
            model: modelId, 
            credential: new ApiKeyCredential(apiKey), 
            options: new OpenAIClientOptions { Endpoint = new Uri(apiBase) });
    }

    private void InitializeHistory(string userId, bool useTools, string? systemPrompt = null)
    {
        bool currentIsTools = _history.Count > 0 && _history[0] is SystemChatMessage scm && scm.Content[0].Text.Contains("You are Fuzz");
        
        if (_history.Count == 0 || (currentIsTools != useTools) || systemPrompt != null)
        {
            _history.Clear();
            string prompt = systemPrompt ?? (useTools 
                ? AgentPrompts.GetTaskManagerPrompt(userId, includeExamples: true) 
                : "You are a helpful AI assistant named Fuzz. You can answer questions and chat with the user.");
            _history.Add(new SystemChatMessage(prompt));
        }
    }

    private async Task<ChatCompletionOptions> BuildOptionsAsync(int configId, bool useTools, bool forceToolCall = false)
    {
        var aiParams = await _configService.GetParametersAsync(configId);
        var options = new ChatCompletionOptions
        {
            Temperature = aiParams != null ? (float)aiParams.Temperature : AgentPrompts.DefaultTemperature,
            MaxOutputTokenCount = aiParams?.MaxTokens ?? AgentPrompts.DefaultMaxTokens,
            TopP = aiParams != null ? (float)aiParams.TopP : AgentPrompts.DefaultTopP,
            FrequencyPenalty = aiParams != null ? (float)aiParams.FrequencyPenalty : 0,
            PresencePenalty = aiParams != null ? (float)aiParams.PresencePenalty : 0
        };

        if (useTools)
        {
            foreach (var tool in _tools)
            {
                var def = tool.GetDefinition();
                var toolParams = BinaryData.FromString(JsonSerializer.Serialize(def.Parameters));
                options.Tools.Add(ChatTool.CreateFunctionTool(def.Name, def.Description, toolParams));
            }

            if (forceToolCall)
                options.ToolChoice = ChatToolChoice.CreateRequiredChoice();
        }

        return options;
    }

    private async Task<string> ExecuteAgentLoopAsync(ChatClient client, ChatCompletionOptions options, string userId)
    {
        int iterations = AgentPrompts.MaxIterations;

        while (iterations-- > 0)
        {
            var result = await client.CompleteChatAsync(_history, options);
            var completion = result.Value;

            if (completion.FinishReason == ChatFinishReason.ToolCalls)
            {
                _history.Add(new AssistantChatMessage(completion));
                await ProcessToolCallsAsync(completion.ToolCalls, userId);

                if (completion.ToolCalls.Any(tc => tc.FunctionName == "GenerateSqlTool"))
                {
                    return "Sorguyu tuning için hazırladım.";
                }
                
                // After first tool call, don't force anymore to allow natural response
                options.ToolChoice = null;
            }
            else
            {
                _history.Add(new AssistantChatMessage(completion));
                return completion.Content[0].Text;
            }
        }

        return "İşlem zaman aşımına uğradı.";
    }

    private async Task ProcessToolCallsAsync(IEnumerable<ChatToolCall> toolCalls, string userId)
    {
        foreach (var toolCall in toolCalls)
        {
            var tool = _tools.FirstOrDefault(t => t.GetDefinition().Name == toolCall.FunctionName);
            if (tool != null)
            {
                var args = JsonSerializer.Deserialize<Dictionary<string, object?>>(toolCall.FunctionArguments.ToString()) ?? new();
                var result = await tool.ExecuteAsync(args, userId);
                _history.Add(new ToolChatMessage(toolCall.Id, result?.ToString() ?? ""));
            }
        }
    }

    private void TrimHistory()
    {
        if (_history.Count > AgentPrompts.MaxHistoryCount)
            _history.RemoveRange(1, 2);
    }


}
