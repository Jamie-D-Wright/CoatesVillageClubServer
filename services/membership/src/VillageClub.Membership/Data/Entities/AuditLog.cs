namespace VillageClub.Membership.Data.Entities;

/// <summary>
/// Audit log entry for tracking user management actions
/// </summary>
public class AuditLog
{
    public long Id { get; set; }
    public DateTime Timestamp { get; set; }
    public Guid? ActorId { get; set; }
    public string Action { get; set; } = string.Empty;
    public Guid? TargetUserId { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
}
