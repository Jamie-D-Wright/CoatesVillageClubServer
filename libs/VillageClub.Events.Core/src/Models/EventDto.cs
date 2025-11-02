namespace VillageClub.Events.Core.Models;

/// <summary>
/// Data transfer object for event information.
/// </summary>
public class EventDto
{
    /// <summary>
    /// Gets or sets the unique event identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the event title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the type of event.
    /// </summary>
    public EventType EventType { get; set; }

    /// <summary>
    /// Gets or sets the event start date and time.
    /// </summary>
    public DateTime StartDateTime { get; set; }

    /// <summary>
    /// Gets or sets the event end date and time.
    /// </summary>
    public DateTime EndDateTime { get; set; }

    /// <summary>
    /// Gets or sets the event status.
    /// </summary>
    public EventStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who created the event.
    /// </summary>
    public Guid CreatedById { get; set; }

    /// <summary>
    /// Gets or sets the event creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last update timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets when the event was published (if applicable).
    /// </summary>
    public DateTime? PublishedAt { get; set; }
}
