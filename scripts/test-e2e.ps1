# Run E2E Tests for All Services
# This script orchestrates the complete E2E test workflow for all services:
# 1. Start all requested services (unless -SkipServiceStart is specified)
# 2. Run the E2E tests for each service
# 3. Stop all services (unless -KeepServiceRunning is specified)

param(
    [switch]$SkipServiceStart,
    [switch]$KeepServiceRunning,
    [ValidateSet("All", "Membership", "Events")]
    [string]$Service = "All"
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Split-Path -Parent $scriptDir

Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║         Village Club E2E Test Workflow                    ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

$servicesStarted = @()
$allTestsPassed = $true

try {
    # Phase 1: Start Services
    if (-not $SkipServiceStart) {
        Write-Host "[Phase 1/3] Starting Services..." -ForegroundColor Yellow
        Write-Host ""
        
        if ($Service -eq "All" -or $Service -eq "Membership") {
            Write-Host "  → Starting Membership service (port 7071)..." -ForegroundColor Cyan
            & "$scriptDir\start-membership-service.ps1"
            $servicesStarted += "Membership"
            Write-Host ""
        }
        
        if ($Service -eq "All" -or $Service -eq "Events") {
            Write-Host "  → Starting Events service (port 7072)..." -ForegroundColor Cyan
            & "$scriptDir\start-events-service.ps1"
            $servicesStarted += "Events"
            Write-Host ""
        }
        
        Write-Host "[OK] All services started" -ForegroundColor Green
        Write-Host ""
    } else {
        Write-Host "[Phase 1/3] Skipping service start (using existing services)" -ForegroundColor Yellow
        Write-Host ""
    }

    # Phase 2: Run Tests
    Write-Host "[Phase 2/3] Running E2E Tests..." -ForegroundColor Yellow
    Write-Host ""
    
    if ($Service -eq "All" -or $Service -eq "Membership") {
        Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Magenta
        Write-Host "║         Testing Membership Service                        ║" -ForegroundColor Magenta
        Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Magenta
        Write-Host ""
        
        try {
            & "$scriptDir\run-e2e-tests.ps1"
            if ($LASTEXITCODE -ne 0) {
                $allTestsPassed = $false
                Write-Host "[FAIL] Membership tests failed" -ForegroundColor Red
            } else {
                Write-Host "[PASS] Membership tests passed" -ForegroundColor Green
            }
        } catch {
            Write-Host "Error testing Membership service: $_" -ForegroundColor Red
            $allTestsPassed = $false
        }
        
        Write-Host ""
    }

    if ($Service -eq "All" -or $Service -eq "Events") {
        Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Magenta
        Write-Host "║         Testing Events Service                            ║" -ForegroundColor Magenta
        Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Magenta
        Write-Host ""
        
        try {
            & "$scriptDir\run-e2e-tests-events.ps1"
            if ($LASTEXITCODE -ne 0) {
                $allTestsPassed = $false
                Write-Host "[FAIL] Events tests failed" -ForegroundColor Red
            } else {
                Write-Host "[PASS] Events tests passed" -ForegroundColor Green
            }
        } catch {
            Write-Host "Error testing Events service: $_" -ForegroundColor Red
            $allTestsPassed = $false
        }
        
        Write-Host ""
    }

} finally {
    # Phase 3: Stop Services
    if (-not $KeepServiceRunning -and $servicesStarted.Count -gt 0) {
        Write-Host "[Phase 3/3] Stopping Services..." -ForegroundColor Yellow
        Write-Host ""
        
        foreach ($svc in $servicesStarted) {
            Write-Host "  → Stopping $svc service..." -ForegroundColor Cyan
            try {
                if ($svc -eq "Membership") {
                    & "$scriptDir\stop-membership-service.ps1"
                } elseif ($svc -eq "Events") {
                    & "$scriptDir\stop-events-service.ps1"
                }
            } catch {
                Write-Host "  [WARNING] Error stopping $svc service: $_" -ForegroundColor Yellow
            }
        }
        
        Write-Host ""
        Write-Host "[OK] All services stopped" -ForegroundColor Green
    } elseif ($KeepServiceRunning -and $servicesStarted.Count -gt 0) {
        Write-Host "[Phase 3/3] Keeping services running for debugging..." -ForegroundColor Yellow
        Write-Host ""
        Write-Host "Services still running:" -ForegroundColor Cyan
        foreach ($svc in $servicesStarted) {
            Write-Host "  → $svc service" -ForegroundColor Cyan
        }
        Write-Host ""
        Write-Host "To stop services manually, run:" -ForegroundColor Yellow
        foreach ($svc in $servicesStarted) {
            if ($svc -eq "Membership") {
                Write-Host "  .\scripts\stop-membership-service.ps1" -ForegroundColor Gray
            } elseif ($svc -eq "Events") {
                Write-Host "  .\scripts\stop-events-service.ps1" -ForegroundColor Gray
            }
        }
    } else {
        Write-Host "[Phase 3/3] No services to stop" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan

if ($allTestsPassed) {
    Write-Host "║         [PASS] All E2E Tests: PASSED                      ║" -ForegroundColor Green
    Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    exit 0
} else {
    Write-Host "║         [FAIL] Some E2E Tests: FAILED                     ║" -ForegroundColor Red
    Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    exit 1
}
