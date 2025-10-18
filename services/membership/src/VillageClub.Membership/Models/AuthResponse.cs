namespace VillageClub.Membership.Models;

/// <summary>
/// Response model for successful authentication.
/// </summary>
public class AuthResponse
{
    /// <summary>
    /// Gets or sets the JWT access token.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the refresh token for obtaining new access tokens.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the expiration timestamp for the access token.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the authenticated user's information.
    /// </summary>
    public UserDto User { get; set; } = null!;
}
