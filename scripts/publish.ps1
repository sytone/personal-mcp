# param(
#     [Parameter(Mandatory=$true)]
#     [string]$Version
# )
$Version = git cliff --bumped-version

Write-Host "🔄 Updating version to $Version..."

# Update version in csproj
$csproj = "src/Personal.Mcp/Personal.Mcp.csproj"
Write-Host "🔄 Updating $csproj"
(Get-Content $csproj) -replace '<Version>.*</Version>', "<Version>$Version</Version>" | Set-Content $csproj
(Get-Content $csproj) -replace '<PackageVersion>.*</PackageVersion>', "<PackageVersion>$Version</PackageVersion>" | Set-Content $csproj

# # Update version in .mcp/server.json
$serverJsonPath = "src/Personal.Mcp/.mcp/server.json"
Write-Host "🔄 Updating $serverJsonPath"
(Get-Content $serverJsonPath) -replace '"version": ".*?"', "`"version`": `"$Version`"" | Set-Content $serverJsonPath

# # Update version in Program.cs
$programCsPath = "src/Personal.Mcp/Program.cs"
Write-Host "🔄 Updating $programCsPath"
(Get-Content $programCsPath) -replace 'Version = ".*?"', "Version = `"$Version`"" | Set-Content $programCsPath

# # Update the reference in README.md under the Quick Start section
$readmePath = "README.md"
Write-Host "🔄 Updating $readmePath"
(Get-Content $readmePath) -replace 'Sytone\.Personal\.Mcp@.*?$', "Sytone.Personal.Mcp@$Version --yes" | Set-Content $readmePath

# Update the change log

# Test the build
Write-Host "🔨 Building the project..."
Write-Host "🔖 Version to be published: $Version"

Write-Host "🪥 Cleaning previous builds..."
dotnet clean -c Release
# If any errors abort
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed. Aborting publish."
    exit $LASTEXITCODE
}

Write-Host "🔥 Removing previous nuget packages..."
Remove-Item -Path ./nuget -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "🏗️ Building, testing, and packing..."
dotnet build -c Release

# If any errors abort
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed. Aborting publish."
    exit $LASTEXITCODE
}

dotnet test -c Release

# If any errors abort
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed. Aborting publish."
    exit $LASTEXITCODE
}

dotnet pack -c Release -o ./nuget

# If any errors abort
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed. Aborting publish."
    exit $LASTEXITCODE
}

git add .
git commit -m "chore: bump files to version to $Version"

# winget install git-cliff

# Commit the version changes
git-cliff --unreleased --bump --prepend CHANGELOG.md
git add CHANGELOG.md
git commit -a -m'chore: git-cliff'
git tag $(git cliff --bumped-version)

Write-Host "✅ Package published!"