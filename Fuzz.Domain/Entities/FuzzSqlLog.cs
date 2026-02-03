using System.ComponentModel.DataAnnotations;

namespace Fuzz.Domain.Entities;

public class FuzzSqlLog
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string InputText { get; set; } = string.Empty;

    [Required]
    public string GeneratedSql { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
