namespace VillageClub.Membership.Models;

/// <summary>
/// Request model for changing a user's password.
/// </summary>
public class ChangePasswordRequest
{
    /// <summary>
    /// Gets or sets the user's current password for verification.
    /// </summary>
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the new password to set.
    /// </summary>
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the confirmation of the new password (must match NewPassword).
    /// </summary>
    public string ConfirmPassword { get; set; } = string.Empty;
}
