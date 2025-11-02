# Start Events Service
# This script starts the events service in the background

$servicePath = "C:\Code\CoatesVillageClubServer\services\events\src\VillageClub.Events"

Write-Host "Starting Events Service on port 7072..." -ForegroundColor Cyan

# Change to service directory and start func
Set-Location $servicePath

# Start the service as a background job
$job = Start-Job -ScriptBlock {
    param($path)
    Set-Location $path
    func start --port 7072
} -ArgumentList $servicePath

Write-Host "Service started as background job (ID: $($job.Id))" -ForegroundColor Green
Write-Host "Waiting for service to initialize (10 seconds)..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

# Check if job is still running
if ($job.State -eq "Running") {
    Write-Host "[OK] Events service is running" -ForegroundColor Green
    Write-Host "  Job ID: $($job.Id)" -ForegroundColor Gray
    Write-Host "  To view logs: Receive-Job -Id $($job.Id) -Keep" -ForegroundColor Gray
    Write-Host "  To stop: Stop-Job -Id $($job.Id); Remove-Job -Id $($job.Id)" -ForegroundColor Gray
    
    # Return job ID for reference
    return $job.Id
} else {
    Write-Host "[ERROR] Service failed to start" -ForegroundColor Red
    Receive-Job -Id $job.Id
    Remove-Job -Id $job.Id
    exit 1
}
