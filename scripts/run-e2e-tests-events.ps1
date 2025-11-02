# Run Events Service E2E Tests with Newman
# This script runs the Events service Postman collection with the local environment

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Split-Path -Parent $scriptDir

Write-Host "Running Events Service E2E Tests..." -ForegroundColor Cyan
Write-Host ""

# Check if newman is installed
if (-not (Get-Command newman -ErrorAction SilentlyContinue)) {
    Write-Host "[ERROR] Newman is not installed. Install with: npm install -g newman" -ForegroundColor Red
    exit 1
}

# Run newman from the project root directory
Push-Location $rootDir
try {
    newman run tests/postman/events-service.postman_collection.json -e tests/postman/local.postman_environment.json
    $exitCode = $LASTEXITCODE
} finally {
    Pop-Location
}

if ($exitCode -eq 0) {
    Write-Host ""
    Write-Host "[PASS] All Events E2E tests passed" -ForegroundColor Green
    exit 0
} else {
    Write-Host ""
    Write-Host "[FAIL] Some Events E2E tests failed" -ForegroundColor Red
    exit 1
}
