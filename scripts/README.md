# Service Management & Debugging Scripts

This directory contains scripts for managing and debugging the membership service during local development.

## 🚀 Quick Start

**Run full E2E test suite:**
```powershell
.\scripts\test-e2e.ps1
```

**Check service status:**
```powershell
.\scripts\debug-service.ps1 status
```

## 📋 Available Scripts

### Core Scripts

| Script | Purpose | Usage |
|--------|---------|-------|
| `test-e2e.ps1` | Complete E2E test workflow | `.\test-e2e.ps1` |
| `start-membership-service.ps1` | Start service in background | `.\start-membership-service.ps1` |
| `stop-membership-service.ps1` | Stop background service | `.\stop-membership-service.ps1` |
| `run-e2e-tests.ps1` | Run Newman tests only | `.\run-e2e-tests.ps1` |

### Debugging Scripts

| Script | Purpose | Usage |
|--------|---------|-------|
| `debug-service.ps1` | Multi-purpose debugging tool | `.\debug-service.ps1 [action]` |
| `view-service-logs.ps1` | View service logs | `.\view-service-logs.ps1 [-Tail] [-Follow]` |

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

### Port 7071 already in use
Kill existing processes:
```powershell
.\scripts\stop-membership-service.ps1
```

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
