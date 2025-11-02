namespace VillageClub.Events.Core.Models;

/// <summary>
/// Request model for updating an existing event.
/// </summary>
public class UpdateEventRequest
{
    /// <summary>
    /// Gets or sets the event title (3-200 characters).
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the event description (optional, supports Markdown).
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the type of event.
    /// </summary>
    public EventType? EventType { get; set; }

    /// <summary>
    /// Gets or sets the event start date and time.
    /// </summary>
    public DateTime? StartDateTime { get; set; }

    /// <summary>
    /// Gets or sets the event end date and time.
    /// </summary>
    public DateTime? EndDateTime { get; set; }
}
