using System.Text.Json.Serialization;

namespace Fur.Models;

public class RepositoryStatistics
{
    [JsonPropertyName("totalPackages")]
    public int TotalPackages { get; set; }

    [JsonPropertyName("activePackages")]
    public int ActivePackages { get; set; }

    [JsonPropertyName("totalDownloads")]
    public long TotalDownloads { get; set; }

    [JsonPropertyName("totalViews")]
    public long TotalViews { get; set; }

    [JsonPropertyName("popularAuthors")]
    public string[] PopularAuthors { get; set; } = Array.Empty<string>();

    [JsonPropertyName("mostDownloaded")]
    public PackageStats[] MostDownloaded { get; set; } = Array.Empty<PackageStats>();

    [JsonPropertyName("recentlyAdded")]
    public PackageStats[] RecentlyAdded { get; set; } = Array.Empty<PackageStats>();

    [JsonPropertyName("lastUpdated")]
    public DateTime LastUpdated { get; set; }
}

public class PackageStats
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("downloads")]
    public long Downloads { get; set; }

    [JsonPropertyName("addedDate")]
    public DateTime AddedDate { get; set; }
}
