# Village Club Infrastructure Cleanup Script
# Purpose: Remove all Azure resources for a specific environment

[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [ValidateSet('dev', 'staging', 'prod')]
    [string]$Environment,
    
    [Parameter(Mandatory=$false)]
    [switch]$Force
)

$ErrorActionPreference = 'Stop'

# Color output functions
function Write-Warning {
    param([string]$Message)
    Write-Host "[WARN] $Message" -ForegroundColor Yellow
}

function Write-Error {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
}

function Write-Info {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "[OK] $Message" -ForegroundColor Green
}

# Banner
Write-Host ""
Write-Host "=============================================================" -ForegroundColor Red
Write-Host "   Village Club Infrastructure Cleanup" -ForegroundColor Red
Write-Host "   Environment: $Environment" -ForegroundColor Red
Write-Host "=============================================================" -ForegroundColor Red
Write-Host ""

# Define resource group name
$resourceGroupName = "rg-villageclub-$Environment"

# Warning for production
if ($Environment -eq 'prod' -and -not $Force) {
    Write-Error "PRODUCTION ENVIRONMENT DETECTED!"
    Write-Warning "You are about to delete the PRODUCTION environment."
    Write-Warning "This will permanently delete all data, databases, and configurations."
    Write-Warning ""
    Write-Warning "To proceed with production cleanup, use the -Force parameter:"
    Write-Warning "  .\cleanup.ps1 -Environment prod -Force"
    exit 1
}

# Check if resource group exists
$rgExists = az group exists --name $resourceGroupName

if ($rgExists -eq 'false') {
    Write-Info "Resource group '$resourceGroupName' does not exist. Nothing to clean up."
    exit 0
}

# List resources that will be deleted
Write-Warning "The following resource group will be PERMANENTLY DELETED:"
Write-Info "  Resource Group: $resourceGroupName"
Write-Info "  Location: $(az group show --name $resourceGroupName --query location -o tsv)"
Write-Info ""

# Get resource count
$resources = az resource list --resource-group $resourceGroupName --query "[].{Name:name, Type:type}" -o json | ConvertFrom-Json
Write-Warning "This will delete $($resources.Count) resources:"
foreach ($resource in $resources) {
    Write-Info "  - $($resource.Name) ($($resource.Type))"
}

Write-Host ""
Write-Error "⚠️  WARNING: THIS ACTION IS IRREVERSIBLE! ⚠️"
Write-Error "All data will be permanently lost, including:"
Write-Host "  - Database contents (users, events, expenses, etc.)" -ForegroundColor Red
Write-Host "  - Blob storage (receipts, documents)" -ForegroundColor Red
Write-Host "  - Key Vault secrets" -ForegroundColor Red
Write-Host "  - Application Insights logs" -ForegroundColor Red
Write-Host "  - APIM configurations" -ForegroundColor Red
Write-Host ""

# Confirmation prompt
if (-not $Force) {
    $confirmation = Read-Host "Type 'DELETE-$Environment' to confirm deletion"
    if ($confirmation -ne "DELETE-$Environment") {
        Write-Info "Cleanup cancelled. No resources were deleted."
        exit 0
    }
    
    # Double confirmation for staging/prod
    if ($Environment -ne 'dev') {
        Write-Warning "Final confirmation required for $Environment environment."
        $finalConfirmation = Read-Host "Type 'YES-DELETE-$Environment' to proceed"
        if ($finalConfirmation -ne "YES-DELETE-$Environment") {
            Write-Info "Cleanup cancelled. No resources were deleted."
            exit 0
        }
    }
}

# Proceed with deletion
Write-Host ""
Write-Warning "Starting deletion of resource group: $resourceGroupName"
Write-Info "This may take several minutes..."

try {
    az group delete --name $resourceGroupName --yes --no-wait
    Write-Success "Deletion initiated successfully!"
    Write-Info "Resource group '$resourceGroupName' is being deleted in the background."
    Write-Info ""
    Write-Info "To check deletion status:"
    Write-Info "  az group show --name $resourceGroupName"
    Write-Info ""
    Write-Info "The resource group should be fully deleted within 5-10 minutes."
} catch {
    Write-Error "Failed to delete resource group: $_"
    exit 1
}

Write-Host ""
Write-Success "Cleanup script completed!"
