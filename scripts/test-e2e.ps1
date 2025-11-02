# Start Services and Run E2E Tests
# This script starts services and runs E2E tests

param(
    [switch]$SkipServiceStart,
    [switch]$KeepServiceRunning,
    [ValidateSet("All", "Membership", "Events")]
    [string]$Service = "All"
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Split-Path -Parent $scriptDir

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Village Club E2E Test Runner" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$allTestsPassed = $true

# Test Membership Service
if ($Service -eq "All" -or $Service -eq "Membership") {
    Write-Host "Testing Membership Service..." -ForegroundColor Magenta
    Write-Host ""
    
    try {
        & "$scriptDir\test-e2e-membership.ps1" -SkipServiceStart:$SkipServiceStart -KeepServiceRunning:$KeepServiceRunning
        if ($LASTEXITCODE -ne 0) {
            $allTestsPassed = $false
        }
    } catch {
        Write-Host "Error testing Membership service: $_" -ForegroundColor Red
        $allTestsPassed = $false
    }
    
    Write-Host ""
}

# Test Events Service
if ($Service -eq "All" -or $Service -eq "Events") {
    Write-Host "Testing Events Service..." -ForegroundColor Magenta
    Write-Host ""
    
    try {
        & "$scriptDir\test-e2e-events.ps1" -SkipServiceStart:$SkipServiceStart -KeepServiceRunning:$KeepServiceRunning
        if ($LASTEXITCODE -ne 0) {
            $allTestsPassed = $false
        }
    } catch {
        Write-Host "Error testing Events service: $_" -ForegroundColor Red
        $allTestsPassed = $false
    }
    
    Write-Host ""
}

Write-Host "========================================" -ForegroundColor Cyan

if ($allTestsPassed) {
    Write-Host "  [PASS] All E2E Tests: PASSED" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Cyan
    exit 0
} else {
    Write-Host "  [FAIL] Some E2E Tests: FAILED" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Cyan
    exit 1
}
