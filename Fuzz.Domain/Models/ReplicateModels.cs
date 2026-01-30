using System.Text.Json.Serialization;

namespace Fuzz.Domain.Models;

public class ReplicatePrediction
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("output")]
    public object? Output { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("urls")]
    public ReplicateUrls? Urls { get; set; }
}

public class ReplicateUrls
{
    [JsonPropertyName("get")]
    public string? Get { get; set; }
}
