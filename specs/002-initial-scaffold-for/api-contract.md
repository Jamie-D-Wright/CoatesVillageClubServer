# API Contract: Village Club Service Scaffold

## Overview
This document defines the HTTP-triggered Azure Functions endpoints for the Village Club Service scaffold.
All endpoints return application/json unless specified otherwise.

## Base URL
- Local: `http://localhost:7071/api`
- Azure: `https://{function-app-name}.azurewebsites.net/api`

## Common Headers
- `Content-Type: application/json`
- `Accept: application/json`

## Endpoints

### Health Check
GET `/health`

**Purpose**: Provides service health information for monitoring

**Response**:
```json
{
  "status": "string",
  "checks": [
    {
      "name": "string",
      "status": "string",
      "description": "string",
      "lastChecked": "string (ISO 8601)",
      "data": {
        "uptime": "string",
        "memoryUsage": "number",
        "threadCount": "number"
      }
    }
  ]
}
```

**Status Codes**:
- 200: Service is healthy
- 503: Service is unhealthy/degraded

### Service Information
GET `/info`

**Purpose**: Returns basic service information and configuration

**Response**:
```json
{
  "applicationName": "string",
  "version": "string",
  "environment": "string",
  "startTime": "string (ISO 8601)",
  "framework": "string"
}
```

**Status Codes**:
- 200: Success

## Error Responses
All endpoints may return these error responses:

```json
{
  "type": "string",
  "title": "string",
  "status": number,
  "detail": "string",
  "instance": "string"
}
```

**Common Status Codes**:
- 400: Bad Request
- 404: Not Found
- 500: Internal Server Error