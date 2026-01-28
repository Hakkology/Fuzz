using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Fuzz.Domain.Data;
using Fuzz.Domain.Services.Plugins;
using System.Text.Json;

namespace Fuzz.Domain.Services;

public interface IFuzzAgentService
{
    Task<FuzzResponse> ProcessCommandAsync(string input, string userId);
    void ClearHistory();
}

public class FuzzAgentService : IFuzzAgentService
{
    private readonly IDbContextFactory<FuzzDbContext> _dbFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FuzzAgentService> _logger;
    private readonly FuzzSqlPlugin _sqlPlugin;
    private readonly List<Content> _history = new();

    public FuzzAgentService(
        IDbContextFactory<FuzzDbContext> dbFactory, 
        IConfiguration configuration,
        ILogger<FuzzAgentService> logger)
    {
        _dbFactory = dbFactory;
        _configuration = configuration;
        _logger = logger;
        _sqlPlugin = new FuzzSqlPlugin(_configuration);
    }

    private async Task<string?> GetUserKeyAsync(string userId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var entry = await db.Keys.FirstOrDefaultAsync(k => k.UserId == userId);
        return entry?.GeminiApiKey;
    }

    public async Task<FuzzResponse> ProcessCommandAsync(string input, string userId)
    {
        try 
        {
            _sqlPlugin.UserId = userId;
            _sqlPlugin.LastQuery = null;

            string? apiKey = await GetUserKeyAsync(userId);
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return new FuzzResponse { Answer = "⚠️ Lütfen 'AI Ayarları' sayfasından Gemini API anahtarınızı girin." };
            }

            // Google.GenAI Client initialization
            var client = new Client(apiKey: apiKey.Trim());
            
            // System prompt initialization in history
            if (_history.Count == 0 || (_history[0].Parts.Count > 0 && _history[0].Parts[0].Text != null && !_history[0].Parts[0].Text!.Contains(userId)))
            {
                _history.Clear();
                _history.Add(new Content { Role = "user", Parts = new List<Part> { new Part { Text = $@"Sen Fuzz Agent'sın. PostgreSQL uzmanısın.
KULLANICI_ID: '{userId}'
TABLO: ""FuzzTodos"" (""Id"", ""Title"", ""IsCompleted"", ""UserId"")
KURALLAR: Tablo ve kolon adlarını MUTLAKA çift tırnak içinde yaz: ""FuzzTodos"", ""Title"".
Sorguda mutlaka ""UserId"" = '{userId}' filtresi olmalı.
ExecuteSql fonksiyonunu kullanarak veriye ulaş ve sonucu Türkçe akıcı bir dille özetle." } } });
                _history.Add(new Content { Role = "model", Parts = new List<Part> { new Part { Text = "Anlaşıldı. PostgreSQL uzmanı olarak veritabanı işlemlerinizde yardıma hazırım." } } });
            }

            _history.Add(new Content { Role = "user", Parts = new List<Part> { new Part { Text = input } } });

            // Define tools for the new SDK - Use uppercase enum values for 0.13.1
            var tools = new List<Tool>
            {
                new Tool
                {
                    FunctionDeclarations = new List<FunctionDeclaration>
                    {
                        new FunctionDeclaration
                        {
                            Name = "ExecuteSql",
                            Description = "Executes a raw PostgreSQL query.",
                            Parameters = new Schema
                            {
                                Type = Google.GenAI.Types.Type.OBJECT, 
                                Properties = new Dictionary<string, Schema>
                                {
                                    { "sql", new Schema { Type = Google.GenAI.Types.Type.STRING, Description = "The raw SQL query. EVERY query MUST include 'UserId' filter." } }
                                },
                                Required = new List<string> { "sql" }
                            }
                        }
                    }
                }
            };

            var config = new GenerateContentConfig
            {
                Tools = tools,
                Temperature = 0.1f
            };

            string finalAnswer = "";
            bool continueLoop = true;
            int maxIterations = 5;

            while (continueLoop && maxIterations-- > 0)
            {
                var response = await client.Models.GenerateContentAsync(
                    model: "gemini-3-flash-preview",
                    contents: _history,
                    config: config);

                if (response.Candidates == null || response.Candidates.Count == 0)
                    break;

                var candidate = response.Candidates[0];
                if (candidate.Content != null)
                {
                    _history.Add(candidate.Content);

                    var functionCalls = candidate.Content.Parts?.Where(p => p.FunctionCall != null).ToList();

                    if (functionCalls != null && functionCalls.Any())
                    {
                        var responseParts = new List<Part>();
                        foreach (var part in functionCalls)
                        {
                            var call = part.FunctionCall;
                            if (call != null && call.Name == "ExecuteSql")
                            {
                                string sql = "";
                                if (call.Args != null && call.Args.TryGetValue("sql", out var sqlObj))
                                    sql = sqlObj?.ToString() ?? "";
                                
                                var result = await _sqlPlugin.ExecuteSqlAsync(sql);
                                
                                responseParts.Add(new Part
                                {
                                    FunctionResponse = new FunctionResponse
                                    {
                                        Name = "ExecuteSql",
                                        Response = new Dictionary<string, object> { { "result", result } }
                                    }
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
                else
                {
                    continueLoop = false;
                }
            }

            if (_history.Count > 15) _history.RemoveRange(2, 2);

            return new FuzzResponse { Answer = finalAnswer, LastSql = _sqlPlugin.LastQuery };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent hatası");
            return new FuzzResponse { Answer = $"Bir teknik hata oluştu: {ex.Message}. Lütfen API Anahtarınızı kontrol edin." };
        }
    }

    public void ClearHistory() => _history.Clear();
}
