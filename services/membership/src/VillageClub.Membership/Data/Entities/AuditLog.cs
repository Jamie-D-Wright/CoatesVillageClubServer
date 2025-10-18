namespace VillageClub.Membership.Data.Entities;

/// <summary>
/// Audit log entry for tracking user management actions.
/// </summary>
public class AuditLog
{
    /// <summary>
    /// Gets or sets the unique identifier for the audit log entry.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the action occurred.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the user identifier who performed the action (optional).
    /// </summary>
    public Guid? ActorId { get; set; }

    /// <summary>
    /// Gets or sets the action that was performed (e.g., "CreateUser", "UpdateUser").
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user identifier that was affected by this action (optional).
    /// </summary>
    public Guid? TargetUserId { get; set; }

    /// <summary>
    /// Gets or sets additional details about the action in JSON format (optional).
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Gets or sets the IP address of the actor (optional).
    /// </summary>
    public string? IpAddress { get; set; }
}
