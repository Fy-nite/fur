using System.Text.Json;
using Fur.Models;

namespace Fur.Services;

public class ApiService : IDisposable
{
    private readonly HttpClient _httpClient;
    // Changed to HTTP for local development - update this when deploying
    private const string BaseUrl = "http://localhost:5001"; 

    public ApiService()
    {
        _httpClient = new HttpClient();
        // Add timeout to prevent hanging
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<FurConfig?> GetPackageInfoAsync(string packageName, string? version = null)
    {
        var url = version != null 
            ? $"{BaseUrl}/api/v1/packages/{packageName}/{version}"
            : $"{BaseUrl}/api/v1/packages/{packageName}";

        try
        {
            Console.WriteLine($"Requesting: {url}");
            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<FurConfig>(json);
            }
            else
            {
                Console.WriteLine($"API returned {response.StatusCode}: {response.ReasonPhrase}");
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Network error: {ex.Message}");
            Console.WriteLine("Make sure the FUR API server is running on http://localhost:5000");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching package info: {ex.Message}");
        }

        return null;
    }

    public async Task<PackageListResponse?> GetPackagesAsync(string? sort = null)
    {
        var url = $"{BaseUrl}/api/v1/packages";
        if (!string.IsNullOrEmpty(sort))
        {
            url += $"?sort={sort}";
        }

        try
        {
            Console.WriteLine($"Requesting: {url}");
            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PackageListResponse>(json);
            }
            else
            {
                Console.WriteLine($"API returned {response.StatusCode}: {response.ReasonPhrase}");
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Network error: {ex.Message}");
            Console.WriteLine("Make sure the FUR API server is running on http://localhost:5000");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching packages: {ex.Message}");
        }

        return null;
    }

    public async Task<PackageListResponse?> SearchPackagesAsync(string query)
    {
        var url = $"{BaseUrl}/api/v1/packages?search={Uri.EscapeDataString(query)}";

        try
        {
            Console.WriteLine($"Requesting: {url}");
            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PackageListResponse>(json);
            }
            else
            {
                Console.WriteLine($"API returned {response.StatusCode}: {response.ReasonPhrase}");
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Network error: {ex.Message}");
            Console.WriteLine("Make sure the FUR API server is running on http://localhost:5000");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error searching packages: {ex.Message}");
        }

        return null;
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
