namespace VillageClub.Contracts.Auth;

/// <summary>
/// Service for generating and validating JWT tokens.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generates a JWT access token for the given user.
    /// </summary>
    /// <param name="userId">User identifier.</param>
    /// <param name="email">User email.</param>
    /// <param name="role">User role.</param>
    /// <param name="committeeRole">Optional committee role.</param>
    /// <returns>JWT token string.</returns>
    string GenerateAccessToken(Guid userId, string email, string role, string? committeeRole = null);

    /// <summary>
    /// Generates a refresh token.
    /// </summary>
    /// <returns>Refresh token string.</returns>
    string GenerateRefreshToken();

    /// <summary>
    /// Validates a JWT token and extracts claims.
    /// </summary>
    /// <param name="token">JWT token to validate.</param>
    /// <returns>Token validation result with claims.</returns>
    TokenValidationResult ValidateToken(string token);

    /// <summary>
    /// Gets the public key for token validation (for other services).
    /// </summary>
    /// <returns>Public key string.</returns>
    string GetPublicKey();
}

/// <summary>
/// Result of token validation.
/// </summary>
public class TokenValidationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the token is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the user identifier from the token.
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Gets or sets the email from the token.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the role from the token.
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// Gets or sets the committee role from the token.
    /// </summary>
    public string? CommitteeRole { get; set; }

    /// <summary>
    /// Gets or sets the error message if validation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}
