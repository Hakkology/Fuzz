using Fuzz.Domain.Services.Interfaces;
using Google.GenAI.Types;

namespace Fuzz.Domain.Services.Tools;

public class TimeAiTool : IAiTool
{
    public FunctionDeclaration GetDefinition()
    {
        return new FunctionDeclaration
        {
            Name = "GetCurrentTime",
            Description = "Returns the current local time.",
            Parameters = new Schema
            {
                Type = Google.GenAI.Types.Type.OBJECT,
                Properties = new Dictionary<string, Schema>()
            }
        };
    }

    public string? CheckGuardrails(Dictionary<string, object?> args)
    {
        return null; 
    }

    public Task<object> ExecuteAsync(Dictionary<string, object?> args, string userId)
    {
        return Task.FromResult<object>(DateTime.Now.ToString("HH:mm:ss"));
    }
}
