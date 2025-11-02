using Microsoft.EntityFrameworkCore;
using VillageClub.Contracts.Models;
using VillageClub.Events.Core.Models;
using VillageClub.Events.Core.Services;
using VillageClub.Events.Data;
using VillageClub.Events.Data.Entities;

namespace VillageClub.Events.Services;

/// <summary>
/// Service for event management operations (CRUD and state transitions).
/// Orchestrates Events.Core business logic with EF Core database operations.
/// </summary>
public class EventService : IEventService
{
    private readonly EventsDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventService"/> class.
    /// </summary>
    /// <param name="context">Database context.</param>
    public EventService(EventsDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<EventDto?> GetByIdAsync(Guid id)
    {
        var eventEntity = await _context.Events.FindAsync(id);

        return eventEntity == null ? null : ToDto(eventEntity);
    }

    /// <inheritdoc/>
    public async Task<PagedResult<EventDto>> GetAllAsync(int page, int pageSize, string? includeStatus = null)
    {
        var query = _context.Events.AsQueryable();

        if (!string.IsNullOrWhiteSpace(includeStatus))
        {
            query = query.Where(e => e.Status == includeStatus);
        }

        var totalCount = await query.CountAsync();

        var events = await query
            .OrderByDescending(e => e.StartDateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<EventDto>
        {
            Items = events.Select(ToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }

    /// <inheritdoc/>
    public async Task<EventDto> CreateAsync(CreateEventRequest request, Guid createdById)
    {
        var now = DateTime.UtcNow;

        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            EventType = request.EventType.ToString(),
            StartDateTime = request.StartDateTime,
            EndDateTime = request.EndDateTime,
            Status = EventStatus.Draft.ToString(),
            CreatedById = createdById,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync();

        return ToDto(eventEntity);
    }

    /// <inheritdoc/>
    public async Task<EventDto?> UpdateAsync(Guid id, UpdateEventRequest request)
    {
        var eventEntity = await _context.Events.FindAsync(id);

        if (eventEntity == null)
        {
            return null;
        }

        // Update only fields that are provided (partial update support)
        if (request.Title != null)
        {
            eventEntity.Title = request.Title;
        }

        if (request.Description != null)
        {
            eventEntity.Description = request.Description;
        }

        if (request.EventType.HasValue)
        {
            eventEntity.EventType = request.EventType.Value.ToString();
        }

        if (request.StartDateTime.HasValue)
        {
            eventEntity.StartDateTime = request.StartDateTime.Value;
        }

        if (request.EndDateTime.HasValue)
        {
            eventEntity.EndDateTime = request.EndDateTime.Value;
        }

        eventEntity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return ToDto(eventEntity);
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(Guid id)
    {
        var eventEntity = await _context.Events.FindAsync(id);

        if (eventEntity == null)
        {
            return false;
        }

        _context.Events.Remove(eventEntity);
        await _context.SaveChangesAsync();

        return true;
    }

    /// <inheritdoc/>
    public async Task<EventDto?> PublishAsync(Guid id)
    {
        return await TransitionStateAsync(id, EventStatus.Published);
    }

    /// <inheritdoc/>
    public async Task<EventDto?> CompleteAsync(Guid id)
    {
        return await TransitionStateAsync(id, EventStatus.Completed);
    }

    /// <inheritdoc/>
    public async Task<EventDto?> CancelAsync(Guid id)
    {
        return await TransitionStateAsync(id, EventStatus.Cancelled);
    }

    /// <summary>
    /// Converts an Event entity to an EventDto.
    /// </summary>
    /// <param name="eventEntity">Event entity.</param>
    /// <returns>Event DTO.</returns>
    public EventDto ToDto(Event eventEntity)
    {
        return new EventDto
        {
            Id = eventEntity.Id,
            Title = eventEntity.Title,
            Description = eventEntity.Description,
            EventType = Enum.Parse<EventType>(eventEntity.EventType),
            StartDateTime = eventEntity.StartDateTime,
            EndDateTime = eventEntity.EndDateTime,
            Status = Enum.Parse<EventStatus>(eventEntity.Status),
            CreatedById = eventEntity.CreatedById,
            CreatedAt = eventEntity.CreatedAt,
            UpdatedAt = eventEntity.UpdatedAt,
            PublishedAt = eventEntity.PublishedAt,
        };
    }

    private async Task<EventDto?> TransitionStateAsync(Guid id, EventStatus newStatus)
    {
        var eventEntity = await _context.Events.FindAsync(id);

        if (eventEntity == null)
        {
            return null;
        }

        // Parse current status
        if (!Enum.TryParse<EventStatus>(eventEntity.Status, out var currentStatus))
        {
            throw new InvalidOperationException($"Invalid event status: {eventEntity.Status}");
        }

        // Validate transition using Events.Core business logic
        var validation = EventBusinessLogic.ValidateStateTransition(
            currentStatus,
            newStatus,
            eventEntity.EndDateTime);

        if (!validation.IsValid)
        {
            throw new InvalidOperationException(validation.ErrorMessage);
        }

        // Apply state transition
        eventEntity.Status = newStatus.ToString();
        eventEntity.UpdatedAt = DateTime.UtcNow;

        // Set PublishedAt timestamp when publishing
        if (newStatus == EventStatus.Published)
        {
            eventEntity.PublishedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return ToDto(eventEntity);
    }
}
