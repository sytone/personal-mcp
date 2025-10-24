param(
    [Parameter(Mandatory=$true)]
    [string]$Version
)

Write-Host "🔄 Updating version to $Version..."

# Update version in csproj
$csproj = "src/Personal.Mcp/Personal.Mcp.csproj"
Write-Host "🔄 Updating $csproj"
(Get-Content $csproj) -replace '<Version>.*</Version>', "<Version>$Version</Version>" | Set-Content $csproj
(Get-Content $csproj) -replace '<PackageVersion>.*</PackageVersion>', "<PackageVersion>$Version</PackageVersion>" | Set-Content $csproj

# Update version in .mcp/server.json
$serverJsonPath = "src/Personal.Mcp/.mcp/server.json"
Write-Host "🔄 Updating $serverJsonPath"
(Get-Content $serverJsonPath) -replace '"version": ".*?"', "`"version`": `"$Version`"" | Set-Content $serverJsonPath

# Update version in Program.cs
$programCsPath = "src/Personal.Mcp/Program.cs"
Write-Host "🔄 Updating $programCsPath"
(Get-Content $programCsPath) -replace 'Version = ".*?"', "Version = `"$Version`"" | Set-Content $programCsPath

# Update the reference in README.md under the Quick Start section
$readmePath = "README.md"
Write-Host "🔄 Updating $readmePath"
(Get-Content $readmePath) -replace 'Sytone\.Personal\.Mcp@.*?$', "Sytone.Personal.Mcp@$Version" | Set-Content $readmePath

# Update the change log

# Test the build
Write-Host "🔨 Building the project..."
Write-Host "🔖 Version to be published: $Version"

Write-Host "🪥 Cleaning previous builds..."
dotnet clean -c Release

Write-Host "🔥 Removing previous nuget packages..."
Remove-Item -Path ./nuget -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "🏗️ Building, testing, and packing..."
dotnet build -c Release
dotnet test  -c Release
dotnet pack -c Release -o ./nuget

# If any errors abort
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed. Aborting publish."
    exit $LASTEXITCODE
}

# dotnet tool install --global Versionize

versionize

# Read-Host "✅ Build succeeded. Press Enter to continue with publishing or Ctrl+C to abort."

# # Commit the version changes
# git add .
# git commit -m "chore: bump version to $Version"
# git push

# # Tag in git
# git tag -a "$Version" -m "Release version $Version"
# git push --tags

Write-Host "✅ Package published!"