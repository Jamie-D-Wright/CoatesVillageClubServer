# Storage Architecture

## Overview

The CoatesVillageClubServer uses a **service-oriented storage architecture** where each microservice has its own dedicated Azure Storage Account. This approach provides:

- **Service isolation**: Each service manages its own storage independently
- **Security boundaries**: Separate access policies per service
- **Scalability**: Services can scale storage independently
- **Cost tracking**: Clear cost attribution per service
- **Compliance**: Service-specific retention policies

## Storage Accounts

### 1. Function Runtime Storage (`stfuncdev`)
**Purpose**: Azure Functions runtime requirements  
**Type**: System storage (not application data)  
**Containers**: Managed by Azure Functions runtime

### 2. Membership Service Storage (`stmbrdev`)
**Service**: User Management  
**Current Use**: Future-ready for member documents  
**Containers**:
- `documents` - Member certificates, documents, profiles (future use)

**Notes**: Currently no active storage requirements, but prepared for future enhancements like document storage for members.

### 3. Events Service Storage (`stevtdev`)
**Service**: Event Management  
**Current Use**: Event-related media and documents  
**Containers**:
- `posters` - Event promotional materials
- `documents` - Event planning documents
- `photos` - Event photos for history/gallery

**Use Cases**:
- Store event promotional posters
- Archive event photos for club history
- Keep event planning documentation

### 4. Shifts Service Storage (`stsftdev`)
**Service**: Shift Management  
**Current Use**: Shift schedules and reports  
**Containers**:
- `schedules` - Shift schedules and rotas
- `reports` - Shift reports and logs

**Use Cases**:
- Export shift schedules
- Generate volunteer reports
- Track shift completion logs

### 5. Stock Service Storage (`ststkdev`)
**Service**: Stock Alert Management  
**Current Use**: Stock reports and analytics  
**Containers**:
- `reports` - Stock reports and analytics
- `exports` - Data exports

**Use Cases**:
- Generate stock usage reports
- Export stock alert history
- Analytics on stock patterns

### 6. Finance Service Storage (`stfindev`)
**Service**: Expense Management  
**Current Use**: **PRIMARY STORAGE** - Receipt uploads (required by FR-029)  
**Containers**:
- `receipts` - **Expense receipts (REQUIRED)** - Max 5MB, JPEG/PNG/PDF, min 200KB
- `invoices` - Club invoices
- `statements` - Financial statements
- `reports` - Financial reports and audits

**Use Cases**:
- Store expense receipts (mandatory requirement)
- Archive invoices
- Store financial statements
- Generate audit reports

**Compliance**: 7-year retention for financial records

## Lifecycle Management

All storage accounts have the following lifecycle policies:

- **Hot Tier**: Default for new uploads
- **Cool Tier**: After 90 days of inactivity
- **Deletion**: After 2,555 days (7 years) - compliance requirement
- **Soft Delete**: 30-day retention for deleted blobs

## Naming Convention

Pattern: `st{service}{env}`

Examples:
- `stmbrdev` - Membership service, dev environment
- `stfinprd` - Finance service, production environment

## Security

- **Public Access**: Disabled on all containers
- **TLS**: Minimum TLS 1.2 required
- **HTTPS Only**: All traffic must use HTTPS
- **Network Access**: Azure Services bypass enabled
- **Access Control**: Managed Identity for Function Apps

## Cost Optimization

- **SKU**: Standard_LRS (Locally Redundant Storage) for dev/test
- **Lifecycle**: Automatic tier transitions reduce costs
- **Retention**: Automated cleanup after retention period
- **Capacity**: Per-service accounts enable granular cost tracking

## Future Services

When adding new microservices:

1. Add storage account variable in `main.bicep`
2. Create new storage module instance
3. Configure appropriate containers for service needs
4. Update outputs section
5. Document in this file

## Related Requirements

- **FR-029**: Receipt image upload required for expenses
- **FR-037**: Receipt validation (5MB max, JPEG/PNG/PDF, 200KB min)
- Retention compliance: 7 years for financial records
