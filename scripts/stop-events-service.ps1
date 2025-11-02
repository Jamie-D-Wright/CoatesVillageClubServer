# Stop Events Service
# Stops the background job running the events service

Write-Host "Stopping Events Service..." -ForegroundColor Yellow

# Get the job running on port 7072
$jobs = Get-Job | Where-Object { $_.Command -like "*func start --port 7072*" }

if ($jobs) {
    foreach ($job in $jobs) {
        Write-Host "Stopping job $($job.Id)..." -ForegroundColor Gray
        Stop-Job -Id $job.Id
        Remove-Job -Id $job.Id
    }
    Write-Host "[OK] Service stopped" -ForegroundColor Green
} else {
    Write-Host "[WARN] No running Events service job found" -ForegroundColor Yellow
}

# Also kill any func processes listening on port 7072
$funcProcesses = Get-NetTCPConnection -LocalPort 7072 -ErrorAction SilentlyContinue | Select-Object -ExpandProperty OwningProcess | Get-Process
if ($funcProcesses) {
    Write-Host "Stopping func processes on port 7072..." -ForegroundColor Yellow
    $funcProcesses | Stop-Process -Force
    Write-Host "[OK] Processes terminated" -ForegroundColor Green
}
