# API Testing with Postman and Newman

This guide documents the standard approach for API testing in the CoatesVillageClubServer project, as required by Constitution Principle IX: Local-First Development.

## Overview

All API endpoints must be tested locally using Postman collections before deployment to Azure. The `newman` CLI tool enables automated, repeatable API testing in local development, CI/CD pipelines, and production verification.

## Standard Structure

```
tests/postman/
├── [service-name].postman_collection.json   # Test collection for each service
├── local.postman_environment.json           # Local environment variables
├── dev.postman_environment.json             # Dev environment (optional)
└── README.md                                # Service-specific test documentation
```

## Creating Postman Collections

### 1. Collection Organization

Organize tests into logical folders:

```json
{
  "info": {
    "name": "[Service Name] API Tests",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "item": [
    {
      "name": "Health Checks",
      "item": [ /* health endpoint tests */ ]
    },
    {
      "name": "Authentication",
      "item": [ /* auth endpoint tests */ ]
    },
    {
      "name": "CRUD Operations",
      "item": [ /* business logic tests */ ]
    },
    {
      "name": "Validation Tests",
      "item": [ /* error handling tests */ ]
    }
  ]
}
```

### 2. Request Structure

Each request should use environment variables for flexibility:

```json
{
  "name": "Create User",
  "request": {
    "method": "POST",
    "header": [
      {
        "key": "Authorization",
        "value": "Bearer {{accessToken}}",
        "type": "text"
      }
    ],
    "url": {
      "raw": "{{baseUrl}}/users",
      "host": ["{{baseUrl}}"],
      "path": ["users"]
    },
    "body": {
      "mode": "raw",
      "raw": "{ /* request body */ }",
      "options": {
        "raw": {
          "language": "json"
        }
      }
    }
  }
}
```

### 3. Test Scripts

Add test scripts with assertions:

```javascript
// Test script example
pm.test("Status code is 201", function () {
    pm.response.to.have.status(201);
});

pm.test("Response has userId", function () {
    var jsonData = pm.response.json();
    pm.expect(jsonData).to.have.property('userId');
    
    // Store for later tests
    pm.environment.set("testUserId", jsonData.userId);
});

pm.test("Email format is valid", function () {
    var jsonData = pm.response.json();
    pm.expect(jsonData.email).to.match(/^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$/);
});
```

### 4. Pre-Request Scripts

Use pre-request scripts for dynamic data:

```javascript
// Generate unique email
const timestamp = Date.now();
pm.environment.set("uniqueEmail", `test.user.${timestamp}@example.com`);

// Generate random data
const randomName = `User_${Math.random().toString(36).substring(7)}`;
pm.environment.set("randomName", randomName);
```

## Environment Files

### Local Environment Template

```json
{
  "id": "local-env",
  "name": "Local Environment",
  "values": [
    {
      "key": "baseUrl",
      "value": "http://localhost:7071/api/v1",
      "enabled": true
    },
    {
      "key": "accessToken",
      "value": "",
      "enabled": true
    },
    {
      "key": "refreshToken",
      "value": "",
      "enabled": true
    },
    {
      "key": "testUserId",
      "value": "",
      "enabled": true
    }
  ]
}
```

**Important**: 
- `baseUrl` should include the route prefix (e.g., `/api/v1` for Azure Functions)
- Token and ID variables are typically empty and populated by test scripts
- Keep sensitive data out of environment files (use `.gitignore`)

## Running Tests with Newman

### Installation

```powershell
# Install newman globally
npm install -g newman

# Verify installation
newman --version
```

### Basic Usage

```powershell
# Run entire collection
newman run tests/postman/membership-service.postman_collection.json `
  -e tests/postman/local.postman_environment.json

# Run with detailed output
newman run tests/postman/membership-service.postman_collection.json `
  -e tests/postman/local.postman_environment.json `
  --verbose

# Run specific folder
newman run tests/postman/membership-service.postman_collection.json `
  -e tests/postman/local.postman_environment.json `
  --folder "Authentication"
```

### HTML Reports

Generate HTML reports for test results:

```powershell
# Install HTML reporter
npm install -g newman-reporter-html

# Run with HTML report
newman run tests/postman/membership-service.postman_collection.json `
  -e tests/postman/local.postman_environment.json `
  -r html `
  --reporter-html-export tests/postman/reports/test-results.html
```

### CI/CD Integration

```powershell
# Run with exit code on failure (for CI/CD)
newman run tests/postman/membership-service.postman_collection.json `
  -e tests/postman/local.postman_environment.json `
  --bail `
  --color off
```

## Testing Patterns

### 1. Sequential Testing (Authentication Flow)

Tests that depend on previous test results:

```javascript
// Test 1: Login
pm.test("Login successful", function () {
    var jsonData = pm.response.json();
    pm.environment.set("accessToken", jsonData.accessToken);
    pm.environment.set("refreshToken", jsonData.refreshToken);
});

// Test 2: Get Profile (uses token from Test 1)
// Header: Authorization: Bearer {{accessToken}}
```

### 2. Validation Testing

Test error handling and validation:

```json
{
  "name": "Create User - Missing Email",
  "request": {
    "method": "POST",
    "body": {
      "raw": "{ \"firstName\": \"Test\", \"lastName\": \"User\" }"
    }
  },
  "event": [
    {
      "listen": "test",
      "script": {
        "exec": [
          "pm.test(\"Status code is 400\", function () {",
          "    pm.response.to.have.status(400);",
          "});",
          "",
          "pm.test(\"Error message mentions email\", function () {",
          "    var jsonData = pm.response.json();",
          "    pm.expect(jsonData.message.toLowerCase()).to.include('email');",
          "});"
        ]
      }
    }
  ]
}
```

### 3. Cleanup

Add cleanup tests at the end:

```javascript
// Delete test user
pm.test("Cleanup successful", function () {
    pm.response.to.have.status(204);
    
    // Clear environment variables
    pm.environment.unset("testUserId");
    pm.environment.unset("accessToken");
    pm.environment.unset("refreshToken");
});
```

## Best Practices

### 1. Collection Design
- ✅ Group related tests into folders
- ✅ Use descriptive names for requests
- ✅ Include both success and error scenarios
- ✅ Test edge cases and boundary conditions
- ✅ Add cleanup tests to remove test data

### 2. Test Scripts
- ✅ Test status codes first
- ✅ Validate response structure
- ✅ Test business logic requirements
- ✅ Store variables for dependent tests
- ✅ Use meaningful assertion messages

### 3. Environment Management
- ✅ Use environment variables for all URLs
- ✅ Use variables for tokens and IDs
- ✅ Keep local and Azure environments separate
- ❌ Never commit tokens or secrets to Git
- ✅ Document required environment variables

### 4. Documentation
- ✅ Include README.md in tests/postman/ directory
- ✅ Document prerequisites (local services, database)
- ✅ Provide example newman commands
- ✅ Explain test coverage and organization
- ✅ Include troubleshooting section

## Integration with Local Development

### Prerequisites

Before running API tests, ensure:

1. **Local services running**:
   ```powershell
   # Azure Functions
   func start --port 7071
   
   # Or use VS Code task: "func: start"
   ```

2. **Azurite running** (for Azure Storage):
   ```powershell
   azurite --silent --location ./azurite --debug ./azurite/debug.log
   ```

3. **SQL Server running** (Docker):
   ```powershell
   docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong@Passw0rd" `
     -p 1433:1433 --name sql-server `
     -d mcr.microsoft.com/mssql/server:2022-latest
   ```

### Workflow

1. **Start local services**
2. **Run tests with newman**:
   ```powershell
   newman run tests/postman/membership-service.postman_collection.json `
     -e tests/postman/local.postman_environment.json
   ```
3. **Review results** (should see all tests passing)
4. **Make code changes**
5. **Rerun tests** to verify changes

## Constitution Compliance

This testing approach satisfies Constitution Principle IX:

> **Principle IX: Local-First Development**
> 
> All features are **tested locally first** before any Azure deployment.
> Use local Azure Function Core Tools, Azurite emulator, and local SQL Server.
> Code MUST work locally before deploying to Azure.

API testing with Postman/newman enables:
- ✅ Automated verification of all endpoints locally
- ✅ Repeatable test execution in development and CI/CD
- ✅ Confidence that APIs work before Azure deployment
- ✅ Quick feedback on breaking changes
- ✅ Documentation of expected API behavior

## Troubleshooting

### Common Issues

**404 Not Found**
- Check route prefix in `host.json` (e.g., `api/v1`)
- Verify `baseUrl` includes route prefix
- Ensure Function app is running

**401 Unauthorized**
- Check if token is required for endpoint
- Verify token is set in environment variables
- Check token expiration

**500 Internal Server Error**
- Check Function app logs
- Verify database connection
- Check Azurite is running
- Review local.settings.json configuration

**Tests fail with "unable to verify first certificate"**
- This occurs with self-signed certificates
- Use `--insecure` flag: `newman run ... --insecure`
- Or configure SSL properly for local development

## Reference Examples

See working examples in:
- `tests/postman/membership-service.postman_collection.json`
- `tests/postman/local.postman_environment.json`
- `tests/postman/README.md`

## Additional Resources

- [Newman Documentation](https://learning.postman.com/docs/running-collections/using-newman-cli/command-line-integration-with-newman/)
- [Postman Test Scripts](https://learning.postman.com/docs/writing-scripts/test-scripts/)
- [Azure Functions Local Development](https://learn.microsoft.com/azure/azure-functions/functions-develop-local)
