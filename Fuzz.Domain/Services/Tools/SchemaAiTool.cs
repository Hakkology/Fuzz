using Fuzz.Domain.Services.Interfaces;
using Google.GenAI.Types;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Text;
using System.Text.Json;

namespace Fuzz.Domain.Services.Tools;

public class SchemaAiTool : IAiTool
{
    private readonly string _connectionString;
    public string? LastQuery { get; private set; }

    public SchemaAiTool(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DefaultConnection") 
            ?? throw new Exception("Connection string 'DefaultConnection' not found.");
    }

    public FunctionDeclaration GetDefinition()
    {
        return new FunctionDeclaration
        {
            Name = "DatabaseTool",
            Description = "A multi-purpose tool for database operations. Can be used to INSPECT the schema OR EXECUTE safe SQL queries.",
            Parameters = new Schema
            {
                Type = Google.GenAI.Types.Type.OBJECT,
                Properties = new Dictionary<string, Schema>
                {
                    {
                        "sql",
                        new Schema
                        {
                            Type = Google.GenAI.Types.Type.STRING,
                            Description = "Optional. A raw SQL query to execute. MUST include \"UserId\" filter. allowed: SELECT, INSERT, UPDATE, DELETE. forbidden: DROP, ALTER, TRUNCATE."
                        }
                    },
                    {
                        "get_schema",
                        new Schema
                        {
                            Type = Google.GenAI.Types.Type.BOOLEAN,
                            Description = "Optional. Set to true to retrieve the list of tables (starting with 'Fuzz') and their columns."
                        }
                    }
                }
            }
        };
    }

    public string? CheckGuardrails(Dictionary<string, object?> args)
    {
        if (args.TryGetValue("sql", out var sqlObj) && sqlObj != null)
        {
            string sql = sqlObj.ToString()?.Trim().ToUpper() ?? "";
            
            // Allow DELETE, but block structural changes
            var forbiddenWords = new[] { "DROP", "TRUNCATE", "ALTER", "GRANT", "REVOKE", "CREATE", "RENAME", "REPLACE" };

            foreach (var word in forbiddenWords)
            {
                if (sql.Contains(word))
                {
                    return $"Guardrails Alert: Forbidden keyword '{word}' detected. DDL actions are not allowed. Only CRUD (SELECT, INSERT, UPDATE, DELETE) is permitted.";
                }
            }
        }
        return null;
    }

    public async Task<object> ExecuteAsync(Dictionary<string, object?> args, string userId)
    {
        var guardrailError = CheckGuardrails(args);
        if (guardrailError != null) return guardrailError;

        if (args.TryGetValue("get_schema", out var getSchemaObj) && getSchemaObj?.ToString()?.ToLower() == "true")
        {
            return await GetSchemaAsync();
        }

        if (args.TryGetValue("sql", out var sqlObj) && sqlObj != null)
        {
            return await ExecuteSqlAsync(sqlObj.ToString() ?? "");
        }

        return "Error: Please provide either 'sql' to execute a query OR 'get_schema' to view the database structure.";
    }

    private async Task<string> GetSchemaAsync()
    {
        var schemaInfo = new StringBuilder();
        const string sql = @"
            SELECT table_name, column_name, data_type 
            FROM information_schema.columns 
            WHERE table_schema = 'public' 
              AND table_name LIKE 'Fuzz%'
            ORDER BY table_name, ordinal_position;";

        try
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            string currentTable = "";
            bool isFirst = true;

            while (await reader.ReadAsync())
            {
                var tableName = reader.GetString(0);
                var columnName = reader.GetString(1);
                var dataType = reader.GetString(2);

                if (tableName != currentTable)
                {
                    if (!isFirst) schemaInfo.AppendLine(")");
                    schemaInfo.Append($"Table: {tableName} (");
                    currentTable = tableName;
                    isFirst = false;
                }
                else
                {
                    schemaInfo.Append(", ");
                }

                schemaInfo.Append($"{columnName}: {dataType}");
            }
            if (!isFirst) schemaInfo.AppendLine(")");
            
            if (schemaInfo.Length == 0) return "No matching tables found (Tables starting with 'Fuzz').";

            return schemaInfo.ToString();
        }
        catch (Exception ex)
        {
            return $"Error fetching schema: {ex.Message}";
        }
    }

    private async Task<object> ExecuteSqlAsync(string sql)
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

            if (results.Count == 0)
            {
                return sql.Trim().ToUpper().StartsWith("SELECT") 
                    ? "No records found." 
                    : "Command executed successfully.";
            }

            return JsonSerializer.Serialize(results);
        }
        catch (Exception ex)
        {
            return $"Database Error: {ex.Message}";
        }
    }
}
