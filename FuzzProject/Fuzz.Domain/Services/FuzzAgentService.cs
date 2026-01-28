using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Fuzz.Domain.Data;
using Fuzz.Domain.Services.Plugins;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Fuzz.Domain.Services;

public interface IFuzzAgentService
{
    Task<FuzzResponse> ProcessCommandAsync(string input, string userId);
}

public class FuzzAgentService : IFuzzAgentService
{
    private readonly FuzzDbContext _dbContext;
    private readonly Kernel _kernel;
    private readonly FuzzSqlPlugin _sqlPlugin;
    private readonly IConfiguration _configuration;
    private readonly IChatCompletionService _chatCompletion;

    public FuzzAgentService(FuzzDbContext dbContext, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _sqlPlugin = new FuzzSqlPlugin(_configuration);
        
        var builder = Kernel.CreateBuilder();
        builder.AddOpenAIChatCompletion(
            modelId: "llama3.1", 
            apiKey: "ollama", 
            endpoint: new Uri("http://localhost:11434/v1"));

        builder.Plugins.AddFromObject(_sqlPlugin);
        _kernel = builder.Build();
        _chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
    }

    private string GetSchemaDescription()
    {
        var schema = "POSTGRESQL DATABASE SCHEMA:\n";
        foreach (var entityType in _dbContext.Model.GetEntityTypes())
        {
            var tableName = entityType.GetTableName();
            schema += $"\n📋 TABLE: \"{tableName}\"\n";
            foreach (var property in entityType.GetProperties())
            {
                var columnName = property.GetColumnName();
                var isRequired = !property.IsNullable;
                var isPrimaryKey = property.IsPrimaryKey();
                var clrType = property.ClrType.Name;
                
                var markers = new List<string>();
                if (isPrimaryKey) markers.Add("PK");
                if (isRequired) markers.Add("Required");
                
                var markerStr = markers.Count > 0 ? $" [{string.Join(", ", markers)}]" : "";
                schema += $"   • \"{columnName}\" ({clrType}){markerStr}\n";
            }
        }
        return schema;
    }

    public async Task<FuzzResponse> ProcessCommandAsync(string input, string userId)
    {
        try 
        {
            _sqlPlugin.UserId = userId;
            _sqlPlugin.LastQuery = null;
            var schemaInfo = GetSchemaDescription();

            var systemPrompt = $@"Sen Fuzz Agent'sın - kullanıcının görevlerini yöneten akıllı bir asistan.

{schemaInfo}

⚠️ KRİTİK KURALLAR:
1. AKTIF KULLANICI: '{userId}' - TÜM işlemlerde bu ID'yi kullan
2. SQL yazarken tablo ve kolon isimlerini ÇİFT TIRNAK içinde yaz: ""FuzzTodos"", ""Title""
3. WHERE kısıtlaması: HER sorguda ""UserId"" = '{userId}' olmalı
4. INSERT'lerde UserId değeri MUTLAKA eklensin
5. Görev (Todo) tablosu: ""FuzzTodos"" - Kolonlar: ""Id"", ""Title"", ""IsCompleted"", ""UserId""

📝 GÖREV YÖNETİMİ ÖRNEKLERİ:
- Görevleri listele: SELECT ""Id"", ""Title"", ""IsCompleted"" FROM ""FuzzTodos"" WHERE ""UserId"" = '{userId}'
- Yeni görev ekle: INSERT INTO ""FuzzTodos"" (""Title"", ""UserId"") VALUES ('Görev başlığı', '{userId}')
- Görev tamamla: UPDATE ""FuzzTodos"" SET ""IsCompleted"" = true WHERE ""Id"" = X AND ""UserId"" = '{userId}'
- Görev sil: DELETE FROM ""FuzzTodos"" WHERE ""Id"" = X AND ""UserId"" = '{userId}'

🎯 CEVAP FORMATI:
- Kullanıcıya HER ZAMAN Türkçe, samimi ve yardımcı ol
- SQL çalıştırdıktan sonra sonucu AÇIKLA (ham veri değil)
- Örn: 'Görevinizi ekledim!' veya 'Şu an 3 tamamlanmamış göreviniz var:'";

            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage(systemPrompt);
            chatHistory.AddUserMessage(input);

            var settings = new OpenAIPromptExecutionSettings
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                Temperature = 0.3
            };

            var result = await _chatCompletion.GetChatMessageContentAsync(chatHistory, settings, _kernel);
            var content = result.Content ?? "";

            // Fallback: Llama bazen JSON döndürür, bunu işleyelim
            content = await ProcessFallbackJsonAsync(content, userId);

            // Eğer hala ham veri varsa, temizle
            content = CleanupResponse(content);

            return new FuzzResponse 
            { 
                Answer = content, 
                LastSql = _sqlPlugin.LastQuery 
            };
        }
        catch (Exception ex)
        {
            return new FuzzResponse { Answer = $"Bir hata oluştu: {ex.Message}" };
        }
    }

    private async Task<string> ProcessFallbackJsonAsync(string content, string userId)
    {
        // JSON function call formatını kontrol et
        var jsonPattern = @"\{[""']?name[""']?\s*:\s*[""']?FuzzSqlPlugin[^}]*\}";
        var sqlPattern = @"[""']?sql[""']?\s*:\s*[""']([^""']+)[""']";
        
        if (Regex.IsMatch(content, jsonPattern, RegexOptions.IgnoreCase))
        {
            var sqlMatch = Regex.Match(content, sqlPattern, RegexOptions.IgnoreCase);
            if (sqlMatch.Success)
            {
                var sql = sqlMatch.Groups[1].Value;
                sql = sql.Replace("\\\"", "\"").Replace("\\'", "'");
                
                // SQL'i çalıştır
                var sqlResult = await _sqlPlugin.ExecuteSqlAsync(sql);
                
                // Sonuca göre güzel bir yanıt oluştur
                return await GenerateNaturalResponseAsync(sql, sqlResult, userId);
            }
        }
        
        return content;
    }

    private async Task<string> GenerateNaturalResponseAsync(string sql, string sqlResult, string userId)
    {
        var upperSql = sql.Trim().ToUpper();
        
        if (upperSql.StartsWith("INSERT"))
        {
            return "✅ Görevinizi başarıyla ekledim!";
        }
        else if (upperSql.StartsWith("UPDATE"))
        {
            if (sql.Contains("IsCompleted") && sql.Contains("true"))
                return "✅ Görev tamamlandı olarak işaretlendi!";
            return "✅ Güncelleme başarıyla yapıldı!";
        }
        else if (upperSql.StartsWith("DELETE"))
        {
            return "🗑️ Görev başarıyla silindi!";
        }
        else if (upperSql.StartsWith("SELECT"))
        {
            if (sqlResult == "Kayıt bulunamadı.")
                return "📋 Henüz görev bulunmuyor. Yeni bir görev eklemek ister misiniz?";
            
            try
            {
                var data = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(sqlResult);
                if (data != null && data.Count > 0)
                {
                    var response = $"📋 **{data.Count} görev bulundu:**\n\n";
                    foreach (var item in data)
                    {
                        var title = item.ContainsKey("Title") ? item["Title"].GetString() : "Başlıksız";
                        var isCompleted = item.ContainsKey("IsCompleted") && item["IsCompleted"].GetBoolean();
                        var status = isCompleted ? "✅" : "⏳";
                        var id = item.ContainsKey("Id") ? item["Id"].GetInt32().ToString() : "?";
                        response += $"{status} **#{id}** - {title}\n";
                    }
                    return response;
                }
            }
            catch
            {
                // JSON parse edilemezse ham sonucu döndür
            }
            
            return sqlResult;
        }
        
        return sqlResult;
    }

    private string CleanupResponse(string content)
    {
        // Ham JSON'u temizle
        content = Regex.Replace(content, @"\{[""']?name[""']?\s*:.*?\}", "", RegexOptions.Singleline);
        content = content.Trim();
        
        if (string.IsNullOrWhiteSpace(content))
            return "İşlem tamamlandı.";
            
        return content;
    }
}
