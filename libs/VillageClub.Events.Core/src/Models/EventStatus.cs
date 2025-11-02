namespace VillageClub.Events.Core.Models;

/// <summary>
/// Represents the status of an event.
/// </summary>
public enum EventStatus
{
    /// <summary>
    /// Event is being drafted and not visible to members.
    /// </summary>
    Draft,

    /// <summary>
    /// Event is published and visible to all users.
    /// </summary>
    Published,

    /// <summary>
    /// Event was cancelled.
    /// </summary>
    Cancelled,

    /// <summary>
    /// Event has concluded.
    /// </summary>
    Completed,
}
