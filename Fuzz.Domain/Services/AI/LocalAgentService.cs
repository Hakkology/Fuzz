using Fuzz.Domain.Entities;
using Fuzz.Domain.Models;
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

    public string? LastSql => _tools.OfType<SchemaAiTool>().FirstOrDefault()?.LastQuery;

    public LocalAgentService(
        IAiConfigService configService, 
        ILogger<LocalAgentService> logger,
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
            var configData = await _configService.GetActiveConfigAsync(userId, AiProvider.Local);
            if (configData == null)
            {
                return new FuzzResponse { Answer = "⚠️ Please configure an active Local AI (Ollama) endpoint in the 'AI Settings' page." };
            }

            string modelId = string.IsNullOrWhiteSpace(configData.ModelId) ? "llama3" : configData.ModelId;
            string apiBase = string.IsNullOrWhiteSpace(configData.ApiBase) ? "http://localhost:11434/v1" : configData.ApiBase;
            
            string apiKey = string.IsNullOrWhiteSpace(configData.ApiKey) ? "ollama" : configData.ApiKey.Trim();
            var client = new ChatClient(model: modelId, credential: new ApiKeyCredential(apiKey), options: new OpenAIClientOptions
            {
                Endpoint = new Uri(apiBase)
            });

            if (_history.Count == 0 || (_history[0] is SystemChatMessage scm && !scm.Content[0].Text.Contains(userId)))
            {
                _history.Clear();
                _history.Add(new SystemChatMessage($@"You are a helpful Personal Assistant who manages tasks for the user.

SQL SYNTAX (CRITICAL - FOLLOW EXACTLY):
- Table/Column names use DOUBLE QUOTES: ""FuzzTodos"", ""Title"", ""UserId""
- String VALUES use SINGLE QUOTES: 'some text', '{userId}'
- Booleans: TRUE or FALSE (not 0/1)

EXAMPLE QUERIES:
- List tasks: SELECT ""Title"", ""IsCompleted"" FROM ""FuzzTodos"" WHERE ""UserId"" = '{userId}'
- Add task: INSERT INTO ""FuzzTodos"" (""Title"", ""IsCompleted"", ""UserId"") VALUES ('Task Name', FALSE, '{userId}')
- Complete task: UPDATE ""FuzzTodos"" SET ""IsCompleted"" = TRUE WHERE ""Title"" = 'Task Name' AND ""UserId"" = '{userId}'

CRITICAL RULES:
1. You MUST call 'DatabaseTool' for EVERY operation (listing, adding, updating, deleting). NEVER assume success without calling the tool.
2. After adding a task, the tool returns 'Rows affected: 1'. Only say 'Tamamdır, eklendi.' if you see 'Rows affected: 1'.
3. If tool returns 'Rows affected: 0', say 'Bir sorun oluştu, ekleyemedim.'
4. NEVER show SQL to the user. Respond naturally in Turkish."));
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
            
            // Force the model to call a tool instead of generating text
            options.ToolChoice = ChatToolChoice.CreateRequiredChoice();

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
            _logger.LogError(ex, "Local Agent Execution Error");
            return new FuzzResponse { Answer = $"A technical error occurred: {ex.Message}" };
        }
    }

    public void ClearHistory() => _history.Clear();
}
