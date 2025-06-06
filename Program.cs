using System.CommandLine;
using Fur.Services;
using Fur.Models;

namespace Fur
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {

            var rootCommand = new RootCommand("FUR - Finite User Repository Package Manager");

            // Install command
            var installCommand = new Command("install", "Install a package");
            var packageArg = new Argument<string>("package", "Package name with optional version (name@version)");
            installCommand.AddArgument(packageArg);
            installCommand.SetHandler(async (string package) =>
            {
                var packageManager = new PackageManager();
                await packageManager.InstallPackageAsync(package);
            }, packageArg);

            // Search command
            var searchCommand = new Command("search", "Search for packages");
            var queryArg = new Argument<string>("query", "Search query");
            searchCommand.AddArgument(queryArg);
            searchCommand.SetHandler(async (string query) =>
            {
                var packageManager = new PackageManager();
                await packageManager.SearchPackagesAsync(query);
            }, queryArg);

            // List command
            var listCommand = new Command("list", "List all packages");
            var sortOption = new Option<string>("--sort", "Sort method (mostDownloads, recentlyUpdated, etc.)");
            listCommand.AddOption(sortOption);
            listCommand.SetHandler(async (string sort) =>
            {
                var packageManager = new PackageManager();
                await packageManager.ListPackagesAsync(sort);
            }, sortOption);

            // Info command
            var infoCommand = new Command("info", "Get package information");
            var infoPackageArg = new Argument<string>("package", "Package name");
            var versionOption = new Option<string>("--version", "Specific version");
            infoCommand.AddArgument(infoPackageArg);
            infoCommand.AddOption(versionOption);
            infoCommand.SetHandler(async (string package, string version) =>
            {
                var packageManager = new PackageManager();
                await packageManager.GetPackageInfoAsync(package, version);
            }, infoPackageArg, versionOption);

            rootCommand.AddCommand(installCommand);
            rootCommand.AddCommand(searchCommand);
            rootCommand.AddCommand(listCommand);
            rootCommand.AddCommand(infoCommand);

            return await rootCommand.InvokeAsync(args);
        }
    }
}
