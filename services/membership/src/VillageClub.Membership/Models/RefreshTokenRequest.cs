namespace VillageClub.Membership.Models;

/// <summary>
/// Request model for refreshing an access token.
/// </summary>
public class RefreshTokenRequest
{
    /// <summary>
    /// Gets or sets the refresh token to exchange for a new access token.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;
}
