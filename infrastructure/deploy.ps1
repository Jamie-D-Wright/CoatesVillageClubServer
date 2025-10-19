# Village Club Infrastructure Deployment Script
# Purpose: Deploy Azure infrastructure using Bicep templates

[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [ValidateSet('dev', 'staging', 'prod')]
    [string]$Environment,
    
    [Parameter(Mandatory=$false)]
    [string]$Location = 'uksouth',
    
    [Parameter(Mandatory=$false)]
    [string]$SubscriptionId,
    
    [Parameter(Mandatory=$false)]
    [switch]$WhatIf
)

$ErrorActionPreference = 'Stop'

# Color output functions
function Write-Step {
    param([string]$Message)
    Write-Host "`n[OK] $Message" -ForegroundColor Green
}

function Write-Info {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor Cyan
}

function Write-Warning {
    param([string]$Message)
    Write-Host "[WARN] $Message" -ForegroundColor Yellow
}

function Write-Error {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
}

# Banner
Write-Host ""
Write-Host "=============================================================" -ForegroundColor Cyan
Write-Host "   Village Club Infrastructure Deployment" -ForegroundColor Cyan
Write-Host "   Environment: $Environment" -ForegroundColor Cyan
Write-Host "=============================================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Check prerequisites
Write-Step "Checking prerequisites..."

# Check Azure CLI
try {
    $azVersion = az version --output json | ConvertFrom-Json
    Write-Info "Azure CLI version: $($azVersion.'azure-cli')"
} catch {
    Write-Error "Azure CLI not found. Please install from: https://aka.ms/installazurecliwindows"
    exit 1
}

# Check .NET SDK
try {
    $dotnetVersion = dotnet --version
    Write-Info ".NET SDK version: $dotnetVersion"
} catch {
    Write-Warning ".NET SDK not found. Required for building the application."
}

# Check Azure Functions Core Tools
try {
    $funcVersion = func --version
    Write-Info "Azure Functions Core Tools version: $funcVersion"
} catch {
    Write-Warning "Azure Functions Core Tools not found. Required for deployment."
}

# Step 2: Login to Azure
Write-Step "Checking Azure login status..."
$account = az account show 2>$null | ConvertFrom-Json
if (-not $account) {
    Write-Info "Not logged in. Starting Azure login..."
    az login
    $account = az account show | ConvertFrom-Json
}
Write-Info "Logged in as: $($account.user.name)"

# Step 3: Set subscription
if ($SubscriptionId) {
    Write-Step "Setting subscription to: $SubscriptionId"
    az account set --subscription $SubscriptionId
} else {
    Write-Info "Using subscription: $($account.name) ($($account.id))"
}

# Step 4: Define resource names
$resourceGroupName = "rg-villageclub-$Environment"
$serviceName = "villageclub"

Write-Step "Resource names:"
Write-Info "  Resource Group: $resourceGroupName"
Write-Info "  Location: $Location"
Write-Info "  Service Name: $serviceName"

# Step 5: Create resource group
Write-Step "Creating resource group..."
$rgExists = az group exists --name $resourceGroupName
if ($rgExists -eq 'false') {
    if ($WhatIf) {
        Write-Info "Would create resource group: $resourceGroupName"
    } else {
        az group create --name $resourceGroupName --location $Location
        Write-Info "Resource group created: $resourceGroupName"
    }
} else {
    Write-Info "Resource group already exists: $resourceGroupName"
}

# Step 6: Collect parameters
Write-Step "Collecting deployment parameters..."

# SQL Admin credentials
Write-Info "SQL Server administrator credentials required."
$sqlAdminLogin = Read-Host "SQL Admin Login (default: sqladmin)"
if ([string]::IsNullOrWhiteSpace($sqlAdminLogin)) {
    $sqlAdminLogin = "sqladmin"
}

$sqlAdminPasswordSecure = Read-Host "SQL Admin Password (min 8 chars, complexity required)" -AsSecureString
$BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($sqlAdminPasswordSecure)
$sqlAdminPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)

# APIM Publisher details
$apimPublisherEmail = Read-Host "APIM Publisher Email (default: admin@coatesvillageclub.org)"
if ([string]::IsNullOrWhiteSpace($apimPublisherEmail)) {
    $apimPublisherEmail = "admin@coatesvillageclub.org"
}

$apimPublisherName = Read-Host "APIM Publisher Name (default: Coates Village Club)"
if ([string]::IsNullOrWhiteSpace($apimPublisherName)) {
    $apimPublisherName = "Coates Village Club"
}

# JWT Settings
$jwtIssuer = Read-Host "JWT Issuer (default: https://villageclub.coates.local)"
if ([string]::IsNullOrWhiteSpace($jwtIssuer)) {
    $jwtIssuer = "https://villageclub.coates.local"
}

$jwtAudience = Read-Host "JWT Audience (default: villageclub-api)"
if ([string]::IsNullOrWhiteSpace($jwtAudience)) {
    $jwtAudience = "villageclub-api"
}

# Step 7: Deploy infrastructure
Write-Step "Deploying infrastructure..."

$deploymentName = "villageclub-$Environment-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
$templateFile = Join-Path $PSScriptRoot "main.bicep"

if ($WhatIf) {
    Write-Info "Would deploy with parameters:"
    Write-Info "  Environment: $Environment"
    Write-Info "  Service Name: $serviceName"
    Write-Info "  Location: $Location"
    Write-Info "  SQL Admin Login: $sqlAdminLogin"
    Write-Info "  APIM Publisher Email: $apimPublisherEmail"
    Write-Info "  APIM Publisher Name: $apimPublisherName"
    Write-Info "  JWT Issuer: $jwtIssuer"
    Write-Info "  JWT Audience: $jwtAudience"
    Write-Info ""
    Write-Info "Running what-if analysis..."
    
    az deployment group what-if `
        --resource-group $resourceGroupName `
        --name $deploymentName `
        --template-file $templateFile `
        --parameters environmentName=$Environment `
        --parameters serviceName=$serviceName `
        --parameters location=$Location `
        --parameters sqlAdminLogin=$sqlAdminLogin `
        --parameters sqlAdminPassword=$sqlAdminPassword `
        --parameters apimPublisherEmail=$apimPublisherEmail `
        --parameters "apimPublisherName=$apimPublisherName" `
        --parameters jwtIssuer=$jwtIssuer `
        --parameters jwtAudience=$jwtAudience
} else {
    Write-Info "Starting deployment (this may take 10-15 minutes)..."
    Write-Info "Deployment name: $deploymentName"
    
    az deployment group create `
        --resource-group $resourceGroupName `
        --name $deploymentName `
        --template-file $templateFile `
        --parameters environmentName=$Environment `
        --parameters serviceName=$serviceName `
        --parameters location=$Location `
        --parameters sqlAdminLogin=$sqlAdminLogin `
        --parameters sqlAdminPassword=$sqlAdminPassword `
        --parameters apimPublisherEmail=$apimPublisherEmail `
        --parameters "apimPublisherName=$apimPublisherName" `
        --parameters jwtIssuer=$jwtIssuer `
        --parameters jwtAudience=$jwtAudience
    
    if ($LASTEXITCODE -eq 0) {
        Write-Step "Deployment completed successfully!"
    } else {
        Write-Error "Deployment failed. Check the error messages above."
        exit 1
    }
}

# Step 8: Initialize database schemas
Write-Step "Database schema initialization..."
Write-Warning "Database schemas need to be created manually using Entity Framework migrations."
Write-Info "Run the following commands after deploying the application:"
Write-Info "  cd services/membership/src/VillageClub.Membership"
Write-Info "  dotnet ef database update"

# Step 9: Get deployment outputs
if (-not $WhatIf) {
    Write-Step "Retrieving deployment outputs..."
    $deployment = az deployment group show `
        --resource-group $resourceGroupName `
        --name $deploymentName `
        --query properties.outputs `
        --output json | ConvertFrom-Json
    
    Write-Host ""
    Write-Host "=============================================================" -ForegroundColor Green
    Write-Host "   Deployment Outputs" -ForegroundColor Green
    Write-Host "=============================================================" -ForegroundColor Green
    Write-Host ""
    
    if ($deployment.membershipFunctionAppName) {
        Write-Info "Membership Function App: $($deployment.membershipFunctionAppName.value)"
    }
    if ($deployment.membershipFunctionAppUrl) {
        Write-Info "Membership URL: $($deployment.membershipFunctionAppUrl.value)"
    }
    if ($deployment.sqlServerFqdn) {
        Write-Info "SQL Server: $($deployment.sqlServerFqdn.value)"
    }
    if ($deployment.databaseName) {
        Write-Info "Database: $($deployment.databaseName.value)"
    }
    if ($deployment.apimGatewayUrl) {
        Write-Info "API Gateway URL: $($deployment.apimGatewayUrl.value)"
    }
    if ($deployment.keyVaultName) {
        Write-Info "Key Vault: $($deployment.keyVaultName.value)"
    }
    if ($deployment.receiptsStorageAccountName) {
        Write-Info "Receipts Storage: $($deployment.receiptsStorageAccountName.value)"
    }
}

# Step 10: Next steps
Write-Host ""
Write-Host "=============================================================" -ForegroundColor Cyan
Write-Host "   Next Steps" -ForegroundColor Cyan
Write-Host "=============================================================" -ForegroundColor Cyan
Write-Host ""
Write-Info "1. Build and deploy the Membership service:"
Write-Info "   cd services/membership/src/VillageClub.Membership"
Write-Info "   func azure functionapp publish func-villageclub-membership-$Environment"
Write-Info ""
Write-Info "2. Run database migrations:"
Write-Info "   dotnet ef database update"
Write-Info ""
Write-Info "3. Test the health endpoint:"
Write-Info "   curl https://func-villageclub-membership-$Environment.azurewebsites.net/api/v1/health"
Write-Info ""
Write-Info "4. Configure APIM with the Membership backend URL"
Write-Info ""
Write-Info "5. View deployment in Azure Portal:"
Write-Info "   https://portal.azure.com/#@/resource/subscriptions/$($account.id)/resourceGroups/$resourceGroupName"

Write-Host "`n[SUCCESS] Deployment script completed!" -ForegroundColor Green
