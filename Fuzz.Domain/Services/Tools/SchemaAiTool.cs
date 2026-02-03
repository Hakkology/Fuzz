using Fuzz.Domain.Services.Interfaces;
using Google.GenAI.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Npgsql;
using System.Text;
using System.Text.Json;

namespace Fuzz.Domain.Services.Tools;

public class SchemaAiTool : IAiTool
{
    private readonly string _connectionString;
    private readonly IMemoryCache _memoryCache;
    public string? LastQuery { get; private set; }

    public SchemaAiTool(IConfiguration config, IMemoryCache memoryCache)
    {
        _connectionString = config.GetConnectionString("DefaultConnection") 
            ?? throw new Exception("Connection string 'DefaultConnection' not found.");
        _memoryCache = memoryCache;
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
                            Description = "SQL query. SYNTAX: Double quotes for names (\"Table\"), SINGLE quotes for values ('text'). Example: SELECT * FROM \"TableName\" WHERE \"ColumnName\" = 'value'"
                        }
                    },
                    {
                        "get_schema",
                        new Schema
                        {
                            Type = Google.GenAI.Types.Type.BOOLEAN,
                            Description = "Optional. Set to true to retrieve the list of tables and their columns."
                        }
                    },
                    {
                        "execute",
                        new Schema
                        {
                            Type = Google.GenAI.Types.Type.BOOLEAN,
                            Description = "Optional. Set to true to EXECUTE the SQL, false to ONLY GENERATE the SQL string without running it. Default is true."
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
            
            var forbiddenWords = new[] { "DROP", "TRUNCATE", "ALTER", "GRANT", "REVOKE", "CREATE", "RENAME", "REPLACE" };

            foreach (var word in forbiddenWords)
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(sql, $@"\b{word}\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                {
                    return $"Guardrails Alert: Forbidden keyword '{word}' detected. DDL actions are not allowed. Only CRUD (SELECT, INSERT, UPDATE, DELETE) is permitted.";
                }
            }

            if (sql.Contains("DELETE") && !sql.Contains("WHERE"))
                return "Guardrails Alert: DELETE statement must contain a WHERE clause.";
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
            bool shouldExecute = true;
            if (args.TryGetValue("execute", out var executeObj) && executeObj?.ToString()?.ToLower() == "false")
            {
                shouldExecute = false;
            }

            return await ExecuteSqlAsync(sqlObj.ToString() ?? "", userId, shouldExecute);
        }

        return "Error: Please provide either 'sql' to execute a query OR 'get_schema' to view the database structure.";
    }

    private async Task<string> GetSchemaAsync()
    {
        return await _memoryCache.GetOrCreateAsync("DatabaseSchema_Public", async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromHours(1);
            return await FetchSchemaFromDbAsync();
        }) ?? "Error: Failed to retrieve schema.";
    }

    private async Task<string> FetchSchemaFromDbAsync()
    {
        var schemaInfo = new StringBuilder();
        const string sql = @"
            SELECT table_name, column_name, data_type 
            FROM information_schema.columns 
            WHERE table_schema = 'public' 
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
            
            if (schemaInfo.Length == 0) return "No tables found in the public schema.";

            return schemaInfo.ToString();
        }
        catch (Exception ex)
        {
            return $"Error fetching schema: {ex.Message}";
        }
    }

    private async Task<object> ExecuteSqlAsync(string sql, string userId, bool execute = true)
    {
        LastQuery = sql;
        if (!execute)
        {
            return $"SQL Generated (Not Executed): {sql}";
        }

        try
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(sql, conn);
            bool isSelect = sql.Trim().ToUpper().StartsWith("SELECT") || sql.Trim().ToUpper().StartsWith("WITH");

            if (isSelect)
            {
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

                if (results.Count == 0) return $"No records found. DEBUG INFO: Conn='{_connectionString}', User='{userId}', SQL='{sql}'";

                return JsonSerializer.Serialize(results);
            }
            else
            {
                // For INSERT, UPDATE, DELETE, use ExecuteNonQuery to get rows affected
                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                return $"Command executed successfully. Rows affected: {rowsAffected}";
            }
        }
        catch (Exception ex)
        {
            return $"Database Error: {ex.Message}";
        }
    }
}
