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
}
