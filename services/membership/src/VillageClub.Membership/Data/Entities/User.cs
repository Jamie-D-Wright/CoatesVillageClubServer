using VillageClub.Contracts.Enums;

namespace VillageClub.Membership.Data.Entities;

/// <summary>
/// User entity representing a person who interacts with the village club system.
/// </summary>
public class User
{
    /// <summary>
    /// Gets or sets the unique identifier for the user.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the user's email address (unique login identifier).
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the hashed password using BCrypt.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's first name.
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's last name.
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's phone number (optional).
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Gets or sets the user's address (optional).
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Gets or sets the user's role (Member, Volunteer, or Committee).
    /// </summary>
    public UserRole Role { get; set; }

    /// <summary>
    /// Gets or sets the committee role (only for Committee members).
    /// </summary>
    public CommitteeRole? CommitteeRole { get; set; }

    /// <summary>
    /// Gets or sets the user's account status (Active, Inactive, or Suspended).
    /// </summary>
    public UserStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the user account was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the user account was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the user last logged in (optional).
    /// </summary>
    public DateTime? LastLoginAt { get; set; }
}
