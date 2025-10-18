using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using VillageClub.Contracts.Auth;
using TokenValidationResult = VillageClub.Contracts.Auth.TokenValidationResult;

namespace VillageClub.Membership.Services;

/// <summary>
/// Service for JWT token generation and validation using RSA keys
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly RSA _rsa;

    private readonly string _issuer;

    private readonly string _audience;

    private readonly int _accessTokenExpirationMinutes;

    /// <summary>
    /// Initializes a new instance of the <see cref="JwtTokenService"/> class.
    /// </summary>
    public JwtTokenService()
    {
        _rsa = RSA.Create(2048);
        _issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "VillageClub.Membership";
        _audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "VillageClub";
        _accessTokenExpirationMinutes = int.TryParse(Environment.GetEnvironmentVariable("JWT_ACCESS_TOKEN_EXPIRATION_MINUTES"), out var accessMinutes)
            ? accessMinutes
            : 15;

        // Load RSA keys from environment variables if available
        var privateKey = Environment.GetEnvironmentVariable("JWT_PRIVATE_KEY");
        if (!string.IsNullOrEmpty(privateKey))
        {
            _rsa.ImportRSAPrivateKey(Convert.FromBase64String(privateKey), out _);
        }
    }

    /// <summary>
    /// Generates an access token for a user.
    /// </summary>
    /// <param name="userId">User identifier.</param>
    /// <param name="email">User email.</param>
    /// <param name="role">User role.</param>
    /// <param name="committeeRole">Optional committee role.</param>
    /// <returns>JWT token string.</returns>
    public string GenerateAccessToken(Guid userId, string email, string role, string? committeeRole = null)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role),
        };

        if (!string.IsNullOrEmpty(committeeRole))
        {
            claims.Add(new Claim("CommitteeRole", committeeRole));
        }

        var expiresAt = DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = new SigningCredentials(
                new RsaSecurityKey(_rsa),
                SecurityAlgorithms.RsaSha256),
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Generates a refresh token (cryptographically random string).
    /// </summary>
    /// <returns>Refresh token string.</returns>
    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        return Convert.ToBase64String(randomBytes);
    }

    /// <summary>
    /// Validates a JWT access token.
    /// </summary>
    /// <param name="token">Token to validate.</param>
    /// <returns>Validation result with claims if valid.</returns>
    public TokenValidationResult ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _issuer,
            ValidAudience = _audience,
            IssuerSigningKey = new RsaSecurityKey(_rsa),
            ClockSkew = TimeSpan.FromMinutes(5),
        };

        try
        {
            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = principal.FindFirst(ClaimTypes.Email)?.Value;
            var role = principal.FindFirst(ClaimTypes.Role)?.Value;
            var committeeRole = principal.FindFirst("CommitteeRole")?.Value;

            return new TokenValidationResult
            {
                IsValid = true,
                UserId = Guid.TryParse(userId, out var id) ? id : null,
                Email = email,
                Role = role,
                CommitteeRole = committeeRole,
            };
        }
        catch (Exception ex)
        {
            return new TokenValidationResult
            {
                IsValid = false,
                ErrorMessage = ex.Message,
            };
        }
    }

    /// <summary>
    /// Gets the RSA public key for token verification by other services.
    /// </summary>
    /// <returns>Base64-encoded public key.</returns>
    public string GetPublicKey()
    {
        var publicKey = _rsa.ExportRSAPublicKey();
        return Convert.ToBase64String(publicKey);
    }
}
