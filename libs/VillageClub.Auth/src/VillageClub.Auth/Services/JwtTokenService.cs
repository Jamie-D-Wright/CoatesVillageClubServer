using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using VillageClub.Contracts.Auth;
using TokenValidationResult = VillageClub.Contracts.Auth.TokenValidationResult;

namespace VillageClub.Auth.Services;

/// <summary>
/// Service for JWT token generation and validation using RSA keys.
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
    /// <param name="issuer">JWT issuer.</param>
    /// <param name="audience">JWT audience.</param>
    /// <param name="accessTokenExpirationMinutes">Access token expiration in minutes.</param>
    /// <param name="privateKey">Optional RSA private key (base64 encoded). If null, generates new key.</param>
    public JwtTokenService(
        string issuer,
        string audience,
        int accessTokenExpirationMinutes = 15,
        string? privateKey = null)
    {
        if (string.IsNullOrWhiteSpace(issuer))
        {
            throw new ArgumentException("Issuer cannot be null or empty", nameof(issuer));
        }

        if (string.IsNullOrWhiteSpace(audience))
        {
            throw new ArgumentException("Audience cannot be null or empty", nameof(audience));
        }

        if (accessTokenExpirationMinutes <= 0)
        {
            throw new ArgumentException("Access token expiration must be greater than 0", nameof(accessTokenExpirationMinutes));
        }

        _issuer = issuer;
        _audience = audience;
        _accessTokenExpirationMinutes = accessTokenExpirationMinutes;

        _rsa = RSA.Create(2048);

        // Load RSA private key if provided
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
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Role, role),
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
