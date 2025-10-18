using VillageClub.Contracts.Enums;

namespace VillageClub.Membership.Models;

/// <summary>
/// Request model for updating an existing user.
/// </summary>
public class UpdateUserRequest
{
    /// <summary>
    /// Gets or sets the user's first name (optional - only updates if provided).
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Gets or sets the user's last name (optional - only updates if provided).
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Gets or sets the user's phone number (optional - only updates if provided).
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Gets or sets the user's address (optional - only updates if provided).
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Gets or sets the user's role (optional - only updates if provided).
    /// </summary>
    public UserRole? Role { get; set; }

    /// <summary>
    /// Gets or sets the committee role (optional - only updates if provided).
    /// </summary>
    public CommitteeRole? CommitteeRole { get; set; }

    /// <summary>
    /// Gets or sets the user's account status (optional - only updates if provided).
    /// </summary>
    public UserStatus? Status { get; set; }
}
