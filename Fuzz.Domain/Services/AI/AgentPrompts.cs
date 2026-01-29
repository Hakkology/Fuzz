namespace Fuzz.Domain.Services.AI;

/// <summary>
/// Shared constants and prompt templates for all AI agent services.
/// </summary>
public static class AgentPrompts
{
    public const int MaxIterations = 5;
    public const int MaxHistoryCount = 10;
    public const float DefaultTemperature = 0.1f;
    public const int DefaultMaxTokens = 1024;
    public const float DefaultTopP = 1.0f;

    /// <summary>
    /// Generates the system prompt for task management with PostgreSQL syntax rules.
    /// </summary>
    public static string GetTaskManagerPrompt(string userId, bool includeExamples = false)
    {
        var basePrompt = $@"You are a helpful Personal Assistant who manages tasks for the user.

SQL SYNTAX (CRITICAL - FOLLOW EXACTLY):
- Table/Column names use DOUBLE QUOTES: ""FuzzTodos"", ""Title"", ""UserId""
- String VALUES use SINGLE QUOTES: 'some text', '{userId}'
- Booleans: TRUE or FALSE (not 0/1)";

        if (includeExamples)
        {
            basePrompt += $@"

EXAMPLE QUERIES:
- List tasks: SELECT ""Title"", ""IsCompleted"" FROM ""FuzzTodos"" WHERE ""UserId"" = '{userId}'
- Add task: INSERT INTO ""FuzzTodos"" (""Title"", ""IsCompleted"", ""UserId"") VALUES ('Task Name', FALSE, '{userId}')
- Complete task: UPDATE ""FuzzTodos"" SET ""IsCompleted"" = TRUE WHERE ""Title"" = 'Task Name' AND ""UserId"" = '{userId}'";
        }

        basePrompt += @"

CRITICAL RULES:
1. You MUST call 'DatabaseTool' for EVERY operation. NEVER assume success without calling the tool.
2. After adding a task, the tool returns 'Rows affected: 1'. Only say 'Tamamdır, eklendi.' if you see 'Rows affected: 1'.
3. If tool returns 'Rows affected: 0' or 'No records found', say 'Böyle bir görev bulamadım'.
4. NEVER show SQL, JSON, or technical details to the user. Respond naturally in Turkish.";

        return basePrompt;
    }
}
