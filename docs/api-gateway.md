# API Gateway Documentation

**Last Updated**: 2025-10-19  
**Gateway Type**: Azure API Management (Consumption Tier)  
**Authentication**: JWT Bearer Tokens (via VillageClub.Auth library)

## Overview

The Village Club API Gateway provides a unified entry point for all microservices, handling cross-cutting concerns like authentication, CORS, service discovery, and routing.

## Gateway URL

**Development**: `http://localhost:7071` (Azure Functions local runtime)  
**Staging**: `https://apim-villageclub-staging.azure-api.net`  
**Production**: `https://apim-villageclub-prod.azure-api.net`

## Service Discovery

### List All Services

Retrieve metadata for all registered services including health status and OpenAPI documentation URLs.

**Endpoint**: `GET /registry/v1/services`  
**Authentication**: None (public endpoint)

**Response Example**:
```json
{
  "services": [
    {
      "name": "Membership",
      "version": "v1",
      "basePath": "/membership/api/v1",
      "healthEndpoint": "/membership/api/v1/health",
      "openapiUrl": "/membership/swagger.json",
      "description": "User management, authentication, and authorization"
    }
  ],
  "timestamp": "2025-10-19T10:30:00.000Z"
}
```

## Authentication Flow

The API Gateway uses JWT (JSON Web Token) authentication provided by the **VillageClub.Auth** library. All protected endpoints require a valid JWT token in the `Authorization` header.

### 1. User Login

**Endpoint**: `POST /membership/api/v1/auth/login`  
**Authentication**: None (public endpoint)

**Request**:
```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}
```

**Response**:
```json
{
  "accessToken": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "refresh_token_here",
  "expiresIn": 86400,
  "tokenType": "Bearer",
  "user": {
    "id": "123e4567-e89b-12d3-a456-426614174000",
    "email": "user@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "role": "Member"
  }
}
```

### 2. Making Authenticated Requests

Include the access token in the `Authorization` header with the `Bearer` scheme:

```http
GET /membership/api/v1/users/me HTTP/1.1
Host: apim-villageclub-prod.azure-api.net
Authorization: Bearer eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...
```

### 3. Token Refresh

When the access token expires (24 hours), use the refresh token to obtain a new access token:

**Endpoint**: `POST /membership/api/v1/auth/refresh`  
**Authentication**: None (uses refresh token)

**Request**:
```json
{
  "refreshToken": "refresh_token_here"
}
```

**Response**: Same as login response with new tokens

### 4. Logout

Revoke the refresh token to invalidate all future refresh attempts:

**Endpoint**: `POST /membership/api/v1/auth/logout`  
**Authentication**: Bearer token required

**Request**:
```json
{
  "refreshToken": "refresh_token_here"
}
```

## JWT Token Structure

Tokens are generated and validated by the **VillageClub.Auth** library using RSA 2048-bit keys.

### Token Claims

- **`sub`** (Subject): User ID (GUID)
- **`email`**: User's email address
- **`role`**: User role (Committee, Volunteer, Member)
- **`name`**: User's full name
- **`iss`** (Issuer): `village-club-membership`
- **`aud`** (Audience): `village-club-api`
- **`exp`** (Expiration): Unix timestamp (24 hours from issuance)
- **`iat`** (Issued At): Unix timestamp

### Example Decoded Token

```json
{
  "sub": "123e4567-e89b-12d3-a456-426614174000",
  "email": "john.doe@example.com",
  "role": "Committee",
  "name": "John Doe",
  "iss": "village-club-membership",
  "aud": "village-club-api",
  "exp": 1697803200,
  "iat": 1697716800
}
```

## JWT Validation Policy

The API Gateway validates JWT tokens using the public key exposed by the Membership service's health endpoint.

### Public vs Protected Endpoints

**Public Endpoints** (no token required):
- `/registry/v1/services` - Service discovery
- `/membership/api/v1/health` - Health check
- `/membership/api/v1/auth/login` - User login
- `/membership/api/v1/auth/register` - User registration

**Protected Endpoints** (token required):
- All other endpoints require a valid JWT token

### Validation Process

1. **Extract Token**: Gateway extracts JWT from `Authorization: Bearer <token>` header
2. **Fetch Public Key**: Gateway retrieves RSA public key from `/membership/api/v1/health`
3. **Verify Signature**: Token signature is verified using the public key
4. **Validate Claims**: Issuer, audience, and expiration are validated
5. **Forward Request**: If valid, request is forwarded to backend service
6. **Return 401**: If invalid, gateway returns `401 Unauthorized`

### Public Key Retrieval

The Membership service exposes its RSA public key in the health check response:

**Endpoint**: `GET /membership/api/v1/health`

**Response**:
```json
{
  "status": "Healthy",
  "service": "Membership",
  "version": "1.0.0",
  "timestamp": "2025-10-19T10:30:00.000Z",
  "publicKey": "-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA...\n-----END PUBLIC KEY-----"
}
```

## CORS Configuration

The API Gateway is configured to allow cross-origin requests from the UI application.

### Allowed Origins

- `http://localhost:3000` (React development server)
- `http://localhost:5173` (Vite development server)

### Allowed Methods

- `GET`, `POST`, `PUT`, `DELETE`, `OPTIONS`

### Allowed Headers

- All headers (`*`)

### Credentials

- Credentials are allowed (`Access-Control-Allow-Credentials: true`)

## Error Responses

All errors from the API Gateway follow a consistent format:

### Authentication Errors

**Status**: `401 Unauthorized`

```json
{
  "message": "Unauthorized. Valid JWT token required.",
  "code": "UNAUTHORIZED",
  "timestamp": "2025-10-19T10:30:00.000Z"
}
```

### Gateway Errors

**Status**: `500 Internal Server Error`

```json
{
  "message": "An unexpected error occurred",
  "code": "GATEWAY_ERROR",
  "timestamp": "2025-10-19T10:30:00.000Z"
}
```

### Service Unavailable

**Status**: `503 Service Unavailable`

```json
{
  "message": "Service temporarily unavailable",
  "code": "SERVICE_UNAVAILABLE",
  "timestamp": "2025-10-19T10:30:00.000Z"
}
```

## Backend Services

### Membership Service

**Base Path**: `/membership/api/v1`  
**Backend URL**: Configured via `membershipServiceUrl` parameter  
**Health Check**: `GET /membership/api/v1/health`

**Endpoints**:
- Authentication: `/auth/login`, `/auth/register`, `/auth/refresh`, `/auth/logout`
- User Management: `/users`, `/users/{id}`, `/users/me`
- Health: `/health`

## Rate Limiting

**Not yet implemented** - Planned for Phase 9 (T138)

Future configuration:
- **Limit**: 100 requests per minute per user
- **Response**: `429 Too Many Requests`

## Monitoring & Health

### Gateway Health

The API Gateway does not have a dedicated health endpoint. Monitor service health via:

1. **Service Registry**: Check `/registry/v1/services` for service metadata
2. **Service Health**: Each service exposes `/api/v1/health` endpoint
3. **Azure Monitor**: Application Insights tracks gateway metrics

### Key Metrics

- Request rate
- Response time (P50, P95, P99)
- Error rate (4xx, 5xx)
- Token validation failures
- Backend service availability

## Infrastructure as Code

The API Gateway is defined in Bicep:

**File**: `infrastructure/modules/apim.bicep`

**Key Resources**:
- `apimService` - Azure API Management instance
- `membershipBackend` - Membership service backend
- `membershipApi` - Membership API definition
- `serviceDiscoveryApi` - Service registry API
- `globalPolicy` - CORS, JWT validation, error handling

**Deployment**:
```bash
az deployment group create \
  --resource-group rg-villageclub-prod \
  --template-file infrastructure/main.bicep \
  --parameters publisherEmail=admin@villageclub.com \
               publisherName="Village Club" \
               membershipServiceUrl="https://func-membership-prod.azurewebsites.net"
```

## Developer Quick Start

### 1. Discover Services

```bash
curl https://apim-villageclub-prod.azure-api.net/registry/v1/services
```

### 2. Login

```bash
curl -X POST https://apim-villageclub-prod.azure-api.net/membership/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "SecurePassword123!"
  }'
```

### 3. Call Protected Endpoint

```bash
curl https://apim-villageclub-prod.azure-api.net/membership/api/v1/users/me \
  -H "Authorization: Bearer <your_access_token>"
```

### 4. Refresh Token

```bash
curl -X POST https://apim-villageclub-prod.azure-api.net/membership/api/v1/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{
    "refreshToken": "<your_refresh_token>"
  }'
```

## Security Best Practices

### For API Consumers

1. **Store tokens securely**: Never store tokens in localStorage (use httpOnly cookies or secure storage)
2. **Use HTTPS only**: Always use HTTPS in production
3. **Handle token expiration**: Implement automatic token refresh before expiration
4. **Logout on close**: Revoke tokens when user logs out or closes the app
5. **Validate responses**: Check response status and handle errors gracefully

### For API Gateway Configuration

1. **Rotate keys regularly**: RSA keys should be rotated every 90 days
2. **Monitor failed validations**: Alert on high JWT validation failure rates
3. **Use managed identities**: Backend connections use Azure Managed Identities
4. **Enable audit logging**: All gateway actions are logged to Application Insights
5. **Implement rate limiting**: Prevent abuse (planned for Phase 9)

## Troubleshooting

### 401 Unauthorized

**Symptoms**: Requests return `401 Unauthorized`

**Causes**:
1. Missing or malformed `Authorization` header
2. Expired access token
3. Invalid token signature (key rotation without client update)
4. Token issued by wrong issuer

**Solutions**:
1. Check `Authorization` header format: `Bearer <token>`
2. Refresh token using `/auth/refresh` endpoint
3. Re-login to obtain new token
4. Verify token claims match expected issuer and audience

### 503 Service Unavailable

**Symptoms**: Requests return `503 Service Unavailable`

**Causes**:
1. Backend service is down or not responding
2. Cold start delay (Azure Functions consumption plan)
3. Database connection issues

**Solutions**:
1. Check service health via `/health` endpoint
2. Wait 5-10 seconds for cold start (first request after idle)
3. Check Azure Function logs in Application Insights
4. Verify database connectivity and credentials

### CORS Errors

**Symptoms**: Browser shows CORS error in console

**Causes**:
1. Request origin not in allowed origins list
2. Credentials sent without `allow-credentials: true`
3. Preflight OPTIONS request failing

**Solutions**:
1. Add origin to CORS policy in `apim.bicep`
2. Ensure `Access-Control-Allow-Credentials: true` is set
3. Check OPTIONS request returns `200 OK`

## Related Documentation

- [Membership Service API](../specs/001-create-a-series/contracts/openapi/membership-api.yaml)
- [VillageClub.Auth Library](../libs/VillageClub.Auth/README.md)
- [Deployment Guide](./deployment.md)
- [Infrastructure Documentation](../infrastructure/README.md)

## Support

For issues or questions:
- **GitHub Issues**: https://github.com/Jamie-D-Wright/CoatesVillageClubServer/issues
- **Email**: support@villageclub.com
