# Test the build
Write-Host "🔨 Building the project..."

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
    Write-Host "❌ Build failed. Aborting pack validation."
    exit $LASTEXITCODE
}

meziantou.validate-nuget-package (Get-ChildItem -Path ./nuget -Filter *.nupkg | Select-Object -First 1).FullName
meziantou.validate-nuget-package (Get-ChildItem "./nuget/*.nupkg")

Read-Host "✅ Build & Pack succeeded."
