using VillageClub.Contracts.Enums;

namespace VillageClub.Membership.Models;

/// <summary>
/// Request model for updating an existing user
/// </summary>
public class UpdateUserRequest
{
    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Address { get; set; }

    public UserRole? Role { get; set; }

    public CommitteeRole? CommitteeRole { get; set; }

    public UserStatus? Status { get; set; }
}
