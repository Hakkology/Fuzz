using System.Text.RegularExpressions;

namespace Fuzz.Domain.Services.AI;

public class AiChatValidationService : IAiChatValidationService
{
    private const int MaxInputLength = 4000;
    
    // Simple patterns to catch obvious jailbreaks or confusion attempts
    // This is NOT exhaustive, but a basic guardrail.
    private static readonly string[] ForbiddenPatterns = 
    {
        "ignore all previous instructions",
        "system prompt",
        "you are now",
        "simülasyonu sonlandır",
        "dev mode"
    };

    public Task<(bool IsValid, string SanitizedInput, string? ErrorMessage)> ValidateAndSanitizeAsync(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return Task.FromResult((false, "", (string?)"Input cannot be empty."));
        }

        var sanitized = input.Trim();

        if (sanitized.Length > MaxInputLength)
        {
            return Task.FromResult((false, sanitized, (string?)$"Input is too long (Max {MaxInputLength} characters)."));
        }

        // Basic check for injection attempts
        foreach (var pattern in ForbiddenPatterns)
        {
            if (sanitized.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return Task.FromResult((false, sanitized, (string?)"Your message contains restricted patterns and cannot be processed."));
            }
        }

        return Task.FromResult((true, sanitized, (string?)null));
    }
}
