using System.ComponentModel.DataAnnotations;

namespace Fuzz.Domain.Entities;

public class FuzzAiModel
{
    [Key]
    public int Id { get; set; }

    [Required]
    public AiProvider Provider { get; set; }

    [Required]
    public string ModelId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public bool IsCustom { get; set; }

    public AiCapabilities Capabilities { get; set; } = AiCapabilities.Text;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
