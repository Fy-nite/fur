using System.Diagnostics;
using System.Text.Json;
using Fur.Models;

namespace Fur.Services;

public class PackageManager
{
    private readonly ApiService _apiService;
    private readonly string _packagesDirectory;

    public PackageManager()
    {
        _apiService = new ApiService();
        _packagesDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".fur", "packages");
        Directory.CreateDirectory(_packagesDirectory);
    }

    public async Task InstallPackageAsync(string packageSpec)
    {
        var (packageName, version) = ParsePackageSpec(packageSpec);
        
        Console.WriteLine($"Installing {packageName}...");
        
        var packageInfo = await _apiService.GetPackageInfoAsync(packageName, version);
        if (packageInfo == null)
        {
            Console.WriteLine($"Package '{packageName}' not found.");
            return;
        }

        // Install dependencies first
        foreach (var dependency in packageInfo.Dependencies)
        {
            Console.WriteLine($"Installing dependency: {dependency}");
            await InstallPackageAsync(dependency);
        }

        // Download and install the package
        await DownloadAndInstallPackageAsync(packageInfo);
        
        Console.WriteLine($"Successfully installed {packageName} v{packageInfo.Version}");
    }

    private async Task DownloadAndInstallPackageAsync(FurConfig packageInfo)
    {
        var packageDir = Path.Combine(_packagesDirectory, packageInfo.Name);
        Directory.CreateDirectory(packageDir);

        // Clone the git repository
        await RunCommandAsync("git", $"clone {packageInfo.Git} {packageDir}");

        // Run the installer script
        if (!string.IsNullOrEmpty(packageInfo.Installer))
        {
            var installerPath = Path.Combine(packageDir, packageInfo.Installer);
            if (File.Exists(installerPath))
            {
                Console.WriteLine($"Running installer: {packageInfo.Installer}");
                await RunCommandAsync("sudo", installerPath);
            }
        }

        // Save package metadata
        var metadataPath = Path.Combine(packageDir, "furconfig.json");
        var json = JsonSerializer.Serialize(packageInfo, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(metadataPath, json);
    }

    public async Task SearchPackagesAsync(string query)
    {
        Console.WriteLine($"Searching for '{query}'...");
        
        var results = await _apiService.SearchPackagesAsync(query);
        if (results == null || (results.Packages.Length == 0 && results.DetailedPackages.Length == 0))
        {
            Console.WriteLine("No packages found.");
            return;
        }

        // If we have detailed packages, show them with full information
        if (results.DetailedPackages.Length > 0)
        {
            Console.WriteLine($"Found {results.PackageCount} packages:");
            foreach (var package in results.DetailedPackages)
            {
                Console.WriteLine($"\n📦 {package.Name} v{package.Version}");
                if (!string.IsNullOrEmpty(package.Description))
                {
                    Console.WriteLine($"   Description: {package.Description}");
                }
                if (package.Authors.Length > 0)
                {
                    Console.WriteLine($"   Authors: {string.Join(", ", package.Authors)}");
                }
                if (package.Dependencies.Length > 0)
                {
                    Console.WriteLine($"   Dependencies: {string.Join(", ", package.Dependencies)}");
                }
                if (!string.IsNullOrEmpty(package.Homepage))
                {
                    Console.WriteLine($"   Homepage: {package.Homepage}");
                }
            }
        }
        // Fallback to simple package names if detailed info isn't available
        else
        {
            Console.WriteLine($"Found {results.PackageCount} packages:");
            foreach (var package in results.Packages)
            {
                Console.WriteLine($"  - {package}");
            }
        }
    }

    public async Task ListPackagesAsync(string? sort = null)
    {
        Console.WriteLine("Fetching package list...");
        
        var results = await _apiService.GetPackagesAsync(sort);
        if (results == null || results.Packages.Length == 0)
        {
            Console.WriteLine("No packages available.");
            return;
        }

        Console.WriteLine($"Available packages ({results.PackageCount} total):");
        foreach (var package in results.Packages)
        {
            Console.WriteLine($"  - {package}");
        }
    }

    public async Task GetPackageInfoAsync(string packageName, string? version = null)
    {
        Console.WriteLine($"Getting info for {packageName}...");
        
        var packageInfo = await _apiService.GetPackageInfoAsync(packageName, version);
        if (packageInfo == null)
        {
            Console.WriteLine($"Package '{packageName}' not found.");
            return;
        }

        Console.WriteLine($"Name: {packageInfo.Name}");
        Console.WriteLine($"Version: {packageInfo.Version}");
        Console.WriteLine($"Authors: {string.Join(", ", packageInfo.Authors)}");
        Console.WriteLine($"Homepage: {packageInfo.Homepage}");
        Console.WriteLine($"Issue Tracker: {packageInfo.IssueTracker}");
        Console.WriteLine($"Git: {packageInfo.Git}");
        Console.WriteLine($"Installer: {packageInfo.Installer}");
        Console.WriteLine($"Dependencies: {string.Join(", ", packageInfo.Dependencies)}");
    }

    private static (string name, string? version) ParsePackageSpec(string packageSpec)
    {
        var parts = packageSpec.Split('@');
        return parts.Length == 2 ? (parts[0], parts[1]) : (parts[0], null);
    }

    private static async Task RunCommandAsync(string command, string arguments)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };

        process.Start();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new Exception($"Command failed: {error}");
        }
    }
}
