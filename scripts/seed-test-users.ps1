# Seed Test Users for E2E Testing
# This script registers test users via the Membership service API
# Requires: Membership service running on port 7071

$ErrorActionPreference = "Stop"

Write-Host "Seeding Test Users for E2E Testing..." -ForegroundColor Cyan
Write-Host ""

$membershipBaseUrl = "http://localhost:7071/api/v1"

# Test if Membership service is running
try {
    $healthCheck = Invoke-WebRequest -Uri "$membershipBaseUrl/health" -Method GET -ErrorAction Stop
    Write-Host "[OK] Membership service is running" -ForegroundColor Green
} catch {
    Write-Host "[ERROR] Membership service is not running on port 7071" -ForegroundColor Red
    Write-Host "        Start it with: .\scripts\start-membership-service.ps1" -ForegroundColor Yellow
    exit 1
}

Write-Host ""

# Test users to create
# UserRole enum: Member=0, Volunteer=1, Committee=2
$testUsers = @(
    @{
        email = "committee@test.com"
        password = "Committee123!"
        firstName = "Test"
        lastName = "Committee"
        role = 2  # Committee (UserRole enum: Member=0, Volunteer=1, Committee=2)
        committeeRole = 0  # Treasurer (CommitteeRole enum: Treasurer=0, Chairman=1, Clerk=2, BarManager=3, GeneralMember=4)
    },
    @{
        email = "member@test.com"
        password = "Member123!"
        firstName = "Test"
        lastName = "Member"
        role = 0  # Member
    }
)

$successCount = 0
$skippedCount = 0

foreach ($user in $testUsers) {
    $roleName = switch ($user.role) {
        0 { "Member" }
        1 { "Volunteer" }
        2 { "Committee" }
        default { "Unknown" }
    }
    
    Write-Host "Creating user: $($user.email) (Role: $roleName)" -ForegroundColor Yellow
    
    $bodyObj = @{
        email = $user.email
        password = $user.password
        firstName = $user.firstName
        lastName = $user.lastName
        role = $user.role
    }
    
    # Add committeeRole if present (required for Committee members)
    if ($null -ne $user.committeeRole) {
        $bodyObj.committeeRole = $user.committeeRole
    }
    
    $body = $bodyObj | ConvertTo-Json
    
    try {
        $response = Invoke-WebRequest `
            -Uri "$membershipBaseUrl/auth/register" `
            -Method POST `
            -Headers @{"Content-Type" = "application/json"} `
            -Body $body `
            -ErrorAction Stop
        
        if ($response.StatusCode -eq 201) {
            Write-Host "  [OK] User created successfully" -ForegroundColor Green
            $successCount++
        } else {
            Write-Host "  [WARN] Unexpected status code: $($response.StatusCode)" -ForegroundColor Yellow
        }
    } catch {
        $errorResponse = $_.Exception.Response
        if ($errorResponse -and $errorResponse.StatusCode -eq 400) {
            # User might already exist
            Write-Host "  [SKIP] User already exists (400 Bad Request)" -ForegroundColor Gray
            $skippedCount++
        } else {
            Write-Host "  [ERROR] Failed to create user: $($_.Exception.Message)" -ForegroundColor Red
            throw
        }
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Seeding Complete" -ForegroundColor Cyan
Write-Host "  Created: $successCount" -ForegroundColor Green
Write-Host "  Skipped: $skippedCount" -ForegroundColor Gray
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Test Credentials:" -ForegroundColor Yellow
Write-Host "  Committee User:" -ForegroundColor White
Write-Host "    Email:    committee@test.com" -ForegroundColor Gray
Write-Host "    Password: Committee123!" -ForegroundColor Gray
Write-Host "  Regular Member:" -ForegroundColor White
Write-Host "    Email:    member@test.com" -ForegroundColor Gray
Write-Host "    Password: Member123!" -ForegroundColor Gray
Write-Host ""
