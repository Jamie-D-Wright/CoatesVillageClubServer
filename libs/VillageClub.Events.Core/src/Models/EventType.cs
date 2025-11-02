namespace VillageClub.Events.Core.Models;

/// <summary>
/// Represents the type of event at the village club.
/// </summary>
public enum EventType
{
    /// <summary>
    /// Special one-off event (e.g., quiz night, live music).
    /// </summary>
    SpecialEvent,

    /// <summary>
    /// Regular Friday/Saturday bar opening hours.
    /// </summary>
    RegularBarNight,

    /// <summary>
    /// Private hire of club facilities.
    /// </summary>
    PrivateHire,

    /// <summary>
    /// Fundraising event for club or community.
    /// </summary>
    Fundraiser,
}
