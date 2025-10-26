# Stop Membership Service
# Stops the background job running the membership service

Write-Host "Stopping Membership Service..." -ForegroundColor Yellow

# Get the job
$job = Get-Job | Where-Object { $_.Command -like "*func start*" } | Select-Object -First 1

if ($job) {
    Write-Host "Stopping job $($job.Id)..." -ForegroundColor Gray
    Stop-Job -Id $job.Id
    Remove-Job -Id $job.Id
    Write-Host "[OK] Service stopped" -ForegroundColor Green
} else {
    Write-Host "[WARN] No running service job found" -ForegroundColor Yellow
}

# Also kill any func processes
$funcProcesses = Get-Process | Where-Object { $_.ProcessName -eq "func" }
if ($funcProcesses) {
    Write-Host "Stopping func processes..." -ForegroundColor Yellow
    $funcProcesses | Stop-Process -Force
    Write-Host "[OK] Processes terminated" -ForegroundColor Green
}
