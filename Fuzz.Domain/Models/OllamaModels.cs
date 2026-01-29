using System.Text.Json.Serialization;

namespace Fuzz.Domain.Models;

public class OllamaTagsResponse
{
    [JsonPropertyName("models")]
    public List<OllamaModel>? Models { get; set; }
}

public class OllamaModel
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("details")]
    public OllamaModelDetails? Details { get; set; }
}

public class OllamaModelDetails
{
    [JsonPropertyName("families")]
    public List<string>? Families { get; set; }

    [JsonPropertyName("family")]
    public string? Family { get; set; }
}
