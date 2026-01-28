namespace Fuzz.Domain.Services;

public class FuzzResponse
{
    public string Answer { get; set; } = string.Empty;
    public string? LastSql { get; set; }
}
