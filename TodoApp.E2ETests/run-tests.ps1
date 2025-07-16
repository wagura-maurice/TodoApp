# TodoApp.E2ETests/run-tests.ps1
param(
    [string]$Filter = ""
)

# Install dependencies if needed
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error "dotnet CLI is not installed. Please install .NET 7.0 SDK or later."
    exit 1
}

# Restore and build the solution
dotnet restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Run the tests
if ($Filter) {
    dotnet test --filter "FullyQualifiedName~$Filter"
} else {
    dotnet test
}

# Open the screenshots directory if it exists
$screenshotsDir = Join-Path $PSScriptRoot "screenshots"
if (Test-Path $screenshotsDir) {
    explorer $screenshotsDir
}