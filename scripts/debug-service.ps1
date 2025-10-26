# Debug Membership Service
# This script provides debugging information and logs for the membership service

param(
    [ValidateSet("status", "logs", "recent", "follow", "jobs", "processes", "health")]
    [string]$Action = "status"
)

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

function Show-Status {
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "  Membership Service Debug Info" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
    
    # Check PowerShell jobs
    Write-Host "[PowerShell Jobs]" -ForegroundColor Yellow
    $jobs = Get-Job | Where-Object { $_.Command -like "*func start*" }
    if ($jobs) {
        $jobs | Format-Table Id, State, Command, PSBeginTime -AutoSize
    } else {
        Write-Host "  No background jobs found" -ForegroundColor Gray
    }
    Write-Host ""
    
    # Check func processes
    Write-Host "[Func Processes]" -ForegroundColor Yellow
    $processes = Get-Process | Where-Object { $_.ProcessName -like "*func*" }
    if ($processes) {
        $processes | Format-Table Id, ProcessName, StartTime, WorkingSet -AutoSize
    } else {
        Write-Host "  No func processes found" -ForegroundColor Gray
    }
    Write-Host ""
    
    # Check if port 7071 is listening
    Write-Host "[Port 7071 Status]" -ForegroundColor Yellow
    try {
        $tcpConnection = Get-NetTCPConnection -LocalPort 7071 -ErrorAction SilentlyContinue
        if ($tcpConnection) {
            Write-Host "  ✓ Port 7071 is listening" -ForegroundColor Green
            Write-Host "  Process ID: $($tcpConnection.OwningProcess)" -ForegroundColor Gray
        } else {
            Write-Host "  ✗ Port 7071 is not listening" -ForegroundColor Red
        }
    } catch {
        Write-Host "  Unable to check port status" -ForegroundColor Yellow
    }
    Write-Host ""
    
    # Check log file
    Write-Host "[Log File]" -ForegroundColor Yellow
    $logFile = Join-Path $scriptDir "membership-service.log"
    if (Test-Path $logFile) {
        $fileInfo = Get-Item $logFile
        Write-Host "  ✓ Log file exists: $logFile" -ForegroundColor Green
        Write-Host "  Size: $([Math]::Round($fileInfo.Length / 1KB, 2)) KB" -ForegroundColor Gray
        Write-Host "  Last Modified: $($fileInfo.LastWriteTime)" -ForegroundColor Gray
    } else {
        Write-Host "  ✗ No log file found at: $logFile" -ForegroundColor Red
    }
    Write-Host ""
}

function Show-Health {
    Write-Host "Checking service health endpoint..." -ForegroundColor Cyan
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:7071/api/v1/health" -Method GET -TimeoutSec 5
        Write-Host "✓ Service is responding" -ForegroundColor Green
        Write-Host "Status Code: $($response.StatusCode)" -ForegroundColor Gray
        Write-Host "Response:" -ForegroundColor Gray
        $response.Content | ConvertFrom-Json | ConvertTo-Json -Depth 10
    } catch {
        Write-Host "✗ Service is not responding" -ForegroundColor Red
        Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Execute requested action
switch ($Action) {
    "status" { Show-Status }
    "logs" { & "$scriptDir\view-service-logs.ps1" }
    "recent" { & "$scriptDir\view-service-logs.ps1" -Tail -Lines 50 }
    "follow" { & "$scriptDir\view-service-logs.ps1" -Follow }
    "jobs" { 
        Get-Job | Format-Table Id, State, Command, PSBeginTime -AutoSize
    }
    "processes" { 
        Get-Process | Where-Object { $_.ProcessName -like "*func*" } | Format-Table Id, ProcessName, StartTime, CPU, WorkingSet -AutoSize
    }
    "health" { Show-Health }
}

Write-Host ""
Write-Host "Available actions:" -ForegroundColor Gray
Write-Host "  .\debug-service.ps1 status    - Show overall status" -ForegroundColor Gray
Write-Host "  .\debug-service.ps1 logs      - Show all logs" -ForegroundColor Gray
Write-Host "  .\debug-service.ps1 recent    - Show last 50 log lines" -ForegroundColor Gray
Write-Host "  .\debug-service.ps1 follow    - Follow logs in real-time" -ForegroundColor Gray
Write-Host "  .\debug-service.ps1 jobs      - List PowerShell jobs" -ForegroundColor Gray
Write-Host "  .\debug-service.ps1 processes - List func processes" -ForegroundColor Gray
Write-Host "  .\debug-service.ps1 health    - Test health endpoint" -ForegroundColor Gray
