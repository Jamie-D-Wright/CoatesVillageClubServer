using VillageClub.Contracts.Models;
using VillageClub.Events.Core.Models;

namespace VillageClub.Events.Services;

/// <summary>
/// Service interface for event management operations.
/// </summary>
public interface IEventService
{
    /// <summary>
    /// Gets an event by ID.
    /// </summary>
    /// <param name="id">Event ID.</param>
    /// <returns>Event DTO or null if not found.</returns>
    Task<EventDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets a paginated list of events.
    /// </summary>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="includeStatus">Optional status filter.</param>
    /// <returns>Paged result of events.</returns>
    Task<PagedResult<EventDto>> GetAllAsync(int page, int pageSize, string? includeStatus = null);

    /// <summary>
    /// Creates a new event.
    /// </summary>
    /// <param name="request">Create event request.</param>
    /// <param name="createdById">ID of the user creating the event.</param>
    /// <returns>Created event DTO.</returns>
    Task<EventDto> CreateAsync(CreateEventRequest request, Guid createdById);

    /// <summary>
    /// Updates an existing event.
    /// </summary>
    /// <param name="id">Event ID.</param>
    /// <param name="request">Update event request.</param>
    /// <returns>Updated event DTO or null if not found.</returns>
    Task<EventDto?> UpdateAsync(Guid id, UpdateEventRequest request);

    /// <summary>
    /// Deletes an event.
    /// </summary>
    /// <param name="id">Event ID.</param>
    /// <returns>True if deleted; false if not found.</returns>
    Task<bool> DeleteAsync(Guid id);

    /// <summary>
    /// Publishes a draft event.
    /// </summary>
    /// <param name="id">Event ID.</param>
    /// <returns>Updated event DTO or null if not found or invalid transition.</returns>
    Task<EventDto?> PublishAsync(Guid id);

    /// <summary>
    /// Completes a published event.
    /// </summary>
    /// <param name="id">Event ID.</param>
    /// <returns>Updated event DTO or null if not found or invalid transition.</returns>
    Task<EventDto?> CompleteAsync(Guid id);

    /// <summary>
    /// Cancels a draft or published event.
    /// </summary>
    /// <param name="id">Event ID.</param>
    /// <returns>Updated event DTO or null if not found or invalid transition.</returns>
    Task<EventDto?> CancelAsync(Guid id);
}
