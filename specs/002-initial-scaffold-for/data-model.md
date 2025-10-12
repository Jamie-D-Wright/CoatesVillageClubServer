# Data Model: Village Club Service Scaffold

## Configuration Model

### ServiceSettings
Represents the core service configuration.

**Properties**:
- `Port` (int): Service listening port
- `LogLevel` (string): Minimum log level to record
- `ApplicationName` (string): Service identifier
- `EnvironmentName` (string): Running environment identifier

**Validation**:
- Port must be between 1 and 65535
- LogLevel must be one of: Debug, Information, Warning, Error, Critical
- ApplicationName must not be empty
- EnvironmentName must not be empty

## Health Model

### HealthStatus
Represents the service health information.

**Properties**:
- `Status` (string): Overall health status
- `Uptime` (TimeSpan): Time since service start
- `LastCheckTime` (DateTime): Timestamp of last health check
- `MemoryUsage` (long): Current memory usage in bytes
- `ThreadCount` (int): Current number of active threads

**States**:
- Healthy: All systems operational
- Degraded: Service running but with issues
- Unhealthy: Service not functioning properly

**Transitions**:
- Healthy → Degraded: Resource thresholds exceeded
- Degraded → Healthy: Resources normalized
- Any → Unhealthy: Critical failure
- Unhealthy → Healthy: After successful restart