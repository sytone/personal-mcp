#!/usr/bin/env pwsh

# Test runner script for Personal MCP
# Usage: .\scripts\test.ps1 [options]

param(
    [string]$Filter = "",
    [string]$Verbosity = "normal",
    [switch]$Coverage,
    [switch]$Watch,
    [switch]$Help
)

if ($Help) {
    Write-Host "Personal MCP Test Runner" -ForegroundColor Green
    Write-Host ""
    Write-Host "Usage: .\scripts\test.ps1 [options]"
    Write-Host ""
    Write-Host "Options:"
    Write-Host "  -Filter <pattern>    Run only tests matching the pattern"
    Write-Host "  -Verbosity <level>   Set verbosity level (quiet, minimal, normal, detailed)"
    Write-Host "  -Coverage            Generate code coverage report"
    Write-Host "  -Watch               Run tests in watch mode"
    Write-Host "  -Help                Show this help message"
    Write-Host ""
    Write-Host "Examples:"
    Write-Host "  .\scripts\test.ps1                                    # Run all tests"
    Write-Host "  .\scripts\test.ps1 -Filter AddJournalEntry            # Run specific test category"
    Write-Host "  .\scripts\test.ps1 -Verbosity minimal                 # Run with minimal output"
    Write-Host "  .\scripts\test.ps1 -Coverage                          # Generate coverage report"
    Write-Host "  .\scripts\test.ps1 -Watch                             # Run in watch mode"
    exit 0
}

$testProject = "tests/Personal.Mcp.Tests/Personal.Mcp.Tests.csproj"

# Build the command
$command = "dotnet test $testProject"

if ($Verbosity) {
    $command += " --verbosity $Verbosity"
}

if ($Filter) {
    $command += " --filter `"$Filter`""
}

if ($Coverage) {
    $command += " --collect:`"XPlat Code Coverage`""
}

if ($Watch) {
    $command += " --watch"
}

Write-Host "Running: $command" -ForegroundColor Yellow
Write-Host ""

# Execute the command
Invoke-Expression $command

if ($Coverage -and $LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "Coverage report generated in TestResults folder" -ForegroundColor Green
    
    # Try to find and display coverage file location
    $coverageFiles = Get-ChildItem -Path "TestResults" -Filter "coverage.cobertura.xml" -Recurse -ErrorAction SilentlyContinue
    if ($coverageFiles) {
        Write-Host "Coverage file: $($coverageFiles[0].FullName)" -ForegroundColor Cyan
    }
}

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "✅ All tests passed!" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "❌ Some tests failed!" -ForegroundColor Red
    exit $LASTEXITCODE
}