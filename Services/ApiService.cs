using System.Text.Json;
using Fur.Models;
using Fur.Utils;

namespace Fur.Services;

public class ApiService : IDisposable
{
    private readonly HttpClient _httpClient;
    // Changed to HTTP for local development - update this when deploying
    private const string BaseUrl = "http://testing.finite.ovh:8080"; 

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
            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<FurConfig>(json);
            }
            else
            {
                ConsoleHelper.WriteError($"API returned {response.StatusCode}: {response.ReasonPhrase}");
            }
        }
        catch (HttpRequestException ex)
        {
            ConsoleHelper.WriteError($"Network error: {ex.Message}");
            ConsoleHelper.WriteInfo("Make sure the FUR API server is running");
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"Error fetching package info: {ex.Message}");
        }

        return null;
    }

    public async Task<PackageListResponse?> GetPackagesAsync(string? sort = null, int page = 1, int pageSize = 50)
    {
        var url = $"{BaseUrl}/api/v1/packages?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(sort))
        {
            url += $"&sort={sort}";
        }

        try
        {
            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PackageListResponse>(json);
            }
            else
            {
                await HandleErrorResponse(response);
            }
        }
        catch (HttpRequestException ex)
        {
            ConsoleHelper.WriteError($"Network error: {ex.Message}");
            ConsoleHelper.WriteInfo("Make sure the FUR API server is running");
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"Error fetching packages: {ex.Message}");
        }

        return null;
    }

    public async Task<PackageListResponse?> SearchPackagesAsync(string query, bool includeDetails = true)
    {
        var url = $"{BaseUrl}/api/v1/packages?search={Uri.EscapeDataString(query)}&details={includeDetails.ToString().ToLower()}";

        try
        {
            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PackageListResponse>(json);
            }
            else
            {
                await HandleErrorResponse(response);
            }
        }
        catch (HttpRequestException ex)
        {
            ConsoleHelper.WriteError($"Network error: {ex.Message}");
            ConsoleHelper.WriteInfo("Make sure the FUR API server is running");
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"Error searching packages: {ex.Message}");
        }

        return null;
    }

    public async Task<RepositoryStatistics?> GetStatisticsAsync()
    {
        var url = $"{BaseUrl}/api/v1/packages/statistics";

        try
        {
            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<RepositoryStatistics>(json);
            }
            else
            {
                await HandleErrorResponse(response);
            }
        }
        catch (HttpRequestException ex)
        {
            ConsoleHelper.WriteError($"Network error: {ex.Message}");
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"Error fetching statistics: {ex.Message}");
        }

        return null;
    }

    public async Task TrackDownloadAsync(string packageName)
    {
        var url = $"{BaseUrl}/api/v1/packages/{packageName}/download";

        try
        {
            var response = await _httpClient.PostAsync(url, null);
            // Don't show errors for download tracking - it's not critical
            if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NotFound)
            {
                Console.WriteLine($"Warning: Could not track download for {packageName}");
            }
        }
        catch
        {
            // Silently ignore download tracking errors
        }
    }

    public async Task<HealthStatus?> CheckHealthAsync()
    {
        var url = $"{BaseUrl}/api/v1/health";

        try
        {
            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<HealthStatus>(json);
            }
        }
        catch
        {
            // Silently ignore health check errors
        }

        return null;
    }

    private async Task HandleErrorResponse(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        
        switch (response.StatusCode)
        {
            case System.Net.HttpStatusCode.NotFound:
                ConsoleHelper.WriteError("Package not found");
                break;
            case System.Net.HttpStatusCode.BadRequest:
                ConsoleHelper.WriteError($"Bad request: {content}");
                break;
            case System.Net.HttpStatusCode.Unauthorized:
                ConsoleHelper.WriteWarning("Unauthorized. This operation may require authentication");
                break;
            case System.Net.HttpStatusCode.TooManyRequests:
                ConsoleHelper.WriteWarning("Rate limited. Please try again later");
                break;
            default:
                ConsoleHelper.WriteError($"API returned {response.StatusCode}: {response.ReasonPhrase}");
                if (!string.IsNullOrEmpty(content))
                {
                    ConsoleHelper.WriteDim(content);
                    Console.WriteLine();
                }
                break;
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
