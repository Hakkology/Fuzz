using OpenAI;
using OpenAI.Chat;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Fuzz.Domain.Data;
using Fuzz.Domain.Ai;
using Fuzz.Domain.Entities;
using System.Text.Json;
using System.ClientModel;

namespace Fuzz.Domain.Services;

public class LocalAgentService : IFuzzAgentService
{
    private readonly IDbContextFactory<FuzzDbContext> _dbFactory;
    private readonly ILogger<LocalAgentService> _logger;
    private readonly IEnumerable<IAiTool> _tools;
    private readonly List<ChatMessage> _history = new();

    public string? LastSql => _tools.OfType<Ai.Tools.SqlAiTool>().FirstOrDefault()?.LastQuery;

    public LocalAgentService(
        IDbContextFactory<FuzzDbContext> dbFactory, 
        ILogger<LocalAgentService> logger,
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
            .FirstOrDefaultAsync(c => c.UserId == userId && c.IsActive && c.Provider == AiProvider.Local);
    }

    public async Task<FuzzResponse> ProcessCommandAsync(string input, string userId)
    {
        try 
        {
            var configData = await GetActiveConfigAsync(userId);
            if (configData == null)
            {
                return new FuzzResponse { Answer = "⚠️ Lütfen 'AI Ayarları' sayfasından aktif bir Yerel (Ollama vb.) yapılandırması seçin." };
            }

            string modelId = string.IsNullOrWhiteSpace(configData.ModelId) ? "llama3" : configData.ModelId;
            string apiBase = string.IsNullOrWhiteSpace(configData.ApiBase) ? "http://localhost:11434/v1" : configData.ApiBase;
            
            // OpenAI SDK can be directed to a local endpoint
            ChatClient client = new(model: modelId, credential: new ApiKeyCredential(configData.ApiKey.Trim()), options: new OpenAIClientOptions
            {
                Endpoint = new Uri(apiBase)
            });
            
            // Initialization
            if (_history.Count == 0 || (_history[0] is SystemChatMessage scm && !scm.Content[0].Text.Contains(userId)))
            {
                _history.Clear();
                _history.Add(new SystemChatMessage($@"Sen Fuzz Agent'sın. PostgreSQL uzmanısın.
KULLANICI_ID: '{userId}'
TABLO: ""FuzzTodos"" (""Id"", ""Title"", ""IsCompleted"", ""UserId"")
KURALLAR: 
1. Tablo/kolon adları çift tırnakta: ""FuzzTodos"".
2. Filtre: ""UserId"" = '{userId}'
3. Araçları kullanarak işlemi yap ve sonucu Türkçe özetle."));
            }

            _history.Add(new UserChatMessage(input));

            ChatCompletionOptions options = new();
            foreach (var tool in _tools)
            {
                var def = tool.GetDefinition();
                var parameters = BinaryData.FromString(JsonSerializer.Serialize(def.Parameters));
                options.Tools.Add(ChatTool.CreateFunctionTool(def.Name, def.Description, parameters));
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
            _logger.LogError(ex, "Yerel Agent hatası");
            return new FuzzResponse { Answer = $"Yerel model ile iletişim kurulurken bir hata oluştu: {ex.Message}. Lütfen yerel sunucunuzun (Ollama vb.) açık ve OpenAI API uyumlu modda çalıştığından emin olun." };
        }
    }

    public void ClearHistory() => _history.Clear();
}
