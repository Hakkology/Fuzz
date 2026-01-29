using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fuzz.Domain.Entities;

public class FuzzAiParameters
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int FuzzAiConfigId { get; set; }

    [ForeignKey("FuzzAiConfigId")]
    public virtual FuzzAiConfig? Config { get; set; }

    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 4096;
    public double TopP { get; set; } = 1.0;
    public double FrequencyPenalty { get; set; } = 0.0;
    public double PresencePenalty { get; set; } = 0.0;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
