using System.Diagnostics;
using System.Text.Json;
using Fur.Models;
using Fur.Utils;

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
        
        ConsoleHelper.WriteStep("Installing", packageName + (version != null ? $"@{version}" : ""));
        
        var packageInfo = await _apiService.GetPackageInfoAsync(packageName, version);
        if (packageInfo == null)
        {
            ConsoleHelper.WriteError($"Package '{packageName}' not found");
            return;
        }

        // Install dependencies first
        foreach (var dependency in packageInfo.Dependencies)
        {
            ConsoleHelper.WriteStep("Dependency", dependency);
            await InstallPackageAsync(dependency);
        }

        // Download and install the package
        await DownloadAndInstallPackageAsync(packageInfo);
        
        // Track download
        await _apiService.TrackDownloadAsync(packageName);
        
        ConsoleHelper.WriteSuccess($"Installed ");
        ConsoleHelper.WritePackage(packageName, packageInfo.Version);
        Console.WriteLine();
    }

    private async Task DownloadAndInstallPackageAsync(FurConfig packageInfo)
    {
        var packageDir = Path.Combine(_packagesDirectory, packageInfo.Name);
        
        if (Directory.Exists(packageDir) && Directory.Exists(Path.Combine(packageDir, ".git")))
        {
            ConsoleHelper.WriteStep("Updating", $"{packageInfo.Name} to v{packageInfo.Version}");
            await UpdateExistingPackageAsync(packageDir, packageInfo);
        }
        else
        {
            ConsoleHelper.WriteStep("Cloning", packageInfo.Git);
            Directory.CreateDirectory(packageDir);
            await CloneNewPackageAsync(packageDir, packageInfo);
        }

        // Run the installer script
        if (!string.IsNullOrEmpty(packageInfo.Installer))
        {
            var installerPath = Path.Combine(packageDir, packageInfo.Installer);
            if (File.Exists(installerPath))
            {
                ConsoleHelper.WriteStep("Running", packageInfo.Installer);
                await RunInstallerScript(installerPath, showOutput: true);
            }
        }

        // Save package metadata
        var metadataPath = Path.Combine(packageDir, "furconfig.json");
        var json = JsonSerializer.Serialize(packageInfo, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(metadataPath, json);
    }

    private async Task UpdateExistingPackageAsync(string packageDir, FurConfig packageInfo)
    {
        try
        {
            // Fetch latest changes
            await RunCommandAsync("git", $"-C \"{packageDir}\" fetch --all --tags");
            
            // Try to switch to the specific version (could be tag or branch)
            try
            {
                await RunCommandAsync("git", $"-C \"{packageDir}\" checkout {packageInfo.Version}");
                ConsoleHelper.WriteStep("Switched", $"to version {packageInfo.Version}");
            }
            catch
            {
                // If checkout fails, try with origin/ prefix for remote branches
                try
                {
                    await RunCommandAsync("git", $"-C \"{packageDir}\" checkout origin/{packageInfo.Version}");
                    ConsoleHelper.WriteStep("Switched", $"to remote branch origin/{packageInfo.Version}");
                }
                catch
                {
                    ConsoleHelper.WriteWarning($"Could not find version {packageInfo.Version}, staying on current branch");
                }
            }
            
            // Pull latest changes if we're on a branch (not a specific tag)
            try
            {
                await RunCommandAsync("git", $"-C \"{packageDir}\" pull");
            }
            catch
            {
                // Ignore pull errors (might be on a detached HEAD for tags)
            }
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteWarning($"Failed to update existing package: {ex.Message}");
            ConsoleHelper.WriteInfo("Proceeding with current local version...");
        }
    }

    private async Task CloneNewPackageAsync(string packageDir, FurConfig packageInfo)
    {
        // Clone the git repository
        await RunCommandAsync("git", $"clone {packageInfo.Git} \"{packageDir}\"");

        // Switch to specific version if not the default
        if (!string.IsNullOrEmpty(packageInfo.Version) && packageInfo.Version != "latest")
        {
            try
            {
                await RunCommandAsync("git", $"-C \"{packageDir}\" checkout {packageInfo.Version}");
                ConsoleHelper.WriteStep("Switched", $"to version {packageInfo.Version}");
            }
            catch
            {
                ConsoleHelper.WriteWarning($"Could not find version {packageInfo.Version}, using default branch");
            }
        }
    }

    private async Task RunInstallerScript(string scriptPath)
    {
        await RunInstallerScript(scriptPath, showOutput: false);
    }

    private async Task RunInstallerScript(string scriptPath, bool showOutput)
    {
        var extension = Path.GetExtension(scriptPath).ToLowerInvariant();
        var (command, arguments) = GetShellForScript(extension, scriptPath);

        if (command == null)
        {
            Console.WriteLine($"Unsupported script type: {extension}");
            return;
        }

        await RunCommandAsync(command, arguments, showOutput);
    }

    private static (string? command, string arguments) GetShellForScript(string extension, string scriptPath)
    {
        return extension switch
        {
            ".sh" => ("bash", scriptPath),
            ".ps1" => ("pwsh", $"-ExecutionPolicy Bypass -File \"{scriptPath}\""),
            ".py" => ("python", scriptPath),
            ".js" => ("node", scriptPath),
            ".rb" => ("ruby", scriptPath),
            ".cmd" or ".bat" => (OperatingSystem.IsWindows() ? "cmd" : null, 
                                OperatingSystem.IsWindows() ? $"/c \"{scriptPath}\"" : ""),
            ".exe" => (OperatingSystem.IsWindows() ? scriptPath : null, ""),
            "" => DetermineShellForExtensionlessScript(scriptPath),
            _ => (null, "")
        };
    }

    private static (string? command, string arguments) DetermineShellForExtensionlessScript(string scriptPath)
    {
        // For extensionless scripts, try to read the shebang line
        try
        {
            var firstLine = File.ReadLines(scriptPath).FirstOrDefault();
            if (firstLine?.StartsWith("#!") == true)
            {
                var shebang = firstLine[2..].Trim();
                
                // Common shebang patterns
                if (shebang.Contains("bash") || shebang.Contains("sh"))
                    return ("bash", scriptPath);
                if (shebang.Contains("python"))
                    return ("python", scriptPath);
                if (shebang.Contains("node"))
                    return ("node", scriptPath);
                if (shebang.Contains("ruby"))
                    return ("ruby", scriptPath);
                
                // Use the shebang directly if it's an absolute path
                if (shebang.StartsWith("/") && File.Exists(shebang))
                    return (shebang, scriptPath);
            }
        }
        catch
        {
            // Ignore errors reading the file
        }

        // Default to bash on Unix-like systems, or make executable and run directly
        if (OperatingSystem.IsWindows())
            return (null, "");
        
        // Make the script executable and run it directly
        try
        {
            RunCommandAsync("chmod", $"+x \"{scriptPath}\"").Wait();
            return (scriptPath, "");
        }
        catch
        {
            return ("bash", scriptPath); // Fallback to bash
        }
    }

    public async Task SearchPackagesAsync(string query)
    {
        ConsoleHelper.WriteStep("Searching", $"'{query}'");
        
        var results = await _apiService.SearchPackagesAsync(query);
        if (results == null || (results.Packages.Length == 0 && results.DetailedPackages.Length == 0))
        {
            ConsoleHelper.WriteWarning("No packages found");
            return;
        }

        // If we have detailed packages, show them with full information
        if (results.DetailedPackages.Length > 0)
        {
            ConsoleHelper.WriteHeader($"Found {results.PackageCount} packages");
            foreach (var package in results.DetailedPackages)
            {
                Console.WriteLine();
                Console.Write("📦 ");
                ConsoleHelper.WritePackage(package.Name, package.Version);
                Console.WriteLine();
                
                if (!string.IsNullOrEmpty(package.Description))
                {
                    ConsoleHelper.WriteDim("   ");
                    Console.WriteLine(package.Description);
                }
                if (package.Authors.Length > 0)
                {
                    ConsoleHelper.WriteDim("   Authors: ");
                    Console.WriteLine(string.Join(", ", package.Authors));
                }
                if (package.Dependencies.Length > 0)
                {
                    ConsoleHelper.WriteDim("   Dependencies: ");
                    Console.WriteLine(string.Join(", ", package.Dependencies));
                }
                if (!string.IsNullOrEmpty(package.Homepage))
                {
                    ConsoleHelper.WriteDim("   Homepage: ");
                    Console.WriteLine(package.Homepage);
                }
            }
        }
        // Fallback to simple package names if detailed info isn't available
        else
        {
            ConsoleHelper.WriteHeader($"Found {results.PackageCount} packages");
            foreach (var package in results.Packages)
            {
                Console.Write("  • ");
                ConsoleHelper.WritePackage(package);
                Console.WriteLine();
            }
        }
    }

    public async Task ListPackagesAsync(string? sort = null)
    {
        ConsoleHelper.WriteStep("Fetching", "package list");
        
        var results = await _apiService.GetPackagesAsync(sort);
        if (results == null || results.Packages.Length == 0)
        {
            ConsoleHelper.WriteWarning("No packages available");
            return;
        }

        ConsoleHelper.WriteHeader($"Available packages ({results.PackageCount} total)");
        foreach (var package in results.Packages)
        {
            Console.Write("  • ");
            ConsoleHelper.WritePackage(package);
            Console.WriteLine();
        }
    }

    public async Task ShowStatisticsAsync()
    {
        ConsoleHelper.WriteStep("Fetching", "repository statistics");
        
        var stats = await _apiService.GetStatisticsAsync();
        if (stats == null)
        {
            ConsoleHelper.WriteError("Could not retrieve statistics");
            return;
        }

        ConsoleHelper.WriteHeader("Repository Statistics");
        Console.WriteLine($"📊 Total Packages: {stats.TotalPackages}");
        Console.WriteLine($"✅ Active Packages: {stats.ActivePackages}");
        Console.WriteLine($"⬇️  Total Downloads: {stats.TotalDownloads:N0}");
        Console.WriteLine($"👁️  Total Views: {stats.TotalViews:N0}");
        
        if (stats.PopularAuthors.Length > 0)
        {
            Console.WriteLine($"👥 Popular Authors: {string.Join(", ", stats.PopularAuthors)}");
        }
        
        if (stats.MostDownloaded.Length > 0)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("🔥 Most Downloaded:");
            Console.ResetColor();
            foreach (var package in stats.MostDownloaded.Take(5))
            {
                Console.Write("  • ");
                ConsoleHelper.WritePackage(package.Name);
                ConsoleHelper.WriteDim($" ({package.Downloads:N0} downloads)");
                Console.WriteLine();
            }
        }
        
        if (stats.RecentlyAdded.Length > 0)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("🆕 Recently Added:");
            Console.ResetColor();
            foreach (var package in stats.RecentlyAdded.Take(5))
            {
                Console.Write("  • ");
                ConsoleHelper.WritePackage(package.Name, package.Version);
                Console.WriteLine();
            }
        }
        
        Console.WriteLine();
        ConsoleHelper.WriteDim($"Last Updated: {stats.LastUpdated:yyyy-MM-dd HH:mm}");
        Console.WriteLine();
    }

    public async Task GetPackageInfoAsync(string packageName, string? version = null)
    {
        ConsoleHelper.WriteStep("Getting", $"info for {packageName}");
        
        var packageInfo = await _apiService.GetPackageInfoAsync(packageName, version);
        if (packageInfo == null)
        {
            ConsoleHelper.WriteError($"Package '{packageName}' not found");
            return;
        }

        ConsoleHelper.WriteHeader("Package Information");
        Console.Write("📦 Name: ");
        ConsoleHelper.WritePackage(packageInfo.Name, packageInfo.Version);
        Console.WriteLine();
        Console.WriteLine($"👥 Authors: {string.Join(", ", packageInfo.Authors)}");
        Console.WriteLine($"🏠 Homepage: {packageInfo.Homepage}");
        Console.WriteLine($"🐛 Issue Tracker: {packageInfo.IssueTracker}");
        Console.WriteLine($"📂 Git: {packageInfo.Git}");
        Console.WriteLine($"⚙️  Installer: {packageInfo.Installer}");
        Console.WriteLine($"📋 Dependencies: {string.Join(", ", packageInfo.Dependencies)}");
    }

    private static (string name, string? version) ParsePackageSpec(string packageSpec)
    {
        var parts = packageSpec.Split('@');
        return parts.Length == 2 ? (parts[0], parts[1]) : (parts[0], null);
    }

    private static async Task RunCommandAsync(string command, string arguments)
    {
        await RunCommandAsync(command, arguments, showOutput: false);
    }

    private static async Task RunCommandAsync(string command, string arguments, bool showOutput)
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

        if (showOutput)
        {
            // Read output and error streams concurrently
            var outputTask = Task.Run(async () =>
            {
                while (!process.StandardOutput.EndOfStream)
                {
                    var line = await process.StandardOutput.ReadLineAsync();
                    if (line != null)
                        Console.WriteLine(line);
                }
            });

            var errorTask = Task.Run(async () =>
            {
                while (!process.StandardError.EndOfStream)
                {
                    var line = await process.StandardError.ReadLineAsync();
                    if (line != null)
                        Console.Error.WriteLine(line);
                }
            });

            await Task.WhenAll(outputTask, errorTask);
        }

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var error = showOutput ? $"Command exited with code {process.ExitCode}" 
                                   : await process.StandardError.ReadToEndAsync();
            throw new Exception($"Command failed: {error}");
        }
    }
}
