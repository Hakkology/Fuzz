using Google.GenAI.Types;

namespace Fuzz.Domain.Ai;

public interface IAiTool
{
    FunctionDeclaration GetDefinition();
    Task<object> ExecuteAsync(Dictionary<string, object?> args, string userId);
}
