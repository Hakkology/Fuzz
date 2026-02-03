using Fuzz.Domain.Services.Interfaces;
using Google.GenAI.Types;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace Fuzz.Domain.Services.Tools;

public class SqlGeneratorAiTool : IAiTool
{
    public string? LastQuery { get; private set; }

    public FunctionDeclaration GetDefinition()
    {
        return new FunctionDeclaration
        {
            Name = "GenerateSqlTool",
            Description = "Use this tool to GENERATE a PostgreSQL SQL query based on the user's request. This tool will NOT execute the query, only record it for tuning.",
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
                            Description = "The PostgreSQL SQL query to generate."
                        }
                    },
                    {
                        "explanation",
                        new Schema
                        {
                            Type = Google.GenAI.Types.Type.STRING,
                            Description = "A brief explanation of what this SQL query does."
                        }
                    }
                },
                Required = new List<string> { "sql" }
            }
        };
    }

    public Task<object> ExecuteAsync(Dictionary<string, object?> args, string userId)
    {
        if (args.TryGetValue("sql", out var sqlObj) && sqlObj != null)
        {
            LastQuery = sqlObj.ToString();
            var explanation = args.TryGetValue("explanation", out var expObj) ? expObj?.ToString() : "";
            
            return Task.FromResult<object>($"SUCCESS: SQL recorded. TASK COMPLETE. Reply with a short confirmation like 'Sorguyu hazırladım.' and STOP.");
        }

        return Task.FromResult<object>("Error: No SQL query provided.");
    }

    public string? CheckGuardrails(Dictionary<string, object?> args) => null;
}
