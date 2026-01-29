using Fuzz.Domain.Services.Interfaces;
using Google.GenAI.Types;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Text.Json;

namespace Fuzz.Domain.Ai.Tools;

public class SqlAiTool : IAiTool
{
    private readonly string _connectionString;
    public string? LastQuery { get; private set; }

    public SqlAiTool(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new Exception("Connection string 'DefaultConnection' not found.");
    }

    public FunctionDeclaration GetDefinition()
    {
        return new FunctionDeclaration
        {
            Name = "ExecuteSql",
            Description = "Executes a raw PostgreSQL query on the tasks database.",
            Parameters = new Schema
            {
                Type = Google.GenAI.Types.Type.OBJECT,
                Properties = new Dictionary<string, Schema>
                {
                    { 
                        "sql", 
                        new Schema { 
                            Type = Google.GenAI.Types.Type.STRING, 
                            Description = "The raw SQL query. MUST include filter for \"UserId\"." 
                        } 
                    }
                },
                Required = new List<string> { "sql" }
            }
        };
    }

    public async Task<object> ExecuteAsync(Dictionary<string, object?> args, string userId)
    {
        if (!args.TryGetValue("sql", out var sqlObj) || sqlObj == null)
            return "Hata: 'sql' parametresi eksik.";

        string sql = sqlObj.ToString() ?? "";
        LastQuery = sql;

        try
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            
            var results = new List<Dictionary<string, object>>();
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.GetValue(i);
                }
                results.Add(row);
            }

            if (results.Count == 0 && sql.Trim().ToUpper().StartsWith("SELECT"))
                return "Kayıt bulunamadı.";

            return sql.Trim().ToUpper().StartsWith("SELECT") 
                ? JsonSerializer.Serialize(results) 
                : "İşlem başarıyla tamamlandı.";
        }
        catch (Exception ex)
        {
            return $"Veritabanı Hatası: {ex.Message}";
        }
    }
}
