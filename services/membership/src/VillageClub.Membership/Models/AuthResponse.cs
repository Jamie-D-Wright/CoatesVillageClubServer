namespace VillageClub.Membership.Models;

/// <summary>
/// Response model for successful authentication
/// </summary>
public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;

    public string RefreshToken { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public UserDto User { get; set; } = null!;
}
