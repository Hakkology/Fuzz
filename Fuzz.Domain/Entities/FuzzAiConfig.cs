using System.ComponentModel.DataAnnotations;

namespace Fuzz.Domain.Entities;

public enum AiProvider
{
    Gemini,
    OpenAI,
    Local,
    ElevenLabs,
    Replicate
}

public class FuzzAiConfig
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public AiProvider Provider { get; set; }

    [Required]
    public string ApiKey { get; set; } = string.Empty;

    public string ModelId { get; set; } = string.Empty;

    public string ApiBase { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public AiCapabilities Mode { get; set; } = AiCapabilities.Text;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual FuzzAiParameters? Parameters { get; set; }
}
