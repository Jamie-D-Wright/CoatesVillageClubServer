# Data Model: Village Club Management System

**Feature**: 001-create-a-series  
**Date**: 2025-10-18  
**Status**: Draft

## Overview

This document defines the data entities, relationships, validation rules, and state transitions for the Village Club Management microservices. Each service owns its entities within its database schema.

---

## Schema Organization

```
Database: VillageClubDB (Azure SQL Serverless)

├── Schema: Membership
│   ├── Users
│   ├── UserRoles (junction table)
│   ├── AuditLogs
│   └── RefreshTokens
│
├── Schema: Events
│   ├── Events
│   ├── EventTypes (lookup)
│   └── EventAttendees (future)
│
├── Schema: Scheduling
│   ├── Shifts
│   ├── ShiftAssignments
│   └── ShiftTypes (lookup)
│
├── Schema: Bar
│   ├── StockAlerts
│   ├── StockItems (future)
│   └── Inventory (future)
│
└── Schema: Finance
    ├── Expenses
    ├── Receipts
    └── ExpenseApprovals
```

---

## Entity Definitions

### Membership Service Entities

#### User

**Purpose**: Represents a person who interacts with the village club system

**Table**: `Membership.Users`

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `Id` | UNIQUEIDENTIFIER | PK, DEFAULT NEWID() | Unique identifier |
| `Email` | NVARCHAR(255) | UNIQUE, NOT NULL | Email address (used for login) |
| `PasswordHash` | NVARCHAR(255) | NOT NULL | Bcrypt hash of password |
| `FirstName` | NVARCHAR(100) | NOT NULL | User's first name |
| `LastName` | NVARCHAR(100) | NOT NULL | User's last name |
| `PhoneNumber` | NVARCHAR(20) | NULL | Contact phone number |
| `Address` | NVARCHAR(500) | NULL | Postal address |
| `Role` | NVARCHAR(20) | NOT NULL | Enum: Member, Volunteer, Committee |
| `CommitteeRole` | NVARCHAR(50) | NULL | If Committee: Treasurer, Chairman, Clerk, BarManager, General |
| `Status` | NVARCHAR(20) | NOT NULL, DEFAULT 'Active' | Enum: Active, Inactive, Suspended |
| `CreatedAt` | DATETIME2 | NOT NULL, DEFAULT GETUTCDATE() | Account creation timestamp |
| `UpdatedAt` | DATETIME2 | NOT NULL, DEFAULT GETUTCDATE() | Last update timestamp |
| `LastLoginAt` | DATETIME2 | NULL | Last successful login |

**Indexes**:
- `IX_Users_Email` (UNIQUE)
- `IX_Users_Role`
- `IX_Users_Status`

**Validation Rules**:
- Email must match regex: `^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$`
- Password must be minimum 8 characters, contain uppercase, lowercase, number
- FirstName and LastName: 2-100 characters, letters and spaces only
- PhoneNumber: UK format validation if provided
- CommitteeRole required if Role = Committee
- CommitteeRole must be NULL if Role != Committee

**Business Rules**:
- Cannot delete user with assigned shifts or pending expenses
- Deactivation sets Status = Inactive, preserves data
- Email changes require re-verification (future)

---

#### RefreshToken

**Purpose**: Stores refresh tokens for long-lived sessions

**Table**: `Membership.RefreshTokens`

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `Id` | UNIQUEIDENTIFIER | PK, DEFAULT NEWID() | Unique identifier |
| `UserId` | UNIQUEIDENTIFIER | FK → Users.Id, NOT NULL | User who owns token |
| `Token` | NVARCHAR(255) | UNIQUE, NOT NULL | Refresh token value (hashed) |
| `ExpiresAt` | DATETIME2 | NOT NULL | Token expiration |
| `CreatedAt` | DATETIME2 | NOT NULL, DEFAULT GETUTCDATE() | Token creation |
| `RevokedAt` | DATETIME2 | NULL | Revocation timestamp |
| `ReplacedByToken` | NVARCHAR(255) | NULL | If rotated, new token value |

**Indexes**:
- `IX_RefreshTokens_UserId`
- `IX_RefreshTokens_Token` (UNIQUE)

**Business Rules**:
- Revoked tokens cannot be reused
- Tokens expire after 30 days
- Old tokens auto-purged after 90 days (scheduled job)

---

#### AuditLog

**Purpose**: Tracks user management actions for security audit

**Table**: `Membership.AuditLogs`

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `Id` | BIGINT | PK, IDENTITY(1,1) | Unique identifier |
| `Timestamp` | DATETIME2 | NOT NULL, DEFAULT GETUTCDATE() | Action timestamp |
| `ActorId` | UNIQUEIDENTIFIER | FK → Users.Id, NULL | User who performed action |
| `Action` | NVARCHAR(50) | NOT NULL | UserCreated, RoleChanged, UserDeactivated, etc. |
| `TargetUserId` | UNIQUEIDENTIFIER | FK → Users.Id, NULL | User affected by action |
| `Details` | NVARCHAR(MAX) | NULL | JSON payload with change details |
| `IpAddress` | NVARCHAR(45) | NULL | Source IP address |

**Indexes**:
- `IX_AuditLogs_Timestamp`
- `IX_AuditLogs_ActorId`
- `IX_AuditLogs_TargetUserId`

---

### Events Service Entities

#### Event

**Purpose**: Represents a scheduled activity at the village club

**Table**: `Events.Events`

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `Id` | UNIQUEIDENTIFIER | PK, DEFAULT NEWID() | Unique identifier |
| `Title` | NVARCHAR(200) | NOT NULL | Event title |
| `Description` | NVARCHAR(MAX) | NULL | Event description (Markdown) |
| `EventType` | NVARCHAR(50) | NOT NULL | Enum: SpecialEvent, RegularBarNight, PrivateHire, Fundraiser |
| `StartDateTime` | DATETIME2 | NOT NULL | Event start date and time |
| `EndDateTime` | DATETIME2 | NOT NULL | Event end date and time |
| `Status` | NVARCHAR(20) | NOT NULL, DEFAULT 'Draft' | Enum: Draft, Published, Cancelled, Completed |
| `CreatedById` | UNIQUEIDENTIFIER | NOT NULL | User ID who created event |
| `CreatedAt` | DATETIME2 | NOT NULL, DEFAULT GETUTCDATE() | Creation timestamp |
| `UpdatedAt` | DATETIME2 | NOT NULL, DEFAULT GETUTCDATE() | Last update timestamp |
| `PublishedAt` | DATETIME2 | NULL | When event was published |

**Indexes**:
- `IX_Events_StartDateTime`
- `IX_Events_EventType`
- `IX_Events_Status`

**Validation Rules**:
- Title: 3-200 characters
- StartDateTime must be in the future (when creating)
- EndDateTime must be after StartDateTime
- Duration must be between 30 minutes and 12 hours
- Cannot publish event with EndDateTime in the past

**Business Rules**:
- Published events visible to all users
- Draft events only visible to committee members
- Cannot delete event with assigned shifts (must cancel)
- Cancelled events preserved for historical record

**State Transitions**:
```
Draft → Published → Completed
  ↓         ↓
  └──→ Cancelled
```

---

### Scheduling Service Entities

#### Shift

**Purpose**: Represents a time period requiring volunteer coverage

**Table**: `Scheduling.Shifts`

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `Id` | UNIQUEIDENTIFIER | PK, DEFAULT NEWID() | Unique identifier |
| `EventId` | UNIQUEIDENTIFIER | NULL | FK to Events.Events.Id (if event-specific) |
| `ShiftType` | NVARCHAR(50) | NOT NULL | Enum: BarShift, EventShift |
| `Title` | NVARCHAR(200) | NOT NULL | Shift title (e.g., "Friday Bar Shift", "Quiz Night Setup") |
| `StartDateTime` | DATETIME2 | NOT NULL | Shift start |
| `EndDateTime` | DATETIME2 | NOT NULL | Shift end |
| `RequiredVolunteers` | INT | NOT NULL, DEFAULT 1 | Max volunteers for this shift |
| `Status` | NVARCHAR(20) | NOT NULL, DEFAULT 'Open' | Enum: Open, Filled, Cancelled |
| `CreatedById` | UNIQUEIDENTIFIER | NOT NULL | User ID who created shift |
| `CreatedAt` | DATETIME2 | NOT NULL, DEFAULT GETUTCDATE() | Creation timestamp |

**Indexes**:
- `IX_Shifts_EventId`
- `IX_Shifts_StartDateTime`
- `IX_Shifts_Status`

**Validation Rules**:
- StartDateTime must be in the future
- EndDateTime must be after StartDateTime
- Duration must be between 1 hour and 8 hours
- RequiredVolunteers: 1-10
- If EventId provided, shift times must fall within event times

**Business Rules**:
- Regular bar shifts have NULL EventId
- Event shifts link to specific event
- Status automatically updates to Filled when RequiredVolunteers = assigned count
- Cannot delete shift with assignments (must cancel)

---

#### ShiftAssignment

**Purpose**: Junction table linking volunteers to shifts

**Table**: `Scheduling.ShiftAssignments`

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `Id` | UNIQUEIDENTIFIER | PK, DEFAULT NEWID() | Unique identifier |
| `ShiftId` | UNIQUEIDENTIFIER | FK → Shifts.Id, NOT NULL | Shift being assigned |
| `VolunteerId` | UNIQUEIDENTIFIER | NOT NULL | User ID of volunteer (validated as Volunteer/Committee role) |
| `AssignedAt` | DATETIME2 | NOT NULL, DEFAULT GETUTCDATE() | When volunteer signed up |
| `Status` | NVARCHAR(20) | NOT NULL, DEFAULT 'Confirmed' | Enum: Confirmed, Cancelled |
| `CancelledAt` | DATETIME2 | NULL | Cancellation timestamp |

**Indexes**:
- `IX_ShiftAssignments_ShiftId`
- `IX_ShiftAssignments_VolunteerId`
- `UQ_ShiftAssignments_ShiftId_VolunteerId` (UNIQUE constraint on combination)

**Validation Rules**:
- VolunteerId must have Role = Volunteer or Committee
- Cannot assign if shift is in the past
- Cannot assign if shift at capacity

**Business Rules**:
- Volunteer cannot be assigned to overlapping shifts
- Cancellation preserves record (sets Status = Cancelled)
- Deleting assignment recalculates Shift.Status if no longer filled

---

### Bar Service Entities

#### StockAlert

**Purpose**: Notification that bar stock is running low

**Table**: `Bar.StockAlerts`

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `Id` | UNIQUEIDENTIFIER | PK, DEFAULT NEWID() | Unique identifier |
| `ItemName` | NVARCHAR(200) | NOT NULL | Name of stock item |
| `Urgency` | NVARCHAR(20) | NOT NULL | Enum: Low, Medium, High |
| `Notes` | NVARCHAR(MAX) | NULL | Additional details |
| `ReportedById` | UNIQUEIDENTIFIER | NOT NULL | User ID who reported (Volunteer/Committee) |
| `ReportedAt` | DATETIME2 | NOT NULL, DEFAULT GETUTCDATE() | Alert creation timestamp |
| `Status` | NVARCHAR(20) | NOT NULL, DEFAULT 'Active' | Enum: Active, Resolved |
| `ResolvedById` | UNIQUEIDENTIFIER | NULL | User ID who resolved (Committee) |
| `ResolvedAt` | DATETIME2 | NULL | Resolution timestamp |

**Indexes**:
- `IX_StockAlerts_ItemName`
- `IX_StockAlerts_Status`
- `IX_StockAlerts_ReportedAt`

**Validation Rules**:
- ItemName: 2-200 characters
- ReportedById must have Role = Volunteer or Committee
- ResolvedById must have Role = Committee
- Cannot resolve without ResolvedById

**Business Rules**:
- Multiple active alerts for same item allowed (indicates recurring issue)
- Auto-group alerts by ItemName in UI
- Committee members can add notes when resolving

---

### Finance Service Entities

#### Expense

**Purpose**: Represents a reimbursement claim for club-related purchases

**Table**: `Finance.Expenses`

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `Id` | UNIQUEIDENTIFIER | PK, DEFAULT NEWID() | Unique identifier |
| `EventId` | UNIQUEIDENTIFIER | NOT NULL | FK to Events.Events.Id (required) |
| `SubmittedById` | UNIQUEIDENTIFIER | NOT NULL | User ID of submitter (Volunteer/Committee) |
| `Amount` | DECIMAL(10, 2) | NOT NULL, CHECK > 0 | Expense amount in GBP |
| `Description` | NVARCHAR(500) | NOT NULL | What was purchased |
| `PurchaseDate` | DATE | NOT NULL | Date of purchase |
| `Status` | NVARCHAR(20) | NOT NULL, DEFAULT 'PendingReview' | See state diagram below |
| `ReceiptBlobUri` | NVARCHAR(500) | NOT NULL | Blob storage path to receipt image |
| `SubmittedAt` | DATETIME2 | NOT NULL, DEFAULT GETUTCDATE() | Submission timestamp |
| `ReviewedById` | UNIQUEIDENTIFIER | NULL | User ID of reviewer (Committee) |
| `ReviewedAt` | DATETIME2 | NULL | Review timestamp |
| `RejectionReason` | NVARCHAR(MAX) | NULL | Why expense was rejected |
| `PaidAt` | DATETIME2 | NULL | Reimbursement payment date |

**Indexes**:
- `IX_Expenses_EventId`
- `IX_Expenses_SubmittedById`
- `IX_Expenses_Status`
- `IX_Expenses_PurchaseDate`

**Validation Rules**:
- Amount: £0.01 - £500.00 (upper limit to prevent fraud)
- Description: 10-500 characters
- PurchaseDate must be within 90 days of submission
- PurchaseDate must be on or before submission date
- ReceiptBlobUri must point to valid blob (checked during upload)
- RejectionReason required if Status = Rejected

**Business Rules**:
- EventId must reference existing published event
- SubmittedById must have Role = Volunteer or Committee
- ReviewedById must have Role = Committee or CommitteeRole = Treasurer
- Cannot modify expense after submission (create new if mistake)
- Receipt blob soft-deleted when expense deleted (30-day retention)

**State Transitions**:
```
PendingReview → Approved → Reimbursed
       ↓
     Rejected (can resubmit as new expense with corrections)
```

---

#### Receipt

**Purpose**: Metadata about receipt image stored in Blob Storage

**Table**: `Finance.Receipts`

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `Id` | UNIQUEIDENTIFIER | PK, DEFAULT NEWID() | Unique identifier |
| `ExpenseId` | UNIQUEIDENTIFIER | FK → Expenses.Id, NOT NULL | Associated expense |
| `BlobName` | NVARCHAR(500) | NOT NULL | Blob storage path |
| `FileName` | NVARCHAR(255) | NOT NULL | Original upload filename |
| `ContentType` | NVARCHAR(100) | NOT NULL | MIME type (image/jpeg, image/png, application/pdf) |
| `FileSizeBytes` | BIGINT | NOT NULL | File size for validation |
| `UploadedAt` | DATETIME2 | NOT NULL, DEFAULT GETUTCDATE() | Upload timestamp |
| `SasTokenExpiresAt` | DATETIME2 | NULL | Temporary access token expiry |

**Indexes**:
- `IX_Receipts_ExpenseId`
- `IX_Receipts_BlobName` (UNIQUE)

**Validation Rules**:
- ContentType: image/jpeg, image/png, application/pdf only
- FileSizeBytes: 200KB - 5MB (per spec FR-037)
- BlobName must match format: `{year}/{month}/{expense-id}/{filename}`

---

#### ExpenseApproval (Future: Audit Trail)

**Purpose**: Tracks approval/rejection actions for audit

**Table**: `Finance.ExpenseApprovals`

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `Id` | UNIQUEIDENTIFIER | PK, DEFAULT NEWID() | Unique identifier |
| `ExpenseId` | UNIQUEIDENTIFIER | FK → Expenses.Id, NOT NULL | Expense being acted on |
| `ActorId` | UNIQUEIDENTIFIER | NOT NULL | User ID who took action |
| `Action` | NVARCHAR(20) | NOT NULL | Enum: Approved, Rejected, Reimbursed |
| `Timestamp` | DATETIME2 | NOT NULL, DEFAULT GETUTCDATE() | Action timestamp |
| `Notes` | NVARCHAR(MAX) | NULL | Optional notes from reviewer |

---

## Cross-Service Relationships

**NOTE**: These relationships are logical (enforced via API calls), NOT database foreign keys across schemas.

| From Service | Entity | To Service | Entity | Relationship |
|--------------|--------|------------|--------|--------------|
| Events | Event.CreatedById | Membership | User.Id | Created by user |
| Scheduling | Shift.EventId | Events | Event.Id | Shift for event (optional) |
| Scheduling | ShiftAssignment.VolunteerId | Membership | User.Id | Volunteer assignment |
| Bar | StockAlert.ReportedById | Membership | User.Id | Alert reporter |
| Bar | StockAlert.ResolvedById | Membership | User.Id | Alert resolver |
| Finance | Expense.EventId | Events | Event.Id | Expense for event |
| Finance | Expense.SubmittedById | Membership | User.Id | Expense submitter |
| Finance | Expense.ReviewedById | Membership | User.Id | Expense reviewer |

**Validation Strategy**:
- Before creating Shift, validate EventId exists via Events service API
- Before creating Expense, validate EventId exists and is published via Events service API
- Before assigning Shift, validate VolunteerId has Volunteer/Committee role via Membership service API

---

## Enumerations

### UserRole
```csharp
public enum UserRole
{
    Member,     // Can view events, use bar
    Volunteer,  // Can sign up for shifts, report stock
    Committee   // Full admin access
}
```

### CommitteeRole
```csharp
public enum CommitteeRole
{
    Treasurer,
    Chairman,
    Clerk,
    BarManager,
    General
}
```

### UserStatus
```csharp
public enum UserStatus
{
    Active,
    Inactive,
    Suspended
}
```

### EventType
```csharp
public enum EventType
{
    SpecialEvent,   // Quiz night, live music
    RegularBarNight, // Standard Fri/Sat opening
    PrivateHire,    // Birthday party, etc.
    Fundraiser      // Charity event
}
```

### EventStatus
```csharp
public enum EventStatus
{
    Draft,
    Published,
    Cancelled,
    Completed
}
```

### ShiftType
```csharp
public enum ShiftType
{
    BarShift,   // Regular bar opening hours
    EventShift  // Event-specific staffing
}
```

### ShiftStatus
```csharp
public enum ShiftStatus
{
    Open,
    Filled,
    Cancelled
}
```

### AssignmentStatus
```csharp
public enum AssignmentStatus
{
    Confirmed,
    Cancelled
}
```

### StockUrgency
```csharp
public enum StockUrgency
{
    Low,
    Medium,
    High
}
```

### StockAlertStatus
```csharp
public enum StockAlertStatus
{
    Active,
    Resolved
}
```

### ExpenseStatus
```csharp
public enum ExpenseStatus
{
    PendingReview,
    Approved,
    Rejected,
    Reimbursed
}
```

---

## Aggregate Total Queries (Cross-Schema)

These queries will be implemented in service layer, not as database views (respects service boundaries):

### Total Expenses Per Event
```csharp
// Finance service calculates, Events service displays
public async Task<decimal> GetTotalExpensesForEvent(Guid eventId)
{
    return await _context.Expenses
        .Where(e => e.EventId == eventId && e.Status != ExpenseStatus.Rejected)
        .SumAsync(e => e.Amount);
}
```

### Shift Fill Rate
```csharp
// Scheduling service calculates
public async Task<double> GetShiftFillRate(DateTime startDate, DateTime endDate)
{
    var shifts = await _context.Shifts
        .Where(s => s.StartDateTime >= startDate && s.StartDateTime <= endDate)
        .Include(s => s.Assignments)
        .ToListAsync();
    
    var totalSlots = shifts.Sum(s => s.RequiredVolunteers);
    var filledSlots = shifts.Sum(s => s.Assignments.Count(a => a.Status == AssignmentStatus.Confirmed));
    
    return (double)filledSlots / totalSlots * 100;
}
```

---

## Data Retention Policy

| Entity | Retention Period | Rationale |
|--------|------------------|-----------|
| Users | Indefinite (soft delete) | Preserve audit trail, GDPR-compliant export available |
| Events | 7 years | Club records requirement |
| Shifts | 2 years | Operational history, less critical than events |
| StockAlerts | 1 year | Operational data, low business value after resolved |
| Expenses | 7 years | Financial records legal requirement (UK) |
| Receipts (Blob) | 7 years | Proof of purchase for expenses |
| AuditLogs | 3 years | Security audit trail |
| RefreshTokens | 90 days | Auto-purge expired/revoked tokens |

**Implementation**: Scheduled Azure Function (monthly) to archive/delete old records.

---

## Migration Strategy

### Initial Schema Creation
```
1. Create schemas: Membership, Events, Scheduling, Bar, Finance
2. Apply EF Core migrations per service (separate migration history)
3. Seed lookup data (EventTypes, ShiftTypes, etc.)
4. Create admin user (via secure script)
```

### Future Changes
- Each service maintains its own migration history table
- Migrations applied via CI/CD pipeline before deployment
- Rollback plan: reverse migrations + data backup

---

**Data Model Version**: 1.0.0  
**Status**: Ready for Phase 1 contract generation  
**Next**: Generate OpenAPI contracts in `contracts/openapi/`
