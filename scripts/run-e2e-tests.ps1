# Run E2E Tests with Newman
# This script runs the Postman collection with the local environment

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Split-Path -Parent $scriptDir

Write-Host "Running Postman E2E Tests..." -ForegroundColor Cyan
Write-Host ""

# Run newman from the project root directory
Push-Location $rootDir
try {
    newman run tests/postman/membership-service.postman_collection.json -e tests/postman/local.postman_environment.json
    $exitCode = $LASTEXITCODE
} finally {
    Pop-Location
}

if ($exitCode -eq 0) {
    Write-Host ""
    Write-Host "[PASS] All E2E tests passed" -ForegroundColor Green
    exit 0
} else {
    Write-Host ""
    Write-Host "[FAIL] Some E2E tests failed" -ForegroundColor Red
    exit 1
}
