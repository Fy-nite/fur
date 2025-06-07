using System.Text.Json.Serialization;

namespace Fur.Models;

public class HealthStatus
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("packageCount")]
    public int PackageCount { get; set; }

    [JsonPropertyName("testingMode")]
    public bool TestingMode { get; set; }

    [JsonPropertyName("database")]
    public string Database { get; set; } = string.Empty;
}
