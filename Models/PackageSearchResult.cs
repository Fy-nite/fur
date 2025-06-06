using System.Text.Json.Serialization;

namespace Fur.Models;

public class PackageSearchResult
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("authors")]
    public string[] Authors { get; set; } = Array.Empty<string>();

    [JsonPropertyName("dependencies")]
    public string[] Dependencies { get; set; } = Array.Empty<string>();

    [JsonPropertyName("homepage")]
    public string Homepage { get; set; } = string.Empty;
}
