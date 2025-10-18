using VillageClub.Contracts.Enums;

namespace VillageClub.Membership.Models;

/// <summary>
/// Request model for creating a new user
/// </summary>
public class CreateUserRequest
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string? Address { get; set; }

    public UserRole Role { get; set; }

    public CommitteeRole? CommitteeRole { get; set; }
}
