namespace VillageClub.Events.Core.Models;

/// <summary>
/// Request model for creating a new event.
/// </summary>
public class CreateEventRequest
{
    /// <summary>
    /// Gets or sets the event title (3-200 characters).
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event description (optional, supports Markdown).
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
}
