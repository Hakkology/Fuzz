using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fuzz.Domain.Entities;

public class FuzzSqlTune
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string InputText { get; set; } = string.Empty;

    [Required]
    public string GeneratedSql { get; set; } = string.Empty;

    public string? CorrectSql { get; set; }

    public bool IsVerified { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
