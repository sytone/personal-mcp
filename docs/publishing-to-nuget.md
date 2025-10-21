# Publishing Personal MCP Server to NuGet

This guide covers the complete process of publishing the Personal MCP Server to NuGet.org as a .NET tool.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Building the Package](#building-the-package)
- [Testing the Package Locally](#testing-the-package-locally)
- [Publishing to NuGet.org](#publishing-to-nugetorg)
- [Verifying the Publication](#verifying-the-publication)
- [Updating an Existing Package Manually](#updating-an-existing-package-manually)
- [Troubleshooting](#troubleshooting)

---

## Prerequisites

### Required Tools

- **.NET 10.0 SDK** or later
- **NuGet CLI** (included with .NET SDK)
- **NuGet.org account** - Sign up at <https://www.nuget.org>

### API Key Setup

1. Log in to your NuGet.org account
2. Navigate to your profile → **API Keys**
3. Click **Create** to generate a new API key
4. Configure the key:
   - **Key Name**: `Personal-MCP-Publishing` (or your preference)
   - **Glob Pattern**: `Sytone.Personal.Mcp` (or `*` for all packages)
   - **Scopes**: Select `Push` and `Push new packages and package versions`
   - **Expiration**: Set an appropriate expiration date
5. Copy the API key immediately (it won't be shown again)
6. Store the key securely (consider using a password manager)

---

## Building the Package

### 1. Clean Previous Builds

```powershell
# Navigate to repository root
cd ~/repos/obsidian-mcp

# Clean previous builds
dotnet clean --configuration Release
Remove-Item -Path ./nuget -Recurse -Force -ErrorAction SilentlyContinue
```

### 2. Build in Release Configuration

```powershell
# Build the project
dotnet build --configuration Release
```

Expected output:

```Powershell
Personal.Mcp net9.0 succeeded (X.Xs) → src\Personal.Mcp\bin\Release\net9.0\Personal.Mcp.dll
Build succeeded in X.Xs
```

### 3. Create NuGet Package

```powershell
# Create package
dotnet pack --configuration Release --output ./nuget
# Verify package was created
Get-ChildItem ./src/Personal.Mcp/bin/Release/*.nupkg
```

Expected output:

```Powershell
  Personal.Mcp net9.0 win-x64 succeeded (1.9s) → src\Personal.Mcp\bin\Release\net9.0\win-x64\publish\
  Personal.Mcp net9.0 linux-arm64 succeeded (2.1s) → src\Personal.Mcp\bin\Release\net9.0\linux-arm64\publish\
  Personal.Mcp net9.0 win-arm64 succeeded (2.0s) → src\Personal.Mcp\bin\Release\net9.0\win-arm64\publish\
  Personal.Mcp net9.0 linux-x64 succeeded (2.1s) → src\Personal.Mcp\bin\Release\net9.0\linux-x64\publish\
  Personal.Mcp net9.0 linux-musl-x64 succeeded (2.1s) → src\Personal.Mcp\bin\Release\net9.0\linux-musl-x64\publish\
  Personal.Mcp net9.0 osx-arm64 succeeded (2.2s) → src\Personal.Mcp\bin\Release\net9.0\osx-arm64\publish\
  Personal.Mcp net9.0 linux-arm64 succeeded (5.2s) → src\Personal.Mcp\bin\Release\net9.0\linux-arm64\Personal.Mcp.dll
  Personal.Mcp net9.0 win-arm64 succeeded (5.4s) → src\Personal.Mcp\bin\Release\net9.0\win-arm64\Personal.Mcp.dll
  Personal.Mcp net9.0 linux-musl-x64 succeeded (5.4s) → src\Personal.Mcp\bin\Release\net9.0\linux-musl-x64\Personal.Mcp.dll
  Personal.Mcp net9.0 win-x64 succeeded (5.6s) → src\Personal.Mcp\bin\Release\net9.0\win-x64\Personal.Mcp.dll
  Personal.Mcp net9.0 linux-x64 succeeded (5.4s) → src\Personal.Mcp\bin\Release\net9.0\linux-x64\Personal.Mcp.dll
  Personal.Mcp net9.0 osx-arm64 succeeded (5.4s) → src\Personal.Mcp\bin\Release\net9.0\osx-arm64\Personal.Mcp.dll
  Personal.Mcp net9.0 succeeded (1.1s) → src\Personal.Mcp\bin\Release\net9.0\Personal.Mcp.dll
```

### 4. Inspect Package Contents

```powershell
# Extract and inspect package contents
$pkg = Get-ChildItem ./nuget/*.nupkg | Select-Object -First 1
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead($pkg.FullName)

# List all files
$zip.Entries | Select-Object FullName | Format-Table -AutoSize

# Check for specific files
$zip.Entries | Where-Object { 
    $_.FullName -match "README|server.json|appsettings|.vscode" 
} | Select-Object FullName

$zip.Dispose()
```

**Verify:**

- ✅ `README.md` is included
- ✅ `.mcp/server.json` is included
- ✅ Tool DLLs are in `tools/net9.0/any/`
- ❌ NO `appsettings*.json` files
- ❌ NO `.vscode/` files
- ❌ NO build artifacts (`obj/`, `bin/` internals)

---

## Testing the Package Locally

Before publishing, test the package locally to ensure it works correctly.

### 1. Install Locally

```powershell
# Install from local package
dotnet tool install --global Sytone.Personal.Mcp --add-source ./nuget --version 0.1.0

# Or update if already installed
dotnet tool update --global Sytone.Personal.Mcp --add-source ./nuget --version 0.1.0
```

### 2. Verify Installation

```powershell
# List installed tools
dotnet tool list --global | Select-String "Sytone.Personal.Mcp"

# Expected output:
# sytone.personal.mcp    0.1.0    Personal.Mcp
```

### 3. Test Execution

```powershell
# Set vault path (use a test vault)
$env:OBSIDIAN_VAULT_PATH = "C:\path\to\test\vault"

# Run the tool
sytone.personal.mcp
```

**Expected behavior:**

- Tool starts without errors
- Listens for MCP protocol messages on stdin/stdout
- Can interact with specified vault

### 4. Test with MCP Client

Configure your MCP client (e.g., VS Code with GitHub Copilot):

```json
{
  "github.copilot.chat.mcp.servers": {
    "personal-mcp-local": {
      "command": "sytone.personal.mcp",
      "env": {
        "OBSIDIAN_VAULT_PATH": "C:\\path\\to\\test\\vault"
      }
    }
  }
}
```

Test basic operations:

- List notes
- Read a note
- Search functionality

### 5. Uninstall Test Version

```powershell
# Remove the test installation
dotnet tool uninstall --global Sytone.Personal.Mcp
```

---

## Publishing to NuGet.org

The GitHub Actions workflow `publish.yml` automates publishing on tagged releases. To publish manually, follow these steps:

### Method 1: Using dotnet CLI (Recommended)

```powershell
# Set your API key (do this once)
$env:NUGET_API_KEY = "your-api-key-here"

# Push to NuGet.org
dotnet nuget push ./nuget/Sytone.Personal.Mcp.0.1.0.nupkg `
    --api-key $env:NUGET_API_KEY `
    --source https://api.nuget.org/v3/index.json
```

**Expected output:**

```powershell
Pushing Sytone.Personal.Mcp.0.1.0.nupkg to 'https://www.nuget.org/api/v2/package'...
  PUT https://www.nuget.org/api/v2/package/
  Created https://www.nuget.org/api/v2/package/ 2000ms
Your package was pushed.
```

### Method 2: Using nuget.exe

```powershell
# Download nuget.exe if needed
Invoke-WebRequest -Uri https://dist.nuget.org/win-x86-commandline/latest/nuget.exe -OutFile nuget.exe

# Push to NuGet.org
.\nuget.exe push ./nuget/Sytone.Personal.Mcp.0.1.0.nupkg `
    -ApiKey $env:NUGET_API_KEY `
    -Source https://api.nuget.org/v3/index.json
```

### Method 3: Using NuGet.org Web Upload

1. Go to <https://www.nuget.org/packages/manage/upload>
2. Click **Browse** and select your `.nupkg` file
3. Click **Upload**
4. Review the package information
5. Click **Submit**

---

## Verifying the Publication

### 1. Check NuGet.org Package Page

After publishing, it may take a few minutes for the package to appear and be indexed.

- **Package URL**: <https://www.nuget.org/packages/Sytone.Personal.Mcp>
- **Direct version**: <https://www.nuget.org/packages/Sytone.Personal.Mcp/0.1.0>

**Verify:**

- [ ] Package appears in search results
- [ ] README is displayed correctly
- [ ] Dependencies are listed
- [ ] Download statistics start tracking

### 2. Test Installation from NuGet.org

```powershell
# Wait 5-10 minutes for package to be indexed

# Install from NuGet.org
dotnet tool install --global Sytone.Personal.Mcp

# Verify installation
dotnet tool list --global | Select-String "Sytone.Personal.Mcp"
```

### 3. Test .mcp/server.json Discovery

If your package includes `.mcp/server.json`, verify it's accessible:

```powershell
# Check tool installation path
$toolPath = (Get-Command sytone.personal.mcp).Source
$mcpJson = Join-Path (Split-Path $toolPath) ".mcp/server.json"

# Verify server.json exists
Test-Path $mcpJson
Get-Content $mcpJson | ConvertFrom-Json
```

---

## Updating an Existing Package Manually

### 1. Update Version Number

Edit `src/Personal.Mcp/Personal.Mcp.csproj`:

```xml
<PackageVersion>0.2.0</PackageVersion>  <!-- Increment version -->
```

**Semantic Versioning Guidelines:**

- **Major** (1.0.0 → 2.0.0): Breaking changes
- **Minor** (1.0.0 → 1.1.0): New features, backward compatible
- **Patch** (1.0.0 → 1.0.1): Bug fixes, backward compatible

### 2. Update Release Notes

Consider adding release notes in the .csproj:

```xml
<PropertyGroup>
  <PackageReleaseNotes>
    Version 0.2.0:
    - Added new feature X
    - Fixed issue Y
    - Improved performance Z
  </PackageReleaseNotes>
</PropertyGroup>
```

### 3. Build and Publish

```powershell
# Clean previous builds
dotnet clean src/Personal.Mcp/Personal.Mcp.csproj -c Release
Remove-Item -Path ./nuget -Recurse -Force -ErrorAction SilentlyContinue

# Build new version
dotnet pack src/Personal.Mcp/Personal.Mcp.csproj -c Release -o ./nuget

# Publish new version
dotnet nuget push ./nuget/Sytone.Personal.Mcp.0.2.0.nupkg `
    --api-key $env:NUGET_API_KEY `
    --source https://api.nuget.org/v3/index.json
```

### 4. Tag Release in Git

```powershell
# Create and push git tag
git tag -a v0.2.0 -m "Release version 0.2.0"
git push origin v0.2.0
```

---

## Troubleshooting

### Issue: "Package already exists"

**Error:**

```powershell
Response status code does not indicate success: 409 (Conflict - The package already exists)
```

**Solution:**

- You cannot overwrite an existing package version on NuGet.org
- Increment the version number in `.csproj`
- Rebuild and push the new version

### Issue: "API key is invalid"

**Error:**

```powershell
Response status code does not indicate success: 403 (Forbidden)
```

**Solutions:**

1. Verify your API key is correct
2. Check API key hasn't expired
3. Verify API key has `Push` permissions
4. Ensure glob pattern matches your package ID

### Issue: "Package validation failed"

**Common causes:**

- Missing required metadata (Description, Authors, etc.)
- Invalid package ID format
- License information missing
- README file path incorrect

**Solution:**
Review package metadata in `.csproj` and fix validation errors shown on NuGet.org.

### Issue: Package contains sensitive data

**Prevention:**

1. Review `.gitignore`:

   ```gitignore
   **/logs/*
   **/bin/**/*
   **/obj/**/*
   tmp/
   .vscode/mcp.json
   **/appsettings.Development.json
   ```

2. Inspect package before publishing:

   ```powershell
   # Extract and review contents
   Expand-Archive ./nuget/Sytone.Personal.Mcp.0.1.0.nupkg -DestinationPath ./package-review
   code ./package-review  # Open in editor to review
   ```

3. If already published with sensitive data:
   - **Immediately unlist** the package on NuGet.org
   - **Contact NuGet support** to request deletion
   - **Rotate any exposed credentials**
   - Fix the issue and publish a new version

### Issue: "Target framework not supported"

**Error:** Users report installation failures on certain .NET versions

**Solution:**

- Consider multi-targeting in `.csproj`:

  ```xml
  <TargetFrameworks>net8.0;net9.0</TargetFrameworks>
  ```

- Or clearly document minimum .NET version requirement

### Issue: Tool doesn't start after installation

**Common causes:**

1. Missing environment variables
2. Incorrect tool configuration
3. Runtime dependencies not included

**Solutions:**

1. Verify package is marked as `<PackAsTool>true</PackAsTool>`
2. Check `<PublishSingleFile>` and `<SelfContained>` settings
3. Test locally before publishing
4. Add clear usage instructions in README

---

## Best Practices

### 1. Version Management

- Use semantic versioning consistently
- Tag releases in git: `git tag v0.1.0`
- Maintain CHANGELOG.md
- Never delete or overwrite published versions

### 2. Documentation

- Keep README.md up-to-date
- Include clear installation instructions
- Provide usage examples
- Document breaking changes prominently

### 3. Testing

- Test package locally before publishing
- Use a test NuGet feed for pre-release versions
- Consider publishing pre-release versions (e.g., `0.1.0-beta`)
- Gather user feedback before stable releases

### 4. Security

- Review security checklist before every publish
- Use GitHub secret scanning
- Enable NuGet package signing (optional but recommended)
- Monitor for vulnerability reports

### 5. Communication

- Use GitHub releases to announce new versions
- Link NuGet package to GitHub repository
- Respond to issues and questions promptly
- Consider a discussion forum or Discord

---

## Additional Resources

- **NuGet Documentation**: <https://docs.microsoft.com/en-us/nuget/>
- **NuGet Package Explorer**: <https://github.com/NuGetPackageExplorer/NuGetPackageExplorer>
- **Semantic Versioning**: <https://semver.org/>
- **MCP Protocol**: <https://modelcontextprotocol.io/>
- **.NET Tool Documentation**: <https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools>

---

## Quick Reference

### One-Command Build and Publish

```powershell
# Complete workflow
cd ~/repos/obsidian-mcp && `
dotnet clean src/Personal.Mcp/Personal.Mcp.csproj -c Release && `
Remove-Item -Path ./nuget -Recurse -Force -ErrorAction SilentlyContinue && `
dotnet pack src/Personal.Mcp/Personal.Mcp.csproj -c Release -o ./nuget && `
dotnet nuget push ./nuget/Sytone.Personal.Mcp.*.nupkg --api-key $env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json
```

### Version Bump and Publish Script

```powershell
# publish.ps1
param(
    [Parameter(Mandatory=$true)]
    [string]$Version,
    
    [Parameter(Mandatory=$true)]
    [string]$ApiKey
)

# Update version in csproj
$csproj = "src/Personal.Mcp/Personal.Mcp.csproj"
(Get-Content $csproj) -replace '<PackageVersion>.*</PackageVersion>', "<PackageVersion>$Version</PackageVersion>" | Set-Content $csproj

# Build and publish
dotnet clean $csproj -c Release
Remove-Item -Path ./nuget -Recurse -Force -ErrorAction SilentlyContinue
dotnet pack $csproj -c Release -o ./nuget
dotnet nuget push "./nuget/Sytone.Personal.Mcp.$Version.nupkg" --api-key $ApiKey --source https://api.nuget.org/v3/index.json

# Tag in git
git tag -a "v$Version" -m "Release version $Version"
Write-Host "✅ Package published! Don't forget to: git push origin v$Version"
```

**Usage:**

```powershell
.\publish.ps1 -Version "0.2.0" -ApiKey $env:NUGET_API_KEY
```

---

**Last Updated:** October 16, 2025
