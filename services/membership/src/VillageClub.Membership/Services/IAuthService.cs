using VillageClub.Membership.Data.Entities;
using VillageClub.Membership.Models;

namespace VillageClub.Membership.Services;

/// <summary>
/// Service for authentication operations (login, registration, token management).
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Authenticates a user with email and password.
    /// </summary>
    /// <param name="request">Login request containing email and password.</param>
    /// <returns>Authentication response with tokens and user data, or null if authentication fails.</returns>
    Task<AuthResponse?> LoginAsync(LoginRequest request);

    /// <summary>
    /// Registers a new user.
    /// </summary>
    /// <param name="request">User registration request.</param>
    /// <returns>Authentication response with tokens and user data.</returns>
    Task<AuthResponse> RegisterAsync(CreateUserRequest request);

    /// <summary>
    /// Refreshes an access token using a refresh token.
    /// </summary>
    /// <param name="request">Refresh token request.</param>
    /// <returns>New authentication response with refreshed tokens, or null if refresh token is invalid.</returns>
    Task<AuthResponse?> RefreshTokenAsync(RefreshTokenRequest request);

    /// <summary>
    /// Revokes a refresh token (logout).
    /// </summary>
    /// <param name="token">Refresh token to revoke.</param>
    /// <returns>True if token was revoked, false if token was not found.</returns>
    Task<bool> RevokeTokenAsync(string token);

    /// <summary>
    /// Changes a user's password.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="request">Change password request.</param>
    /// <returns>True if password was changed, false if current password is incorrect.</returns>
    Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);

    /// <summary>
    /// Records user login for audit purposes.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    Task RecordLoginAsync(Guid userId);
}
