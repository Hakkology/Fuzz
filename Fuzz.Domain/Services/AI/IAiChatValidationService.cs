namespace Fuzz.Domain.Services.AI;

public interface IAiChatValidationService
{
    Task<(bool IsValid, string SanitizedInput, string? ErrorMessage)> ValidateAndSanitizeAsync(string input);
}
