# Start Events Service and Run E2E Tests
# This script starts the events service and runs E2E tests

param(
    [switch]$SkipServiceStart,
    [switch]$KeepServiceRunning
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Split-Path -Parent $scriptDir

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Events Service E2E Test Runner" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Start the service (unless skipped)
$jobId = $null
if (-not $SkipServiceStart) {
    Write-Host "[1/3] Starting Events Service..." -ForegroundColor Yellow
    & "$scriptDir\start-events-service.ps1"
    
    # Get the job ID
    $job = Get-Job | Where-Object { $_.Command -like "*func start --port 7072*" } | Select-Object -First 1
    if ($job) {
        $jobId = $job.Id
        Write-Host "[OK] Service started (Job ID: $jobId)" -ForegroundColor Green
    } else {
        Write-Host "[ERROR] Failed to start service" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "[1/3] Skipping service start (using existing service)" -ForegroundColor Yellow
}

Write-Host ""

# Step 2: Run E2E tests
Write-Host "[2/3] Running E2E Tests..." -ForegroundColor Yellow
Write-Host ""

try {
    & "$scriptDir\run-e2e-tests-events.ps1"
    $testsPassed = $LASTEXITCODE -eq 0
} catch {
    Write-Host "Error running tests: $_" -ForegroundColor Red
    $testsPassed = $false
}

Write-Host ""

# Step 3: Stop the service (unless requested to keep running)
if (-not $KeepServiceRunning -and $jobId) {
    Write-Host "[3/3] Stopping Events Service..." -ForegroundColor Yellow
    & "$scriptDir\stop-events-service.ps1"
} else {
    Write-Host "[3/3] Keeping service running (Job ID: $jobId)" -ForegroundColor Yellow
    if ($jobId) {
        Write-Host "  To view logs: Receive-Job -Id $jobId -Keep" -ForegroundColor Gray
        Write-Host "  To stop: Stop-Job -Id $jobId; Remove-Job -Id $jobId" -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan

if ($testsPassed) {
    Write-Host "  [PASS] E2E Tests: PASSED" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Cyan
    exit 0
} else {
    Write-Host "  [FAIL] E2E Tests: FAILED" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Cyan
    exit 1
}
