param(
    [Parameter(Mandatory=$true)]
    [string]$Version
)

# Update version in csproj
$csproj = "src/Personal.Mcp/Personal.Mcp.csproj"
(Get-Content $csproj) -replace '<PackageVersion>.*</PackageVersion>', "<PackageVersion>$Version</PackageVersion>" | Set-Content $csproj

# Update version in .mcp/server.json
$serverJsonPath = "src/Personal.Mcp/.mcp/server.json"
(Get-Content $serverJsonPath) -replace '"version": ".*?"', "`"version`": `"$Version`"" | Set-Content $serverJsonPath

# Update version in Program.cs
$programCsPath = "src/Personal.Mcp/Program.cs"
(Get-Content $programCsPath) -replace 'Version = ".*?"', "Version = `"$Version`"" | Set-Content $programCsPath

# Update the change log

# Test the build
dotnet clean $csproj -c Release
Remove-Item -Path ./nupkg -Recurse -Force -ErrorAction SilentlyContinue
dotnet pack $csproj -c Release -o ./nupkg

# If any errors abort
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed. Aborting publish."
    exit $LASTEXITCODE
}

Read-Host "✅ Build succeeded. Press Enter to continue with publishing or Ctrl+C to abort."

# Tag in git
git tag -a "$Version" -m "Release version $Version"
git push --tags

Write-Host "✅ Package published!"