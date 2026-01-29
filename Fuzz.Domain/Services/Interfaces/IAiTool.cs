using Google.GenAI.Types;

namespace Fuzz.Domain.Services.Interfaces;

public interface IAiTool
{
    FunctionDeclaration GetDefinition();
    Task<object> ExecuteAsync(Dictionary<string, object?> args, string userId);
    string? CheckGuardrails(Dictionary<string, object?> args);
}
