# FUR - Finite User Repository Package Manager

FUR is a lightweight, cross-platform package manager designed for easy installation and management of software packages from git repositories.

## Features

- 📦 **Package Installation**: Install packages directly from git repositories
- 🔍 **Package Search**: Search for packages by name or description
- 📋 **Package Listing**: Browse all available packages with sorting options
- ℹ️ **Package Information**: Get detailed information about specific packages
- 🔗 **Dependency Management**: Automatic dependency resolution and installation
- 🛠️ **Custom Installers**: Support for package-specific installation scripts

## Installation

### Prerequisites

- .NET 8.0 Runtime
- Git (for cloning repositories)
- sudo access (for running installer scripts on Unix systems)

### Build from Source

```bash
git clone <fur-repository-url>
cd fur
dotnet build --configuration Release
```

## Usage

### Install a Package

Install the latest version of a package:
```bash
fur install package-name
```

Install a specific version:
```bash
fur install package-name@1.0.0
```

### Search for Packages

Search for packages by name or description:
```bash
fur search "web server"
```

### List All Packages

List all available packages:
```bash
fur list
```

List packages with sorting:
```bash
fur list --sort mostDownloads
fur list --sort recentlyUpdated
```

### Get Package Information

Get detailed information about a package:
```bash
fur info package-name
```

Get information about a specific version:
```bash
fur info package-name --version 1.0.0
```

## Package Configuration

Packages are configured using a `furconfig.json` file in the repository root:

```json
{
  "name": "my-package",
  "version": "1.0.0",
  "description": "A sample package",
  "authors": ["John Doe", "Jane Smith"],
  "homepage": "https://example.com",
  "issue_tracker": "https://github.com/user/repo/issues",
  "git": "https://github.com/user/repo.git",
  "installer": "install.sh",
  "dependencies": ["dependency1", "dependency2"]
}
```

### Configuration Fields

- **name**: Package name (required)
- **version**: Package version (required)
- **description**: Brief package description
- **authors**: List of package authors
- **homepage**: Package homepage URL
- **issue_tracker**: URL for reporting issues
- **git**: Git repository URL (required)
- **installer**: Path to installation script (optional)
- **dependencies**: List of package dependencies

## Custom Package Sources

You can specify multiple repository API URLs in a `fursettings.json` file in the application directory:

```json
{
  "repositories": [
    "http://localhost:5001",
    "http://testing.finite.ovh:8080"
  ]
}
```

FUR will check each repository in order when searching for packages.

## Package Management Commands

- **Install**: `fur install package-name[@version]`
- **Upgrade**: `fur upgrade package-name[@version]`
- **Downgrade**: `fur downgrade package-name@older-version`
- **Uninstall**: `fur uninstall package-name`

## API Integration

FUR integrates with a REST API server for package management. The default configuration points to `http://localhost:5001`.

### API Endpoints

- `GET /api/v1/packages` - List all packages
- `GET /api/v1/packages?search=query` - Search packages
- `GET /api/v1/packages?sort=method` - List packages with sorting
- `GET /api/v1/packages/{name}` - Get latest package info
- `GET /api/v1/packages/{name}/{version}` - Get specific version info

## Development

### Project Structure

```
d:\fur\
├── Services/
│   ├── PackageManager.cs    # Core package management logic
│   └── ApiService.cs        # API communication service
├── Models/
│   ├── FurConfig.cs         # Package configuration model
│   ├── PackageSearchResult.cs
│   └── PackageListResponse.cs
├── Program.cs               # CLI entry point
├── fur.csproj              # Project configuration
└── README.md               # This file
```

### Dependencies

- **System.CommandLine**: Command-line interface framework
- **.NET 8.0**: Runtime framework

### Building

```bash
# Development build
dotnet build

# Release build
dotnet build --configuration Release

# Run the application
dotnet run -- [command] [arguments]
```

### Testing Commands

```bash
# Test package installation
dotnet run -- install test-package

# Test package search
dotnet run -- search "test"

# Test package listing
dotnet run -- list

# Test package info
dotnet run -- info test-package
```

## Package Storage

Packages are stored in the user's home directory:
- **Windows**: `%USERPROFILE%\.fur\packages\`
- **Unix/Linux**: `~/.fur/packages/`

Each package is stored in its own subdirectory with:
- Cloned git repository
- `furconfig.json` metadata file

## Error Handling

FUR provides detailed error messages for common issues:
- Network connectivity problems
- Missing packages
- Invalid package configurations
- Installation script failures

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test thoroughly
5. Submit a pull request

## License

This project is licensed under the AGPL-3.0 License. 
## Support

For issues and questions:
- Create an issue in the repository
- Check the issue tracker URL in package configurations
- Review the API server logs for debugging

---

**Note**: Make sure the FUR API server is running on `http://localhost:5001` before using the package manager for now while it's in development. The API server will be included in future releases.
