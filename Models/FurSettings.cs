using System.Text.Json.Serialization;

namespace Fur.Models;

public class FurSettings
{
    [JsonPropertyName("repositories")]
    public string[] RepositoryUrls { get; set; } = new[] { "http://finitenet.runasp.net" };
}
