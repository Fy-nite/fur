using System.Text.Json.Serialization;

namespace Fur.Models;

public class PackageListResponse
{
    [JsonPropertyName("package_count")]
    public int PackageCount { get; set; }

    [JsonPropertyName("packages")]
    public string[] Packages { get; set; } = Array.Empty<string>();

    [JsonPropertyName("detailed_packages")]
    public PackageSearchResult[] DetailedPackages { get; set; } = Array.Empty<PackageSearchResult>();
}
