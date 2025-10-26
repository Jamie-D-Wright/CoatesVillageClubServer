# Postman Test Suite for Membership Service

This directory contains comprehensive API tests for the Village Club Membership Service.

## Contents

- `membership-service.postman_collection.json` - Complete test collection with 16+ test scenarios
- `local.postman_environment.json` - Environment configuration for local testing

## Test Coverage

### Health Checks
- Health endpoint validation
- Readiness endpoint validation

### Authentication Flow
- User registration with validation
- Login with valid/invalid credentials
- Token refresh
- Password change
- User logout

### User Management
- Get current user
- Get user by ID
- Get all users (paginated)
- Create user (admin)
- Update user
- Delete user

### Validation Tests
- Invalid email format
- Weak password validation
- Missing required fields

## Prerequisites

1. **Newman** - Postman CLI test runner
   ```powershell
   npm install -g newman
   ```

2. **Newman HTML Reporter** (optional, for HTML reports)
   ```powershell
   npm install -g newman-reporter-html
   ```

3. **Running Membership Service**
   - Ensure the membership service is running locally on port 7071
   - Start with: `func start` from the membership service directory

## Running Tests

### Basic Run (CLI Output Only)
```powershell
newman run membership-service.postman_collection.json -e local.postman_environment.json
```

### With HTML Report
```powershell
newman run membership-service.postman_collection.json -e local.postman_environment.json -r html,cli --reporter-html-export report.html
```

### Run Specific Folder
```powershell
# Run only Health Checks
newman run membership-service.postman_collection.json -e local.postman_environment.json --folder "Health Checks"

# Run only Authentication tests
newman run membership-service.postman_collection.json -e local.postman_environment.json --folder "Authentication"

# Run only User Management tests
newman run membership-service.postman_collection.json -e local.postman_environment.json --folder "User Management"
```

### With Detailed Output
```powershell
newman run membership-service.postman_collection.json -e local.postman_environment.json --verbose
```

### Export Results to JSON
```powershell
newman run membership-service.postman_collection.json -e local.postman_environment.json -r json --reporter-json-export results.json
```

## Test Execution Flow

The tests are designed to run sequentially and build upon each other:

1. **Health Checks** - Verify service is running
2. **Register New User** - Creates a test user with unique email
3. **Login** - Obtains access token and refresh token
4. **Authenticated Operations** - Uses token for protected endpoints
5. **Validation Tests** - Test error handling and validation

## Environment Variables

The collection uses the following environment variables (auto-managed by tests):

- `baseUrl` - API base URL (default: http://localhost:7071)
- `accessToken` - JWT access token (set after login)
- `refreshToken` - JWT refresh token (set after login)
- `testUserId` - ID of created test user
- `adminCreatedUserId` - ID of admin-created user

## Importing to Postman

You can also import these collections into Postman Desktop:

1. Open Postman
2. Click **Import**
3. Select both files:
   - `membership-service.postman_collection.json`
   - `local.postman_environment.json`
4. Select the "Membership Service - Local" environment
5. Run the collection

## Continuous Integration

To integrate with CI/CD pipelines:

```yaml
# Example GitHub Actions
- name: Run API Tests
  run: |
    npm install -g newman
    newman run tests/postman/membership-service.postman_collection.json \
      -e tests/postman/local.postman_environment.json \
      -r cli,json \
      --reporter-json-export test-results.json
```

## Troubleshooting

### Service Not Running
If tests fail with connection errors, ensure the membership service is running:
```powershell
cd services/membership/src/VillageClub.Membership
func start --port 7071
```

### Database Issues
Ensure the local database is properly configured and migrations are applied.

### Port Conflicts
If port 7071 is in use, update the `baseUrl` in `local.postman_environment.json`

## Adding New Tests

To add new test scenarios:

1. Add request to appropriate folder in the collection
2. Add test scripts in the "Tests" tab
3. Use environment variables for dynamic data
4. Follow existing patterns for consistency

## Test Results Interpretation

- **Green/Passed** - All assertions passed
- **Red/Failed** - One or more assertions failed
- **Skipped** - Test was not executed

Each test includes multiple assertions to verify:
- HTTP status codes
- Response structure
- Data validity
- Business logic correctness
