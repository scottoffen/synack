#!/usr/bin/env pwsh
# test-coverage.ps1

$ErrorActionPreference = "Stop"

# Run tests and collect coverage
dotnet test src --collect:"XPlat Code Coverage" --settings src/coverlet.runsettings

# Find the most recent coverage file
$coverageFile = Get-ChildItem -Recurse -Filter "coverage.cobertura.xml" | Sort-Object LastWriteTime -Descending | Select-Object -First 1

if (-not $coverageFile) {
    Write-Error "❌ No coverage file found."
    exit 1
}

# Generate the report
reportgenerator `
  -reports:$coverageFile.FullName `
  -targetdir:"coverage" `
  -reporttypes:"HtmlInline_AzurePipelines;TextSummary"

# Open report in default browser
$indexPath = Join-Path "coverage" "index.html"

if ($IsWindows) {
    Start-Process $indexPath
} elseif ($IsMacOS) {
    open $indexPath
} elseif ($IsLinux) {
    xdg-open $indexPath
} else {
    Write-Warning "Platform not detected - open coverage-report/index.html manually"
}
