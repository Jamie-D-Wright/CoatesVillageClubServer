# Run E2E Tests for Membership Service
# This script orchestrates the complete E2E test workflow:
# 1. Start the Membership service (unless -SkipServiceStart is specified)
# 2. Run the Postman E2E tests with Newman
# 3. Stop the Membership service (unless -KeepServiceRunning is specified)

param(
    [switch]$SkipServiceStart,
    [switch]$KeepServiceRunning
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Split-Path -Parent $scriptDir

Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║         Membership Service E2E Test Workflow              ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

$serviceStarted = $false

try {
    # Phase 1: Start Service
    if (-not $SkipServiceStart) {
        Write-Host "[1/3] Starting Membership service..." -ForegroundColor Yellow
        & "$scriptDir\start-membership-service.ps1"
        $serviceStarted = $true
        Write-Host ""
    } else {
        Write-Host "[1/3] Skipping service start (using existing service)" -ForegroundColor Yellow
        Write-Host ""
    }

    # Phase 2: Run Tests
    Write-Host "[2/3] Running E2E tests with Newman..." -ForegroundColor Yellow
    Write-Host ""
    
    Push-Location $rootDir
    try {
        # Check if newman is installed
        $newmanInstalled = Get-Command newman -ErrorAction SilentlyContinue
        if (-not $newmanInstalled) {
            Write-Host "ERROR: Newman is not installed. Install with: npm install -g newman" -ForegroundColor Red
            exit 1
        }

        newman run tests/postman/membership-service.postman_collection.json -e tests/postman/local.postman_environment.json
        $testExitCode = $LASTEXITCODE
    } finally {
        Pop-Location
    }

    Write-Host ""
    
    # Phase 3: Stop Service (unless keeping it running)
    if ($serviceStarted -and -not $KeepServiceRunning) {
        Write-Host "[3/3] Stopping Membership service..." -ForegroundColor Yellow
        & "$scriptDir\stop-membership-service.ps1"
        Write-Host ""
    } elseif ($KeepServiceRunning) {
        Write-Host "[3/3] Service kept running for manual testing" -ForegroundColor Yellow
        Write-Host "      View logs: .\scripts\view-service-logs.ps1" -ForegroundColor Gray
        Write-Host "      Stop later: .\scripts\stop-membership-service.ps1" -ForegroundColor Gray
        Write-Host ""
    }

    # Final Result
    Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    if ($testExitCode -eq 0) {
        Write-Host "║  [PASS] Membership Service E2E Tests Completed            ║" -ForegroundColor Green
    } else {
        Write-Host "║  [FAIL] Membership Service E2E Tests Failed               ║" -ForegroundColor Red
    }
    Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    
    exit $testExitCode

} catch {
    Write-Host ""
    Write-Host "ERROR: E2E test workflow failed" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    
    # Clean up service if we started it
    if ($serviceStarted -and -not $KeepServiceRunning) {
        Write-Host ""
        Write-Host "Cleaning up: Stopping Membership service..." -ForegroundColor Yellow
        & "$scriptDir\stop-membership-service.ps1"
    }
    
    exit 1
}
