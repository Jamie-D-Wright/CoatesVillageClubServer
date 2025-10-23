# API Contracts Summary

**Feature**: 001-create-a-series  
**Date**: 2025-10-18

## Overview

This directory contains OpenAPI 3.0 specifications for all Village Club microservices. Full specifications to be generated during implementation.

---

## Services and Key Endpoints

### 1. Membership Service
**File**: `membership-api.yaml` (✅ Complete)

**Base Path**: `/api/v1`

**Key Endpoints**:
- `POST /auth/login` - Authenticate user, issue JWT
- `POST /auth/refresh` - Refresh access token
- `POST /auth/logout` - Revoke refresh token
- `GET /users` - List users (Committee only)
- `POST /users` - Create user (Committee only)
- `GET /users/{id}` - Get user details
- `PUT /users/{id}` - Update user
- `DELETE /users/{id}` - Deactivate user
- `GET /users/me` - Get current user profile
- `GET /health` - Service health check

---

### 2. Events Service
**File**: `events-api.yaml` (To be generated)

**Base Path**: `/api/v1`

**Key Endpoints**:
- `GET /events` - List events (future/past filtering, pagination)
- `POST /events` - Create event (Committee only)
- `GET /events/{id}` - Get event details
- `PUT /events/{id}` - Update event (Committee only)
- `DELETE /events/{id}` - Delete/cancel event (Committee only)
- `GET /events/{id}/expenses` - Get total expenses for event
- `GET /health` - Service health check

**Key Models**:
- `Event`: id, title, description, eventType, startDateTime, endDateTime, status, createdById
- `EventType`: SpecialEvent, RegularBarNight, PrivateHire, Fundraiser
- `EventStatus`: Draft, Published, Cancelled, Completed

---

### 3. Scheduling Service
**File**: `scheduling-api.yaml` (To be generated)

**Base Path**: `/api/v1`

**Key Endpoints**:
- `GET /shifts` - List shifts (filter by date range, status)
- `POST /shifts` - Create shift (Committee only)
- `GET /shifts/{id}` - Get shift details with assignments
- `PUT /shifts/{id}` - Update shift (Committee only)
- `DELETE /shifts/{id}` - Cancel shift (Committee only)
- `POST /shifts/{id}/assignments` - Assign volunteer to shift (Volunteer/Committee)
- `DELETE /shifts/{id}/assignments/{assignmentId}` - Cancel shift assignment
- `GET /shifts/my-assignments` - Get current user's shift assignments
- `GET /health` - Service health check

**Key Models**:
- `Shift`: id, eventId (nullable), shiftType, title, startDateTime, endDateTime, requiredVolunteers, status
- `ShiftAssignment`: id, shiftId, volunteerId, assignedAt, status
- `ShiftType`: BarShift, EventShift
- `ShiftStatus`: Open, Filled, Cancelled

**Business Logic**:
- Validate no overlapping shift assignments for same volunteer
- Validate EventId exists if provided (call Events service)
- Auto-update Shift.Status to Filled when capacity reached

---

### 4. Bar Service
**File**: `bar-api.yaml` (To be generated)

**Base Path**: `/api/v1`

**Key Endpoints**:
- `GET /stock-alerts` - List active stock alerts (Committee/Volunteer)
- `POST /stock-alerts` - Create stock alert (Volunteer/Committee)
- `GET /stock-alerts/{id}` - Get alert details
- `PUT /stock-alerts/{id}/resolve` - Mark alert as resolved (Committee only)
- `GET /health` - Service health check

**Key Models**:
- `StockAlert`: id, itemName, urgency, notes, reportedById, reportedAt, status, resolvedById, resolvedAt
- `StockUrgency`: Low, Medium, High
- `StockAlertStatus`: Active, Resolved

---

### 5. Finance Service
**File**: `finance-api.yaml` (To be generated)

**Base Path**: `/api/v1`

**Key Endpoints**:
- `GET /expenses` - List expenses (filter by status, eventId, dateRange)
- `POST /expenses` - Submit expense claim with receipt upload (Volunteer/Committee)
- `GET /expenses/{id}` - Get expense details
- `PUT /expenses/{id}/approve` - Approve expense (Committee/Treasurer only)
- `PUT /expenses/{id}/reject` - Reject expense with reason (Committee/Treasurer only)
- `PUT /expenses/{id}/reimburse` - Mark as reimbursed (Treasurer only)
- `GET /expenses/{id}/receipt` - Get receipt download URL (SAS token)
- `GET /expenses/summary` - Get expense summary by event/date
- `GET /health` - Service health check

**Key Models**:
- `Expense`: id, eventId, submittedById, amount, description, purchaseDate, status, receiptBlobUri, submittedAt, reviewedById, reviewedAt, rejectionReason, paidAt
- `Receipt`: id, expenseId, fileName, contentType, fileSizeBytes, uploadedAt
- `ExpenseStatus`: PendingReview, Approved, Rejected, Reimbursed

**Business Logic**:
- Validate EventId exists and is published (call Events service)
- Validate receipt upload: 200KB-5MB, JPEG/PNG/PDF only
- Generate SAS token for receipt access (1-hour expiry)
- Calculate total expenses per event

---

## Shared Patterns

### Authentication
All services (except `GET /health`) require JWT Bearer token in `Authorization` header:
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Pagination
List endpoints support standard pagination query parameters:
```
?page=1&pageSize=50
```

Response format:
```json
{
  "items": [...],
  "totalCount": 123,
  "page": 1,
  "pageSize": 50,
  "totalPages": 3
}
```

### Error Responses
Standard error format across all services:
```json
{
  "message": "Error description",
  "code": "ERROR_CODE",
  "timestamp": "2025-10-18T20:00:00Z"
}
```

Common HTTP status codes:
- `400` - Validation error
- `401` - Authentication required/failed
- `403` - Insufficient permissions
- `404` - Resource not found
- `409` - Conflict (e.g., duplicate email, overlapping shifts)
- `500` - Internal server error

### Correlation IDs
All requests include `X-Correlation-ID` header for distributed tracing:
```
X-Correlation-ID: 3fa85f64-5717-4562-b3fc-2c963f66afa6
```

---

## Inter-Service Communication

Services call each other via API Gateway using internal routing:

**Example: Submitting Expense (Finance → Events validation)**
```
1. UI → APIM → Finance: POST /api/v1/expenses
2. Finance → APIM → Events: GET /api/v1/events/{eventId}
3. Events validates and returns event details
4. Finance saves expense and returns to UI
```

**Circuit Breaker Policy** (via Polly):
- 3 retries with exponential backoff
- Circuit opens after 5 consecutive failures
- Half-open after 30 seconds
- 5-second timeout per request

---

## API Versioning Strategy

- **URL-based versioning**: `/api/v1/`, `/api/v2/`
- **Breaking changes**: Increment major version, maintain old version for 6 months
- **Non-breaking changes**: Add to current version (new optional fields, new endpoints)
- **Deprecation**: Mark deprecated in OpenAPI, log usage, remove after deprecation period

---

## Contract Testing

Each service MUST provide contract tests ensuring:
1. Response schemas match OpenAPI spec
2. Required fields are present
3. Enum values are valid
4. HTTP status codes match spec

Contract test location: `services/{service}/tests/VillageClub.{Service}.ContractTests/`

---

## Next Steps

1. ✅ Membership API spec complete
2. ⏭️ Generate remaining OpenAPI specs during implementation
3. ⏭️ Implement Swashbuckle in each service to auto-generate from code
4. ⏭️ Validate generated specs match these contracts
5. ⏭️ Publish to APIM Developer Portal

---

**Version**: 1.0.0  
**Status**: Summary complete, full specs to be generated during implementation
