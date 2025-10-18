using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using VillageClub.Contracts.Enums;
using VillageClub.Membership.Services;
using Xunit;

namespace VillageClub.Membership.Tests.Services;

public class JwtTokenServiceTests : IDisposable
{
    private readonly JwtTokenService _sut;
    private readonly Dictionary<string, string> _originalEnvVars = new();

    public JwtTokenServiceTests()
    {
        // Save original environment variables
        _originalEnvVars["JWT_ISSUER"] = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? string.Empty;
        _originalEnvVars["JWT_AUDIENCE"] = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? string.Empty;
        _originalEnvVars["JWT_ACCESS_TOKEN_EXPIRATION_MINUTES"] = Environment.GetEnvironmentVariable("JWT_ACCESS_TOKEN_EXPIRATION_MINUTES") ?? string.Empty;

        // Set test environment variables
        Environment.SetEnvironmentVariable("JWT_ISSUER", "https://test-issuer.com");
        Environment.SetEnvironmentVariable("JWT_AUDIENCE", "https://test-audience.com");
        Environment.SetEnvironmentVariable("JWT_ACCESS_TOKEN_EXPIRATION_MINUTES", "15");

        _sut = new JwtTokenService();
    }

    public void Dispose()
    {
        // Restore original environment variables
        foreach (var kvp in _originalEnvVars)
        {
            Environment.SetEnvironmentVariable(kvp.Key, string.IsNullOrEmpty(kvp.Value) ? null : kvp.Value);
        }
    }

    [Fact]
    public void GenerateAccessToken_ShouldReturnValidJwtToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var role = UserRole.Committee.ToString();

        // Act
        var token = _sut.GenerateAccessToken(userId, email, role, null);

        // Assert
        token.Should().NotBeNullOrEmpty();
        token.Split('.').Should().HaveCount(3, "JWT tokens have 3 parts: header.payload.signature");
    }

    [Fact]
    public void GenerateAccessToken_ShouldIncludeCorrectClaims()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "committee@example.com";
        var role = UserRole.Committee.ToString();
        var committeeRole = CommitteeRole.Chairman.ToString();

        // Act
        var token = _sut.GenerateAccessToken(userId, email, role, committeeRole);

        // Assert - Decode token and verify claims
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // ClaimTypes constants expand to full URIs, so check by short name
        jwtToken.Claims.Should().Contain(c => c.Type == "nameid" && c.Value == userId.ToString());
        jwtToken.Claims.Should().Contain(c => c.Type == "email" && c.Value == email);
        jwtToken.Claims.Should().Contain(c => c.Type == "role" && c.Value == role);
        jwtToken.Claims.Should().Contain(c => c.Type == "CommitteeRole" && c.Value == committeeRole);
        jwtToken.Issuer.Should().Be("https://test-issuer.com");
        jwtToken.Audiences.Should().Contain("https://test-audience.com");
    }

    [Fact]
    public void GenerateAccessToken_ShouldNotIncludeCommitteeRole_WhenNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "volunteer@example.com";
        var role = UserRole.Volunteer.ToString();

        // Act
        var token = _sut.GenerateAccessToken(userId, email, role, null);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Claims.Should().NotContain(c => c.Type == "committee_role");
    }

    [Fact]
    public void GenerateAccessToken_ShouldHaveCorrectExpiration()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var role = UserRole.Member.ToString();
        var beforeGeneration = DateTime.UtcNow;

        // Act
        var token = _sut.GenerateAccessToken(userId, email, role, null);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        var expectedExpiration = beforeGeneration.AddMinutes(15);

        jwtToken.ValidTo.Should().BeCloseTo(expectedExpiration, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnBase64String()
    {
        // Act
        var token = _sut.GenerateRefreshToken();

        // Assert
        token.Should().NotBeNullOrEmpty();
        token.Length.Should().BeGreaterThan(40, "64 bytes in base64 should be longer than 40 characters");
        
        // Verify it's valid base64
        var action = () => Convert.FromBase64String(token);
        action.Should().NotThrow("token should be valid base64");
    }

    [Fact]
    public void GenerateRefreshToken_ShouldGenerateUniqueTokens()
    {
        // Act
        var token1 = _sut.GenerateRefreshToken();
        var token2 = _sut.GenerateRefreshToken();
        var token3 = _sut.GenerateRefreshToken();

        // Assert
        var tokens = new[] { token1, token2, token3 };
        tokens.Should().OnlyHaveUniqueItems("cryptographically random tokens should be unique");
    }

    [Fact]
    public void ValidateToken_ShouldReturnSuccess_ForValidToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var role = UserRole.Committee.ToString();
        var committeeRole = CommitteeRole.Treasurer.ToString();
        var token = _sut.GenerateAccessToken(userId, email, role, committeeRole);

        // Act
        var result = _sut.ValidateToken(token);

        // Assert
        result.IsValid.Should().BeTrue();
        result.UserId.Should().Be(userId);
        result.Email.Should().Be(email);
        result.Role.Should().Be(role);
        result.CommitteeRole.Should().Be(committeeRole);
    }

    [Fact]
    public void ValidateToken_ShouldReturnSuccess_WhenCommitteeRoleIsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "member@example.com";
        var role = UserRole.Member.ToString();
        var token = _sut.GenerateAccessToken(userId, email, role, null);

        // Act
        var result = _sut.ValidateToken(token);

        // Assert
        result.IsValid.Should().BeTrue();
        result.UserId.Should().Be(userId);
        result.Email.Should().Be(email);
        result.Role.Should().Be(role);
        result.CommitteeRole.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_ShouldReturnFailure_ForInvalidToken()
    {
        // Arrange
        var invalidToken = "invalid.jwt.token";

        // Act
        var result = _sut.ValidateToken(invalidToken);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateToken_ShouldReturnFailure_ForMalformedToken()
    {
        // Arrange
        var malformedToken = "not-a-jwt-token-at-all";

        // Act
        var result = _sut.ValidateToken(malformedToken);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateToken_ShouldReturnFailure_ForEmptyToken()
    {
        // Act
        var result = _sut.ValidateToken(string.Empty);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateToken_ShouldReturnFailure_ForNullToken()
    {
        // Act
        var result = _sut.ValidateToken(null!);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetPublicKey_ShouldReturnBase64EncodedKey()
    {
        // Act
        var publicKey = _sut.GetPublicKey();

        // Assert
        publicKey.Should().NotBeNullOrEmpty();
        
        // Verify it's valid base64
        var action = () => Convert.FromBase64String(publicKey);
        action.Should().NotThrow("public key should be valid base64");
    }

    [Fact]
    public void GetPublicKey_ShouldReturnConsistentKey()
    {
        // Act
        var key1 = _sut.GetPublicKey();
        var key2 = _sut.GetPublicKey();

        // Assert
        key1.Should().Be(key2, "the same service instance should return the same public key");
    }

    [Fact]
    public void ValidateToken_ShouldWork_WithDifferentRoles()
    {
        // Arrange & Act
        var roles = new[]
        {
            UserRole.Committee.ToString(),
            UserRole.Volunteer.ToString(),
            UserRole.Member.ToString()
        };

        foreach (var role in roles)
        {
            var userId = Guid.NewGuid();
            var token = _sut.GenerateAccessToken(userId, $"{role}@example.com", role, null);
            var result = _sut.ValidateToken(token);

            // Assert
            result.IsValid.Should().BeTrue($"token with role {role} should be valid");
            result.Role.Should().Be(role);
        }
    }

    [Fact]
    public void ValidateToken_ShouldRejectToken_FromDifferentIssuer()
    {
        // Arrange - Generate token with one service
        var userId = Guid.NewGuid();
        var token = _sut.GenerateAccessToken(userId, "test@example.com", UserRole.Member.ToString(), null);

        // Change issuer and create new service
        Environment.SetEnvironmentVariable("JWT_ISSUER", "https://different-issuer.com");
        var differentService = new JwtTokenService();

        // Act
        var result = differentService.ValidateToken(token);

        // Assert
        result.IsValid.Should().BeFalse("token from different issuer should be rejected");
    }

    [Fact]
    public void GenerateAccessToken_ShouldGenerateDifferentTokens_DueToTimestamp()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var role = UserRole.Member.ToString();

        // Act
        var token1 = _sut.GenerateAccessToken(userId, email, role, null);
        // Small delay to ensure different timestamp
        Thread.Sleep(1100);
        var token2 = _sut.GenerateAccessToken(userId, email, role, null);

        // Assert
        token1.Should().NotBe(token2, "tokens generated at different times should differ");
    }
}
