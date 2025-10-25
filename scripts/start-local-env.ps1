<#
.SYNOPSIS
    Starts local development environment for Village Club Management system.

.DESCRIPTION
    This script automates the startup of all required local infrastructure:
    - Docker Desktop (verification)
    - SQL Server container
    - Azurite storage emulator (if needed standalone)

.PARAMETER SkipDockerCheck
    Skip Docker Desktop running verification (use if Docker is managed externally)

.PARAMETER SqlPassword
    SQL Server SA password (default: YourStrong!Passw0rd)

.PARAMETER ContainerName
    SQL Server container name (default: sqlserver-villageclub)

.EXAMPLE
    .\start-local-env.ps1
    Starts all services with default settings

.EXAMPLE
    .\start-local-env.ps1 -SqlPassword "MyCustomP@ssw0rd"
    Starts services with custom SQL password

.NOTES
    Author: Village Club Development Team
    Version: 1.0.0
    Last Updated: 2025-10-25
#>

[CmdletBinding()]
param(
    [switch]$SkipDockerCheck,
    [string]$SqlPassword = "YourStrong!Passw0rd",
    [string]$ContainerName = "sqlserver-villageclub"
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

Write-Host "`n================================================" -ForegroundColor Magenta
Write-Host "  Village Club - Local Environment Startup" -ForegroundColor Magenta
Write-Host "================================================`n" -ForegroundColor Magenta

# Step 1: Verify Docker Desktop is running
if (-not $SkipDockerCheck) {
    Write-Host "ℹ Checking Docker Desktop status..." -ForegroundColor Cyan
    try {
        $dockerInfo = docker info 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Host "✗ Docker Desktop is not running" -ForegroundColor Red
            Write-Host "`nPlease start Docker Desktop and wait for it to fully initialize."
            Write-Host "Look for 'Docker Desktop is running' (green indicator) in system tray.`n"
            exit 1
        }
        Write-Host "✓ Docker Desktop is running" -ForegroundColor Green
    }
    catch {
        Write-Host "✗ Docker command not found" -ForegroundColor Red
        Write-Host "`nPlease install Docker Desktop from: https://www.docker.com/products/docker-desktop`n"
        exit 1
    }
}

# Step 2: Check if SQL Server container exists
Write-Host "ℹ Checking SQL Server container status..." -ForegroundColor Cyan
$containerExists = docker ps -a --filter "name=$ContainerName" --format "{{.Names}}" 2>$null

if ($containerExists -eq $ContainerName) {
    # Container exists - check if it's running
    $containerStatus = docker ps --filter "name=$ContainerName" --format "{{.Status}}" 2>$null
    
    if ($containerStatus) {
        Write-Host "✓ SQL Server container is already running" -ForegroundColor Green
        Write-Host "  Status: $containerStatus" -ForegroundColor Gray
    }
    else {
        Write-Host "ℹ Starting existing SQL Server container..." -ForegroundColor Cyan
        docker start $ContainerName | Out-Null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ SQL Server container started" -ForegroundColor Green
            Start-Sleep -Seconds 3
        }
        else {
            Write-Host "✗ Failed to start SQL Server container" -ForegroundColor Red
            exit 1
        }
    }
}
else {
    # Container doesn't exist - create and start it
    Write-Host "ℹ Creating new SQL Server container..." -ForegroundColor Cyan
    
    $dockerCmd = @(
        "run"
        "-e", "ACCEPT_EULA=Y"
        "-e", "SA_PASSWORD=$SqlPassword"
        "-p", "1433:1433"
        "--name", $ContainerName
        "-d"
        "mcr.microsoft.com/mssql/server:2022-latest"
    )
    
    docker @dockerCmd | Out-Null
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ SQL Server container created and started" -ForegroundColor Green
        Write-Host "ℹ Waiting for SQL Server to initialize (10 seconds)..." -ForegroundColor Cyan
        Start-Sleep -Seconds 10
    }
    else {
        Write-Host "✗ Failed to create SQL Server container" -ForegroundColor Red
        Write-Host "`nTroubleshooting:"
        Write-Host "  - Check if port 1433 is already in use"
        Write-Host "  - Verify Docker Desktop has sufficient resources (2GB+ RAM)`n"
        exit 1
    }
}

# Step 3: Verify SQL Server connectivity
Write-Host "ℹ Testing SQL Server connectivity..." -ForegroundColor Cyan
$sqlConnectable = Test-NetConnection -ComputerName localhost -Port 1433 -InformationLevel Quiet -WarningAction SilentlyContinue

if ($sqlConnectable) {
    Write-Host "✓ SQL Server is accessible on port 1433" -ForegroundColor Green
}
else {
    Write-Host "⚠ SQL Server port 1433 is not responding yet" -ForegroundColor Yellow
    Write-Host "ℹ Waiting additional 10 seconds for SQL Server to fully start..." -ForegroundColor Cyan
    Start-Sleep -Seconds 10
    
    $sqlConnectable = Test-NetConnection -ComputerName localhost -Port 1433 -InformationLevel Quiet -WarningAction SilentlyContinue
    if ($sqlConnectable) {
        Write-Host "✓ SQL Server is now accessible" -ForegroundColor Green
    }
    else {
        Write-Host "✗ SQL Server is still not accessible after 20 seconds" -ForegroundColor Red
        Write-Host "`nCheck container logs with: docker logs $ContainerName`n"
        exit 1
    }
}

# Step 4: Check Azurite status (built into Functions Core Tools v4)
Write-Host "ℹ Checking Azurite status..." -ForegroundColor Cyan
$azuriteRunning = Test-NetConnection -ComputerName localhost -Port 10000 -InformationLevel Quiet -WarningAction SilentlyContinue

if ($azuriteRunning) {
    Write-Host "✓ Azurite storage emulator is running" -ForegroundColor Green
}
else {
    Write-Host "ℹ Azurite will start automatically with Azure Functions runtime" -ForegroundColor Cyan
    Write-Host "  No manual startup required (built into Functions Core Tools v4)" -ForegroundColor Gray
}

# Step 5: Verify local.settings.json exists
Write-Host "ℹ Checking Membership service configuration..." -ForegroundColor Cyan
$localSettingsPath = ".\services\membership\local.settings.json"

if (Test-Path $localSettingsPath) {
    Write-Host "✓ local.settings.json found" -ForegroundColor Green
}
else {
    Write-Host "⚠ local.settings.json not found" -ForegroundColor Yellow
    
    $templatePath = ".\services\membership\local.settings.json.example"
    if (Test-Path $templatePath) {
        Write-Host "ℹ Copying from template..." -ForegroundColor Cyan
        Copy-Item $templatePath $localSettingsPath
        Write-Host "✓ Created local.settings.json from template" -ForegroundColor Green
        Write-Host "`n  Please review and update values in: $localSettingsPath" -ForegroundColor Yellow
    }
    else {
        Write-Host "✗ Template file not found: $templatePath" -ForegroundColor Red
        Write-Host "`nPlease create local.settings.json manually.`n"
    }
}

# Summary
Write-Host "`n================================================" -ForegroundColor Magenta
Write-Host "  Environment Status Summary" -ForegroundColor Magenta
Write-Host "================================================`n" -ForegroundColor Magenta

Write-Host "Service                   Status" -ForegroundColor Cyan
Write-Host "------------------------  -----------------"
Write-Host "Docker Desktop            " -NoNewline
Write-Host "Running" -ForegroundColor Green
Write-Host "SQL Server Container      " -NoNewline
Write-Host "Running" -ForegroundColor Green
Write-Host "SQL Server Port (1433)    " -NoNewline
if ($sqlConnectable) { Write-Host "Accessible" -ForegroundColor Green } else { Write-Host "Not Accessible" -ForegroundColor Red }
Write-Host "Azurite (Port 10000)      " -NoNewline
if ($azuriteRunning) { Write-Host "Running" -ForegroundColor Green } else { Write-Host "Auto-start with Functions" -ForegroundColor Cyan }
Write-Host "Configuration File        " -NoNewline
if (Test-Path $localSettingsPath) { Write-Host "Found" -ForegroundColor Green } else { Write-Host "Missing" -ForegroundColor Yellow }

Write-Host "`n================================================" -ForegroundColor Magenta
Write-Host "  Next Steps" -ForegroundColor Magenta
Write-Host "================================================`n" -ForegroundColor Magenta

Write-Host "1. Apply EF Core migrations:" -ForegroundColor Cyan
Write-Host "   cd services\membership\src\VillageClub.Membership" -ForegroundColor Gray
Write-Host "   dotnet ef database update" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Start Membership service:" -ForegroundColor Cyan
Write-Host "   cd services\membership" -ForegroundColor Gray
Write-Host "   func start --port 7071" -ForegroundColor Gray
Write-Host ""
Write-Host "3. Test health endpoint:" -ForegroundColor Cyan
Write-Host "   Invoke-WebRequest -Uri 'http://localhost:7071/api/v1/health' -UseBasicParsing" -ForegroundColor Gray
Write-Host ""
Write-Host "For detailed setup guide, see: docs\LOCAL-DEVELOPMENT.md`n" -ForegroundColor Yellow

# Connection strings for reference
Write-Host "`n================================================" -ForegroundColor Magenta
Write-Host "  Connection Strings (for reference)" -ForegroundColor Magenta
Write-Host "================================================`n" -ForegroundColor Magenta

Write-Host "SQL Server:" -ForegroundColor Cyan
Write-Host "  Server=localhost,1433;Database=VillageClubDB;User Id=sa;Password=$SqlPassword;TrustServerCertificate=True" -ForegroundColor Gray
Write-Host ""
Write-Host "Azurite:" -ForegroundColor Cyan
Write-Host "  UseDevelopmentStorage=true" -ForegroundColor Gray
Write-Host ""

Write-Host "Local environment is ready!`n" -ForegroundColor Green

