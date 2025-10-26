# View Membership Service Logs
# This script shows logs from running or completed background jobs

param(
    [int]$JobId,
    [switch]$Tail,
    [int]$Lines = 50,
    [switch]$Follow,
    [switch]$Clear
)

# Find the membership service job if not specified
if (-not $JobId) {
    $jobs = Get-Job | Where-Object { $_.Command -like "*func start*" }
    
    if ($jobs.Count -eq 0) {
        Write-Host "No membership service jobs found." -ForegroundColor Yellow
        Write-Host "Checking for log file..." -ForegroundColor Gray
        
        $logFile = Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) "membership-service.log"
        if (Test-Path $logFile) {
            Write-Host "Found log file: $logFile" -ForegroundColor Green
            if ($Tail) {
                Get-Content $logFile -Tail $Lines
            } elseif ($Follow) {
                Get-Content $logFile -Wait -Tail $Lines
            } else {
                Get-Content $logFile
            }
        } else {
            Write-Host "No log file found at: $logFile" -ForegroundColor Red
        }
        exit 0
    }
    
    if ($jobs.Count -gt 1) {
        Write-Host "Multiple jobs found. Please specify JobId:" -ForegroundColor Yellow
        $jobs | Select-Object Id, State, Command | Format-Table
        exit 1
    }
    
    $JobId = $jobs[0].Id
}

# Display job info
$job = Get-Job -Id $JobId -ErrorAction SilentlyContinue
if (-not $job) {
    Write-Host "Job ID $JobId not found" -ForegroundColor Red
    exit 1
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Membership Service Logs" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Job ID: $($job.Id)" -ForegroundColor Gray
Write-Host "State: $($job.State)" -ForegroundColor Gray
Write-Host "Started: $($job.PSBeginTime)" -ForegroundColor Gray
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Clear received output if requested
if ($Clear) {
    Receive-Job -Id $JobId | Out-Null
    Write-Host "Previous logs cleared. Showing new output only..." -ForegroundColor Yellow
    Write-Host ""
}

# Show logs
if ($Follow) {
    Write-Host "Following logs (Ctrl+C to stop)..." -ForegroundColor Yellow
    Write-Host ""
    while ($true) {
        $output = Receive-Job -Id $JobId -Keep
        if ($output) {
            $output | Select-Object -Last $Lines
        }
        Start-Sleep -Seconds 2
    }
} elseif ($Tail) {
    Receive-Job -Id $JobId -Keep | Select-Object -Last $Lines
} else {
    Receive-Job -Id $JobId -Keep
}
