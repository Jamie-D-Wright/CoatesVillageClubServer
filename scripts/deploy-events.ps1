# Deploy Events Service to Azure
# This script ensures the correct code is deployed to the correct function app

[CmdletBinding()]
param(
    [Parameter(Mandatory=$false)]
    [ValidateSet('dev', 'staging', 'prod')]
    [string]$Environment = 'dev'
)

$ErrorActionPreference = "Stop"

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  Events Service Deployment" -ForegroundColor Cyan
Write-Host "  Environment: $Environment" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Step 1: Validate we're in the correct repository
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Split-Path -Parent $scriptDir

if (-not (Test-Path "$rootDir\services\events\src\VillageClub.Events\VillageClub.Events.csproj")) {
    Write-Host "[ERROR] Cannot find Events service project" -ForegroundColor Red
    Write-Host "  Expected: $rootDir\services\events\src\VillageClub.Events\VillageClub.Events.csproj" -ForegroundColor Red
    exit 1
}

# Step 2: Set correct directory
$eventsDir = "$rootDir\services\events\src\VillageClub.Events"
Write-Host "[1/4] Navigating to Events service directory..." -ForegroundColor Yellow
Write-Host "  Path: $eventsDir" -ForegroundColor Gray
Set-Location $eventsDir

# Step 3: Build the project
Write-Host "`n[2/4] Building Events service..." -ForegroundColor Yellow
dotnet build --configuration Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERROR] Build failed" -ForegroundColor Red
    exit 1
}
Write-Host "[OK] Build successful" -ForegroundColor Green

# Step 4: Determine function app name
$functionAppName = "cvc-func-events-$Environment"
Write-Host "`n[3/4] Target Function App: $functionAppName" -ForegroundColor Yellow

# Step 5: Confirm deployment
Write-Host "`n[WARNING] You are about to deploy EVENTS code to $functionAppName" -ForegroundColor Yellow
$confirmation = Read-Host "Continue? (yes/no)"
if ($confirmation -ne 'yes') {
    Write-Host "[CANCELLED] Deployment cancelled" -ForegroundColor Yellow
    exit 0
}

# Step 6: Publish
Write-Host "`n[4/4] Publishing to Azure..." -ForegroundColor Yellow
func azure functionapp publish $functionAppName --dotnet-isolated

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n========================================" -ForegroundColor Green
    Write-Host "  [SUCCESS] Deployment Complete!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Verify deployment:" -ForegroundColor Cyan
    Write-Host "  Health: https://$functionAppName.azurewebsites.net/api/v1/health" -ForegroundColor Gray
    Write-Host "  Expected: {`"service`":`"Events`",`"status`":`"Healthy`"}" -ForegroundColor Gray
    Write-Host ""
} else {
    Write-Host "`n[ERROR] Deployment failed" -ForegroundColor Red
    exit 1
}
