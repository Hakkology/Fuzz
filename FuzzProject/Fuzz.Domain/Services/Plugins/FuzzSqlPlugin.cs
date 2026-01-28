using Npgsql;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Fuzz.Domain.Services.Plugins;

public class FuzzSqlPlugin
{
    private readonly string _connectionString;
    public string UserId { get; set; } = string.Empty;
    public string? LastQuery { get; set; }

    public FuzzSqlPlugin(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new Exception("Conn string not found.");
    }

    public async Task<string> ExecuteSqlAsync(string sql)
    {
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
