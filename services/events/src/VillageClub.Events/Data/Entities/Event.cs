namespace VillageClub.Events.Data.Entities;

/// <summary>
/// Represents an event at the village club.
/// </summary>
/// <remarks>
/// Events can be special events (quiz nights, live music), regular bar nights,
/// private hires, or fundraisers. Each event has a status lifecycle from Draft
/// to Published to Completed, with the ability to be Cancelled.
/// </remarks>
public class Event
{
    /// <summary>
    /// Gets or sets the unique identifier for the event.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the event title.
    /// </summary>
    /// <remarks>
    /// Must be between 3 and 200 characters.
    /// </remarks>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event description in Markdown format.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the type of event.
    /// </summary>
    /// <remarks>
    /// Valid values: SpecialEvent, RegularBarNight, PrivateHire, Fundraiser.
    /// </remarks>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event start date and time.
    /// </summary>
    /// <remarks>
    /// Must be in the future when creating a new event.
    /// </remarks>
    public DateTime StartDateTime { get; set; }

    /// <summary>
    /// Gets or sets the event end date and time.
    /// </summary>
    /// <remarks>
    /// Must be after StartDateTime. Duration must be between 30 minutes and 12 hours.
    /// </remarks>
    public DateTime EndDateTime { get; set; }

    /// <summary>
    /// Gets or sets the event status.
    /// </summary>
    /// <remarks>
    /// Valid values: Draft, Published, Cancelled, Completed.
    /// Default is Draft. State transitions: Draft → Published → Completed, with Cancelled available from Draft or Published.
    /// </remarks>
    public string Status { get; set; } = "Draft";

    /// <summary>
    /// Gets or sets the ID of the user who created the event.
    /// </summary>
    /// <remarks>
    /// References Membership.Users.Id.
    /// </remarks>
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
    /// Gets or sets the timestamp when the event was published.
    /// </summary>
    /// <remarks>
    /// Null for Draft events. Set when status changes to Published.
    /// </remarks>
    public DateTime? PublishedAt { get; set; }
}
