using VillageClub.Contracts.Enums;

namespace VillageClub.Membership.Data.Entities;

/// <summary>
/// User entity representing a person who interacts with the village club system
/// </summary>
public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public UserRole Role { get; set; }
    public CommitteeRole? CommitteeRole { get; set; }
    public UserStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}
