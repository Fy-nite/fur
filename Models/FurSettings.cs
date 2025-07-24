using System.Text.Json.Serialization;

namespace Fur.Models;

public class FurSettings
{
    [JsonPropertyName("repositories")]
    public string[] RepositoryUrls { get; set; } = new[] { "http://testing.finite.ovh:8080" };
}
