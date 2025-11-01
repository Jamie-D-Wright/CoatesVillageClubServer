# debug-service.ps1 - Membership Service Debugging Utility
# Provides tools for diagnosing service startup, background job, and connectivity issues

param(
    [Parameter(Position=0)]
    [ValidateSet("status", "health", "jobs", "processes")]
    [string]$Command = "status"
)

function Show-Status {
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "  Membership Service Debug Status" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""

    # Check PowerShell jobs
    Write-Host "PowerShell Background Jobs:" -ForegroundColor Yellow
    $jobs = Get-Job | Where-Object { $_.Name -like "*membership*" -or $_.Command -like "*func start*" }
    if ($jobs) {
        $jobs | Format-Table -Property Id, Name, State, Command -AutoSize
    } else {
        Write-Host "  [No membership-related jobs found]" -ForegroundColor Gray
    }
    Write-Host ""

    # Check processes
    Write-Host "Func.exe Processes:" -ForegroundColor Yellow
    $funcProcesses = Get-Process -Name "func" -ErrorAction SilentlyContinue
    if ($funcProcesses) {
        $funcProcesses | Format-Table -Property Id, ProcessName, CPU, WorkingSet -AutoSize
    } else {
        Write-Host "  [No func.exe processes running]" -ForegroundColor Gray
    }
    Write-Host ""

    # Check port 7071
    Write-Host "Port 7071 Listeners:" -ForegroundColor Yellow
    $port = Get-NetTCPConnection -LocalPort 7071 -ErrorAction SilentlyContinue
    if ($port) {
        $port | Format-Table -Property LocalAddress, LocalPort, State, OwningProcess -AutoSize
    } else {
        Write-Host "  [Nothing listening on port 7071]" -ForegroundColor Gray
    }
    Write-Host ""

    # Check log file
    Write-Host "Service Log File:" -ForegroundColor Yellow
    $logFile = "C:\Code\CoatesVillageClubServer\scripts\membership-service.log"
    if (Test-Path $logFile) {
        $lastModified = (Get-Item $logFile).LastWriteTime
        $fileSize = (Get-Item $logFile).Length
        Write-Host "  [OK] Log file exists" -ForegroundColor Green
        Write-Host "  Last Modified: $lastModified" -ForegroundColor Gray
        Write-Host "  Size: $fileSize bytes" -ForegroundColor Gray
    } else {
        Write-Host "  [WARN] No log file found at: $logFile" -ForegroundColor Red
    }
    Write-Host ""
}

function Show-Health {
    Write-Host "Checking service health endpoint..." -ForegroundColor Cyan
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:7071/api/v1/health" -Method GET -TimeoutSec 5
        Write-Host "[OK] Service is responding" -ForegroundColor Green
        Write-Host "Status Code: $($response.StatusCode)" -ForegroundColor Gray
        Write-Host "Response:" -ForegroundColor Gray
        $response.Content | ConvertFrom-Json | ConvertTo-Json -Depth 10
    } catch {
        Write-Host "[FAIL] Service is not responding" -ForegroundColor Red
        Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Gray
    }
    Write-Host ""
}

function Show-Jobs {
    Write-Host "All PowerShell Background Jobs:" -ForegroundColor Cyan
    Get-Job | Format-Table -AutoSize
    Write-Host ""
}

function Show-Processes {
    Write-Host "All Azure Functions Processes:" -ForegroundColor Cyan
    Get-Process -Name "func" -ErrorAction SilentlyContinue | Format-Table -Property Id, ProcessName, CPU, WorkingSet, StartTime -AutoSize
    Write-Host ""
}

# Execute command
switch ($Command) {
    "status"    { Show-Status }
    "health"    { Show-Health }
    "jobs"      { Show-Jobs }
    "processes" { Show-Processes }
}

Write-Host "Usage:" -ForegroundColor Gray
Write-Host "  .\debug-service.ps1 status     - Show complete service status (default)" -ForegroundColor Gray
Write-Host "  .\debug-service.ps1 health     - Test health endpoint" -ForegroundColor Gray
Write-Host "  .\debug-service.ps1 jobs       - List all background jobs" -ForegroundColor Gray
Write-Host "  .\debug-service.ps1 processes  - List func.exe processes" -ForegroundColor Gray
