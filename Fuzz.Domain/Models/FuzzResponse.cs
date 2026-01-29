namespace Fuzz.Domain.Models;

public class FuzzResponse
{
    public string Answer { get; set; } = string.Empty;
    public string? LastSql { get; set; }
}
