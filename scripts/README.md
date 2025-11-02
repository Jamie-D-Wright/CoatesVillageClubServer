# Service Management & Debugging Scripts

This directory contains scripts for managing and debugging services during local development.

## 🚀 Quick Start

**Run full E2E test suite (Membership Service):**
```powershell
.\scripts\test-e2e.ps1
```

**Run full E2E test suite (Events Service):**
```powershell
.\scripts\test-e2e-events.ps1
```

**Check service status:**
```powershell
.\scripts\debug-service.ps1 status
```

## 📋 Available Scripts

### Membership Service Scripts

| Script | Purpose | Usage |
|--------|---------|-------|
| `test-e2e.ps1` | Complete E2E test workflow | `.\test-e2e.ps1` |
| `start-membership-service.ps1` | Start service in background | `.\start-membership-service.ps1` |
| `stop-membership-service.ps1` | Stop background service | `.\stop-membership-service.ps1` |
| `run-e2e-tests.ps1` | Run Newman tests only | `.\run-e2e-tests.ps1` |

### Events Service Scripts

| Script | Purpose | Usage |
|--------|---------|-------|
| `test-e2e-events.ps1` | Complete E2E test workflow (Events) | `.\test-e2e-events.ps1` |
| `start-events-service.ps1` | Start Events service in background | `.\start-events-service.ps1` |
| `stop-events-service.ps1` | Stop Events background service | `.\stop-events-service.ps1` |
| `run-e2e-tests-events.ps1` | Run Newman tests for Events | `.\run-e2e-tests-events.ps1` |

### Debugging Scripts (All Services)

| Script | Purpose | Usage |
|--------|---------|-------|
| `debug-service.ps1` | Multi-purpose debugging tool | `.\debug-service.ps1 [action]` |
| `debug-events-service.ps1` | Events service debugging tool | `.\debug-events-service.ps1 [action]` |
| `view-service-logs.ps1` | View Membership service logs | `.\view-service-logs.ps1 [-Tail] [-Follow]` |
| `view-events-service-logs.ps1` | View Events service logs | `.\view-events-service-logs.ps1 [-Tail] [-Follow]` |

## 🏗️ Service Ports

| Service | Port | Health Endpoint |
|---------|------|----------------|
| Membership | 7071 | `http://localhost:7071/api/v1/health` |
| Events | 7072 | `http://localhost:7072/api/v1/health` |
| (Future) Scheduling | 7073 | `http://localhost:7073/api/v1/health` |
| (Future) Bar | 7074 | `http://localhost:7074/api/v1/health` |
| (Future) Notifications | 7075 | `http://localhost:7075/api/v1/health` |

## 🔍 Debugging Guide

### Check Overall Status
```powershell
.\scripts\debug-service.ps1 status
```
Shows:
- PowerShell background jobs
- Func processes
- Port 7071 listener status
- Log file information

### View Service Logs

**Show all logs:**
```powershell
.\scripts\view-service-logs.ps1
```

**Show last 50 lines:**
```powershell
.\scripts\view-service-logs.ps1 -Tail -Lines 50
```

**Follow logs in real-time:**
```powershell
.\scripts\view-service-logs.ps1 -Follow
```

**View specific job logs:**
```powershell
.\scripts\view-service-logs.ps1 -JobId 3
```

### Test Service Health
```powershell
.\scripts\debug-service.ps1 health
```
Calls the `/api/v1/health` endpoint to verify service is responding.

### List Background Jobs
```powershell
.\scripts\debug-service.ps1 jobs
```

### List Func Processes
```powershell
.\scripts\debug-service.ps1 processes
```

## 🛠️ Advanced Usage

### test-e2e.ps1 Options

**Keep service running after tests:**
```powershell
.\scripts\test-e2e.ps1 -KeepServiceRunning
```
Useful for debugging or running manual tests after automated tests.

**Use existing running service:**
```powershell
.\scripts\test-e2e.ps1 -SkipServiceStart
```
Faster test runs when service is already running.

### Manual Job Control

**View job output:**
```powershell
Receive-Job -Id <JobId> -Keep
```

**Stop specific job:**
```powershell
Stop-Job -Id <JobId>
Remove-Job -Id <JobId>
```

**Check job state:**
```powershell
Get-Job -Id <JobId> | Select-Object Id, State, Command
```

## 🐛 Common Debugging Scenarios

### Service won't start

1. **Check for port conflicts:**
   ```powershell
   Get-NetTCPConnection -LocalPort 7071
   ```

2. **View error logs:**
   ```powershell
   .\scripts\view-service-logs.ps1
   ```

3. **Check background job status:**
   ```powershell
   Get-Job | Where-Object { $_.State -eq "Failed" }
   ```

### Tests failing

1. **Verify service is running:**
   ```powershell
   .\scripts\debug-service.ps1 health
   ```

2. **Check recent service logs:**
   ```powershell
   .\scripts\debug-service.ps1 recent
   ```

3. **Run tests with service already running:**
   ```powershell
   .\scripts\start-membership-service.ps1
   .\scripts\run-e2e-tests.ps1
   ```

### Cleanup stuck processes

1. **Stop all background jobs:**
   ```powershell
   .\scripts\stop-membership-service.ps1
   ```

2. **Force kill all func processes:**
   ```powershell
   Get-Process | Where-Object { $_.ProcessName -like "*func*" } | Stop-Process -Force
   ```

3. **Remove all completed jobs:**
   ```powershell
   Get-Job | Remove-Job -Force
   ```

## 📊 Log Analysis with Copilot

When I help debug issues, I can access logs through:

1. **PowerShell job output** via `Receive-Job`
2. **Terminal history** from recent command outputs
3. **Log files** (if enabled in scripts)
4. **Process information** via `Get-Process`
5. **Network status** via `Get-NetTCPConnection`

### Example Debugging Session

```powershell
# Start service
PS> .\scripts\start-membership-service.ps1

# Check status
PS> .\scripts\debug-service.ps1 status

# Follow logs in real-time
PS> .\scripts\debug-service.ps1 follow
# (Ctrl+C to stop)

# In another terminal, run tests
PS> .\scripts\run-e2e-tests.ps1

# If tests fail, check recent logs
PS> .\scripts\debug-service.ps1 recent
```

## 🔧 Troubleshooting

### "Job not found" error
The background job may have stopped. Check:
```powershell
Get-Job -State Failed
```

### Service logs are empty
The job might not have started correctly. Try:
```powershell
.\scripts\debug-service.ps1 status
```

### Port 7071 already in use (Membership Service)
Kill existing processes:
```powershell
.\scripts\stop-membership-service.ps1
```

### Port 7072 already in use (Events Service)
Kill existing processes:
```powershell
.\scripts\stop-events-service.ps1
```

## 🎯 Events Service E2E Testing

### Prerequisites

1. **Newman installed globally:**
   ```powershell
   npm install -g newman
   ```

2. **Database configured:**
   - VillageClubDB exists with Events schema
   - Test users seeded (committee@test.com, member@test.com)

3. **Membership service running** (for authentication):
   ```powershell
   .\scripts\start-membership-service.ps1
   ```

### Running Events Service Tests

**Full automated workflow:**
```powershell
.\scripts\test-e2e-events.ps1
```
This will:
1. Start Events service on port 7072
2. Wait for service to be healthy
3. Run 17 test requests with 34 assertions
4. Stop Events service
5. Display results

**Keep service running for debugging:**
```powershell
.\scripts\test-e2e-events.ps1 -KeepServiceRunning
```

**Use existing running service:**
```powershell
# Start service manually
.\scripts\start-events-service.ps1

# Run tests against running service
.\scripts\test-e2e-events.ps1 -SkipServiceStart

# Clean up when done
.\scripts\stop-events-service.ps1
```

### Events Service Test Coverage

The Events service E2E test suite validates:
- **Health Checks** (2 requests, 5 assertions): Health and readiness endpoints
- **Authentication** (2 requests, 6 assertions): Committee and member token acquisition
- **CRUD Operations** (5 requests, 11 assertions): Create, get, update, delete with authorization tests
- **State Transitions** (4 requests, 7 assertions): Publish, complete, cancel workflows
- **Query Operations** (2 requests, 4 assertions): Pagination and status filtering
- **Cleanup** (2 requests, 2 assertions): Delete test data

**Expected Result:** 34/34 assertions passing (100%)

### Debugging Events Service Issues

**Check Events service status:**
```powershell
.\scripts\debug-events-service.ps1 status
```

**View Events service logs:**
```powershell
.\scripts\view-events-service-logs.ps1
```

**Test Events service health:**
```powershell
Invoke-WebRequest http://localhost:7072/api/v1/health
```

**Check background job:**
```powershell
Get-Job | Where-Object { $_.Command -like "*7072*" }
```

**View recent Events service logs:**
```powershell
Get-Job | Where-Object { $_.Command -like "*7072*" } | Receive-Job -Keep | Select-Object -Last 50
```

### Common Events Service Issues

**Test failing: "Cannot convert enum value"**
- **Cause**: JsonStringEnumConverter not configured
- **Fix**: Ensure EventFunctions._jsonOptions includes `JsonStringEnumConverter(JsonNamingPolicy.CamelCase)`

**Test failing: "500 Internal Server Error on Forbidden"**
- **Cause**: Synchronous WriteString in AuthorizationHelper
- **Fix**: Use `await response.WriteAsJsonAsync(errorResponse)` instead of synchronous methods

**Events service won't connect to database**
- **Cause**: Connection string misconfigured
- **Fix**: Check `local.settings.json` has correct VillageClubDB connection string

**Authentication tests fail**
- **Cause**: Membership service not running
- **Fix**: Start membership service first: `.\scripts\start-membership-service.ps1`

## 📝 Notes

- **Log Persistence**: Logs from background jobs are stored in memory until the job is removed
- **Job Cleanup**: Jobs remain in PowerShell session even after service stops - use `Remove-Job` to clear
- **Terminal Sessions**: Background jobs are tied to the PowerShell session that created them
- **Log Files**: Future enhancement will add persistent file-based logging

## 🎯 Best Practices

1. **Always check status first** when debugging: `.\scripts\debug-service.ps1 status`
2. **Use `-KeepServiceRunning`** when iterating on tests
3. **Clean up jobs** when done: `.\scripts\stop-membership-service.ps1`
4. **Check health endpoint** before running tests: `.\scripts\debug-service.ps1 health`
5. **Follow logs** in real-time during development: `.\scripts\debug-service.ps1 follow`
