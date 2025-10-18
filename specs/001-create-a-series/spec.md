# Feature Specification: Village Club Management Microservices

**Feature Branch**: `001-create-a-series`  
**Created**: October 18, 2025  
**Status**: Draft  
**Input**: User description: "Create a series of microservices to help manage the village club. The club runs events and has regular opening hours or 8pm-12am Friday and Saturday. The village club has three different types of users. Comittee members; these are in charge of running the club and fulfull various roles such as Treasurer, Chairman, Clerk, Bar Manager as well as more general comitee members. Volunteers give their time to run the events and do shifts behind the bar. There are finally members, who participate in events and use the bar on evenings when it is open. Users need to be managed, events need to be created and updated, shifts need to be filled by volunteers and members need to be able to see what is on. Volunteers need to be able to raise when certain stock is low- there is no inventory tracking or till system required because the till is a manual key punch that is standalone. Comittee members and volunteers who run events need to be able to raise expenses to the club so that they can be re-imbursed for them. All expenses require a reciept as proof of purchase and should be linked to an event. No UI is required for this project and is in a seperate repository. The UI will use these microservices so they need to be discoverable and documented."

## Clarifications

### Session 2025-10-18

- Q: What authentication and authorization strategy should be used across the microservices? → A: JWT tokens with API gateway - services will be behind an API gateway, tokens will contain role claims for authorization
- Q: What are the validation constraints for receipt image uploads? → A: Maximum 5MB file size, JPEG/PNG/PDF formats, minimum 200KB to ensure quality
- Q: What inter-service communication pattern should be used? → A: Synchronous REST/HTTP calls between services with circuit breaker pattern for resilience
- Q: What are the service availability and recovery expectations? → A: 99% uptime target, manual recovery acceptable, maximum 15 minutes recovery time objective during operating hours
- Q: What observability and monitoring strategy should be implemented? → A: Structured logging to centralized store, health check endpoints, key metrics (response time, error rate, auth failures)
- Q: How should shifts relate to events - can an event have multiple shifts? → A: An event can have multiple shifts, but the default should be 1 shift covering the whole event duration or the full Friday/Saturday evening (8pm-12am)

## User Scenarios & Testing *(mandatory)*

### User Story 1 - User Management and Authentication (Priority: P1)

Committee members need to manage users across three distinct roles (Committee, Volunteer, Member) including creating accounts, assigning roles, and updating user information. The system must support role-based access control to ensure users can only perform actions appropriate to their role.

**Why this priority**: Foundation for all other functionality. Without user management, no other features can function properly since all actions require authenticated users with appropriate permissions.

**Independent Test**: Can be fully tested by creating users with different roles and verifying that each role has appropriate access levels. Delivers immediate value by establishing the security foundation and enabling user onboarding.

**Acceptance Scenarios**:

1. **Given** a committee member is authenticated, **When** they create a new user account with role "Member", **Then** the new user can authenticate and access member-level features only
2. **Given** a committee member is authenticated, **When** they update a volunteer's role to committee member, **Then** the user gains committee-level permissions
3. **Given** a volunteer is authenticated, **When** they attempt to create a new user account, **Then** they receive an authorization error
4. **Given** a user account exists, **When** a committee member deactivates the account, **Then** the user can no longer authenticate
5. **Given** multiple users exist with different roles, **When** a committee member retrieves the user list, **Then** they see all users with their current roles and status

---

### User Story 2 - Event Management (Priority: P1)

Committee members need to create, update, and publish events for the village club. Members and volunteers need to view upcoming and past events to know what's happening at the club. Events should include details like date, time, description, and type.

**Why this priority**: Core functionality for the club's primary purpose. Events drive member engagement and volunteer participation. This is essential MVP functionality.

**Independent Test**: Can be fully tested by creating events, publishing them, and verifying that users with appropriate roles can view them. Delivers value by enabling event communication and planning.

**Acceptance Scenarios**:

1. **Given** a committee member is authenticated, **When** they create a new event with date, time, title and description, **Then** the event is saved and visible to all users
2. **Given** an event exists, **When** a committee member updates the event details, **Then** all users see the updated information
3. **Given** multiple events exist, **When** a member views the events list, **Then** they see all future events sorted by date
4. **Given** an event date has passed, **When** a user views past events, **Then** they can see historical events
5. **Given** a committee member is authenticated, **When** they delete an event, **Then** the event is no longer visible to any users

---

### User Story 3 - Shift Management for Bar Volunteers (Priority: P2)

Committee members and bar managers need to create shifts for bar opening hours (Friday and Saturday 8pm-12am) and events. Volunteers need to view available shifts and sign up for them. Committee members need to see which shifts are filled and which need volunteers.

**Why this priority**: Critical for operational continuity but depends on User Management and Event Management being in place. Ensures the bar can open and events can be staffed.

**Independent Test**: Can be fully tested by creating shifts, having volunteers sign up, and verifying shift assignments are tracked. Delivers value by solving volunteer coordination.

**Acceptance Scenarios**:

1. **Given** a committee member is authenticated, **When** they create a bar shift for Friday 8pm-12am, **Then** the shift is available for volunteer signup
2. **Given** an available shift exists, **When** a volunteer signs up for the shift, **Then** the shift shows as assigned to that volunteer
3. **Given** a shift is assigned to a volunteer, **When** the volunteer cancels their signup, **Then** the shift becomes available again
4. **Given** multiple shifts exist for an event, **When** a committee member views the event, **Then** they see all shifts and their assignment status
5. **Given** a shift is at capacity, **When** another volunteer tries to sign up, **Then** they receive a message that the shift is full

---

### User Story 4 - Stock Alert Management (Priority: P3)

Volunteers working behind the bar need to report when stock items are running low. Committee members and bar managers need to view these alerts to arrange restocking. The system should track which items are low and when alerts were raised.

**Why this priority**: Important for operational efficiency but not critical for MVP. The club can function with manual stock checks initially. This feature improves workflow but isn't blocking.

**Independent Test**: Can be fully tested by volunteers creating stock alerts and committee members viewing them. Delivers value by improving inventory awareness without requiring a full inventory system.

**Acceptance Scenarios**:

1. **Given** a volunteer is working a shift, **When** they create a stock alert for "Lager" with urgency "Low", **Then** the alert is saved and visible to committee members
2. **Given** multiple stock alerts exist, **When** a committee member views the alerts list, **Then** they see all unresolved alerts with item name, urgency, reporter, and date
3. **Given** a stock alert exists, **When** a committee member marks it as resolved, **Then** the alert is removed from the active alerts list
4. **Given** a volunteer creates a stock alert, **When** they include additional notes, **Then** the notes are visible to committee members reviewing the alert
5. **Given** multiple alerts exist for the same item, **When** viewing alerts, **Then** they are grouped to show recurring low-stock issues

---

### User Story 5 - Expense Management and Reimbursement (Priority: P2)

Committee members and volunteers who run events need to submit expenses for reimbursement, including uploading receipt images as proof of purchase. Expenses must be linked to specific events. Treasurers and committee members need to review, approve, or reject expense claims.

**Why this priority**: Essential for financial management and volunteer satisfaction. Must be in place before events generate significant expenses. Higher priority than stock alerts because it affects club finances and volunteer morale.

**Independent Test**: Can be fully tested by submitting expense claims with receipts, linking them to events, and processing approvals. Delivers value by establishing financial accountability and reimbursement workflow.

**Acceptance Scenarios**:

1. **Given** a volunteer has worked an event, **When** they submit an expense claim with amount, description, receipt image, and linked event, **Then** the expense is saved with status "Pending Review"
2. **Given** an expense claim exists with status "Pending Review", **When** a treasurer reviews and approves it, **Then** the status changes to "Approved" and the approval date is recorded
3. **Given** an expense claim lacks a receipt, **When** a volunteer tries to submit it, **Then** they receive a validation error requiring receipt upload
4. **Given** multiple expense claims exist, **When** a treasurer views expenses for a specific event, **Then** they see all expenses linked to that event with total amount
5. **Given** an expense claim is approved, **When** a treasurer marks it as paid, **Then** the status changes to "Reimbursed" and the payment date is recorded
6. **Given** a committee member reviews an expense claim, **When** they reject it with a reason, **Then** the status changes to "Rejected" and the submitter can see the rejection reason

---

### User Story 6 - Service Discovery and Documentation (Priority: P1)

Developers building the UI need to discover available microservices, understand their endpoints, and see API documentation. The system must provide machine-readable service definitions and human-readable documentation for each microservice.

**Why this priority**: Critical infrastructure requirement. Without proper service discovery and documentation, the separate UI repository cannot effectively consume these microservices. This is foundational for the architecture.

**Independent Test**: Can be fully tested by accessing service registry, retrieving service metadata, and viewing API documentation. Delivers value by enabling integration with the UI application.

**Acceptance Scenarios**:

1. **Given** all microservices are running, **When** a client queries the service registry, **Then** they receive a list of all available services with their endpoints and health status
2. **Given** a specific microservice is selected, **When** a developer accesses its documentation endpoint, **Then** they receive comprehensive API documentation including endpoints, request/response formats, and authentication requirements
3. **Given** a microservice is temporarily unavailable, **When** a client queries the service registry, **Then** the service is marked as unhealthy or unavailable
4. **Given** API documentation exists for a service, **When** a developer views it, **Then** they see example requests and responses for each endpoint
5. **Given** a new microservice version is deployed, **When** the service registry is queried, **Then** it reflects the updated service version information

---

### Edge Cases

- What happens when a volunteer tries to sign up for a shift that conflicts with another shift they've already committed to?
- How does the system handle expense receipt uploads that are too large or in unsupported formats?
- What happens when a committee member tries to delete an event that has assigned shifts and submitted expenses?
- How does the system handle users being assigned multiple committee roles simultaneously (e.g., Treasurer and Bar Manager)?
- What happens when regular bar opening hours fall on a holiday and the club is closed?
- How does the system handle a stock alert for an item that already has multiple unresolved alerts?
- What happens when a user's role is changed while they have pending actions (e.g., downgraded from committee to member while having pending expense approvals)?
- How does the system handle orphaned expenses if an event is deleted?

## Requirements *(mandatory)*

### Functional Requirements

**User Management Service:**

- **FR-001**: System MUST support three distinct user roles: Committee Member, Volunteer, and Member
- **FR-002**: System MUST allow committee members to create, update, and deactivate user accounts
- **FR-003**: System MUST support assignment of specific committee roles (Treasurer, Chairman, Clerk, Bar Manager, General Committee Member)
- **FR-004**: System MUST authenticate users via JWT tokens issued by a centralized authentication service, with tokens containing user identity and role claims
- **FR-005**: System MUST validate JWT tokens at the API gateway level before routing requests to microservices
- **FR-006**: System MUST prevent users from performing actions outside their role permissions by validating role claims in JWT tokens
- **FR-007**: System MUST maintain audit logs of user management actions (creation, role changes, deactivation)
- **FR-008**: System MUST allow users to update their own contact information (email, phone, address) while committee members control role and status assignments

**Event Management Service:**

- **FR-008**: System MUST allow committee members to create events with title, description, date, time, and event type
- **FR-009**: System MUST allow committee members to update and delete events
- **FR-010**: System MUST allow all authenticated users to view published events
- **FR-011**: System MUST display events in chronological order with clear distinction between past and upcoming events
- **FR-012**: System MUST support event categorization with four types: Special Event, Regular Bar Night, Private Hire, and Fundraiser
- **FR-013**: System MUST maintain historical event records even after the event has occurred

**Shift Management Service:**

- **FR-014**: System MUST allow committee members to create shifts for regular bar hours (Friday and Saturday 8pm-12am)
- **FR-015**: System MUST allow committee members to create shifts for events, with default of one shift covering the full event duration
- **FR-016**: System MUST support multiple shifts per event when additional coverage or role specialization is needed
- **FR-017**: System MUST allow volunteers to view available shifts and sign up for them
- **FR-018**: System MUST allow volunteers to cancel their shift signups at any time before the shift starts
- **FR-019**: System MUST track which volunteer is assigned to each shift
- **FR-020**: System MUST support multiple volunteers per shift with configurable capacity limits
- **FR-021**: System MUST prevent volunteers from signing up for overlapping shifts
- **FR-022**: System MUST link event-related shifts to their corresponding events

**Stock Alert Service:**

- **FR-022**: System MUST allow volunteers to create stock alerts with item name, urgency level, and optional notes
- **FR-023**: System MUST allow committee members and bar managers to view all active stock alerts
- **FR-024**: System MUST allow committee members to mark stock alerts as resolved
- **FR-025**: System MUST track who created each alert and when
- **FR-026**: System MUST support urgency levels for stock alerts (Low, Medium, High)
- **FR-027**: System MUST group alerts by item to identify recurring stock issues

**Expense Management Service:**

- **FR-028**: System MUST allow committee members and event volunteers to submit expense claims
- **FR-029**: System MUST require receipt image upload for all expense claims
- **FR-030**: System MUST require all expenses to be linked to a specific event
- **FR-031**: System MUST support expense claim statuses: Pending Review, Approved, Rejected, Reimbursed
- **FR-032**: System MUST allow treasurers and committee members to review, approve, or reject expense claims
- **FR-033**: System MUST capture approval/rejection date and approver identity
- **FR-034**: System MUST allow treasurers to mark approved expenses as reimbursed with payment date
- **FR-035**: System MUST support rejection reasons that are visible to the expense submitter
- **FR-036**: System MUST calculate total expenses per event
- **FR-037**: System MUST validate receipt images with maximum 5MB file size, JPEG/PNG/PDF formats only, and minimum 200KB size to ensure quality and readability

**Service Discovery & Documentation:**

- **FR-038**: System MUST provide a service registry that lists all available microservices
- **FR-039**: System MUST expose API documentation for each microservice in a standard format
- **FR-040**: System MUST provide health check endpoints for each microservice
- **FR-041**: System MUST include endpoint descriptions, request/response schemas, and authentication requirements in documentation
- **FR-042**: System MUST provide example requests and responses for each API endpoint
- **FR-043**: System MUST update service registry automatically when services are deployed or become unavailable
- **FR-044**: System MUST use synchronous REST/HTTP for inter-service communication with circuit breaker pattern to handle service failures gracefully

**Observability & Monitoring:**

- **FR-045**: System MUST implement structured logging with consistent format across all microservices to a centralized logging store
- **FR-046**: System MUST track key operational metrics including API response times, error rates, and authentication failure counts
- **FR-047**: System MUST expose metrics endpoints for monitoring service health and performance
- **FR-048**: System MUST log all authentication attempts, authorization decisions, and security-relevant events for audit purposes

### Key Entities

- **User**: Represents a person who interacts with the village club system. Has attributes including name, contact information, role (Committee/Volunteer/Member), specific committee position (if applicable), status (active/inactive), and authentication credentials. Related to Shifts (as assignee), StockAlerts (as reporter), and Expenses (as submitter).

- **Event**: Represents a scheduled activity at the village club. Has attributes including title, description, date, time, event type/category, and status. Related to Shifts (events can have one or multiple shifts, default is one shift covering full event duration) and Expenses (costs are linked to events).

- **Shift**: Represents a time period requiring volunteer coverage, either for regular bar hours or specific events. Has attributes including date, start time, end time, volunteer capacity, current assignments, and type (bar/event). Related to Event (if event-specific, with one-to-many relationship where an event can have multiple shifts) and User (assigned volunteers).

- **StockAlert**: Represents a notification that bar stock is running low. Has attributes including item name, urgency level (Low/Medium/High), notes, reporter, creation date, status (active/resolved), and resolver. Related to User (reporter and resolver).

- **Expense**: Represents a reimbursement claim for club-related purchases. Has attributes including amount, description, receipt image reference, submission date, status (Pending/Approved/Rejected/Reimbursed), approval date, rejection reason, payment date, submitter, and approver. Related to Event (all expenses must link to an event) and User (submitter and approver).

- **CommitteeRole**: Represents specific leadership positions within the committee. Has attributes including role name (Treasurer, Chairman, Clerk, Bar Manager, General Member) and associated permissions. Related to User (committee members can have specific roles).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Committee members can create and publish a new event in under 2 minutes
- **SC-002**: Volunteers can view available shifts and sign up for one in under 30 seconds
- **SC-003**: Expense submission including receipt upload completes in under 3 minutes
- **SC-004**: Service discovery provides complete microservice list with health status in under 1 second
- **SC-005**: System handles at least 50 concurrent users without performance degradation
- **SC-006**: All API endpoints respond within 2 seconds under normal load
- **SC-007**: 95% of stock alerts are acknowledged by committee members within 24 hours
- **SC-008**: 100% of submitted expenses include valid receipt attachments
- **SC-009**: Zero unauthorized access attempts succeed against role-protected endpoints
- **SC-010**: API documentation accuracy verified by successful UI integration with zero documentation-related issues
- **SC-011**: Shift fill rate achieves 90% for regular bar hours within first month of deployment
- **SC-012**: Committee members can review and approve/reject expense claims in under 5 minutes per claim
- **SC-013**: System achieves 99% uptime during operating hours (Friday and Saturday 8pm-12am)
- **SC-014**: Service recovery completes within 15 minutes of failure detection during operating hours
