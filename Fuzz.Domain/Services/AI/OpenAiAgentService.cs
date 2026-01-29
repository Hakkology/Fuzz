using Fuzz.Domain.Entities;
using Fuzz.Domain.Models;
using Fuzz.Domain.Services.Interfaces;
using Fuzz.Domain.Services.Tools;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using System.Text.Json;

namespace Fuzz.Domain.Services;

public class OpenAiAgentService : IFuzzAgentService
{
    private readonly IAiConfigService _configService;
    private readonly ILogger<OpenAiAgentService> _logger;
    private readonly IEnumerable<IAiTool> _tools;
    private readonly List<ChatMessage> _history = new();

    public string? LastSql => _tools.OfType<SchemaAiTool>().FirstOrDefault()?.LastQuery;

    public OpenAiAgentService(
        IAiConfigService configService, 
        ILogger<OpenAiAgentService> logger,
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
            var configData = await _configService.GetActiveConfigAsync(userId, AiProvider.OpenAI);
            if (configData == null || string.IsNullOrWhiteSpace(configData.ApiKey))
            {
                return new FuzzResponse { Answer = "⚠️ Please configure an active OpenAI API key in the 'AI Settings' page." };
            }

            string modelId = string.IsNullOrWhiteSpace(configData.ModelId) ? "gpt-4o" : configData.ModelId;
            var client = new ChatClient(model: modelId, apiKey: configData.ApiKey.Trim());

            if (_history.Count == 0 || (_history[0] is SystemChatMessage scm && !scm.Content[0].Text.Contains(userId)))
            {
                _history.Clear();
                _history.Add(new SystemChatMessage($@"You are a helpful Personal Assistant who manages tasks.
USER_ID: '{userId}'
TABLE: ""FuzzTodos"" (""Title"", ""IsCompleted"")

RULES:
1. You can Add, Delete, or List tasks using the 'DatabaseTool'.
2. NEVER show SQL queries, JSON, or technical details to the user.
3. If the user asks for tasks, use the tool to get them, then list them nicely in Turkish.
4. If the user adds a task, use the tool, then just say 'Tamamdır, eklendi.'.
5. SQL RULES (CRITICAL):
   - ALWAYS use double quotes for table/column names: ""FuzzTodos"", ""UserId"".
   - ALWAYS use TRUE/FALSE for booleans (NOT 0/1).
   - Filter by ""UserId"" = '{userId}'.
6. If a tool returns 'Rows affected: 0', say 'Böyle bir görev bulamadım'."));
            }

            _history.Add(new UserChatMessage(input));

            var aiParams = await _configService.GetParametersAsync(configData.Id);

            ChatCompletionOptions options = new();
            if (aiParams != null)
            {
                options.Temperature = (float)aiParams.Temperature;
                options.MaxOutputTokenCount = aiParams.MaxTokens;
                options.TopP = (float)aiParams.TopP;
                options.FrequencyPenalty = (float)aiParams.FrequencyPenalty;
                options.PresencePenalty = (float)aiParams.PresencePenalty;
            }
            else
            {
                options.Temperature = 0.1f;
                options.MaxOutputTokenCount = 1024;
            }

            foreach (var tool in _tools)
            {
                var def = tool.GetDefinition();
                var toolParams = BinaryData.FromString(JsonSerializer.Serialize(def.Parameters));
                options.Tools.Add(ChatTool.CreateFunctionTool(def.Name, def.Description, toolParams));
            }

            string finalAnswer = "";
            bool continueLoop = true;
            int maxIterations = 5;

            while (continueLoop && maxIterations-- > 0)
            {
                ChatCompletion completion = await client.CompleteChatAsync(_history, options);

                if (completion.FinishReason == ChatFinishReason.ToolCalls)
                {
                    _history.Add(new AssistantChatMessage(completion));
                    
                    foreach (var toolCall in completion.ToolCalls)
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
                else
                {
                    finalAnswer = completion.Content[0].Text;
                    _history.Add(new AssistantChatMessage(completion));
                    continueLoop = false;
                }
            }

            if (_history.Count > 10) _history.RemoveRange(1, 2);

            return new FuzzResponse { Answer = finalAnswer, LastSql = LastSql };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI Agent Error");
            return new FuzzResponse { Answer = $"A technical error occurred: {ex.Message}" };
        }
    }

    public void ClearHistory() => _history.Clear();
}
