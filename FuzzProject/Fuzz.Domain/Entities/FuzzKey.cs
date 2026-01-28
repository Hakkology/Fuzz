using System.ComponentModel.DataAnnotations;

namespace Fuzz.Domain.Entities;

public class FuzzKey
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string GeminiApiKey { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
