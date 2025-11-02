# VillageClub.Events.Core

**Purpose**: Pure business logic library for event management in the Village Club system.

**Scope**: Framework-agnostic event management business rules and validation logic.

## What Belongs Here

✅ **Event business logic**:
- Event state transitions (Draft → Published → Completed)
- Event validation rules (date validation, duration checks)
- Event type-specific business rules
- Event capacity and attendance logic

✅ **Validators** (FluentValidation):
- CreateEventRequest validation
- UpdateEventRequest validation
- Event field validation (title length, dates, etc.)

✅ **Pure models/DTOs**:
- EventDto
- CreateEventRequest
- UpdateEventRequest
- EventType enum
- EventStatus enum

## What Does NOT Belong Here

❌ **Database operations** → Use EventService in the Events service
❌ **HTTP concerns** → Use EventFunctions in the Events service
❌ **Infrastructure** → Use DI configuration in Events service Program.cs
❌ **Entity Framework entities** → These live in Events service Data layer
❌ **Azure Functions SDK** → This library is framework-agnostic

## Dependencies

- ✅ FluentValidation (for validation logic)
- ❌ No Entity Framework
- ❌ No Azure Functions SDK
- ❌ No ASP.NET Core

## Usage

This library is consumed by the Events service (`services/events/src/VillageClub.Events/`) which orchestrates:
- Events.Core business logic (this library)
- Database operations (Entity Framework)
- HTTP endpoints (Azure Functions)

## Example

```csharp
// Business logic in library
public class EventBusinessLogic
{
    public EventStatus TransitionState(EventStatus current, EventCommand command)
    {
        return command switch
        {
            EventCommand.Publish when current == EventStatus.Draft => EventStatus.Published,
            EventCommand.Complete when current == EventStatus.Published => EventStatus.Completed,
            _ => throw new InvalidOperationException($"Cannot {command} from {current}")
        };
    }
}

// Service orchestration in Events service
public class EventService
{
    private readonly EventBusinessLogic _logic;
    private readonly EventsDbContext _db;
    
    public async Task PublishEvent(int eventId)
    {
        var entity = await _db.Events.FindAsync(eventId);
        entity.Status = _logic.TransitionState(entity.Status, EventCommand.Publish);
        await _db.SaveChangesAsync();
    }
}
```

## Version

**Created**: 2025-11-01  
**Part of**: US2 (Event Management) - Phase 5  
**Constitution**: v2.4.0 - Library-First Development (Principle IX)
