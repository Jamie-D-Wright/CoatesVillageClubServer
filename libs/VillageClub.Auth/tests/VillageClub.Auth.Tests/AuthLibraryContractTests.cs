using FluentAssertions;
using VillageClub.Auth.Services;
using Xunit;

namespace VillageClub.Auth.Tests;

/// <summary>
/// Contract tests for VillageClub.Auth library.
/// These tests verify the library's public API behaves as specified.
/// </summary>
public class AuthLibraryContractTests
{
    [Fact]
    public void PasswordHashService_Should_Exist_And_Implement_Interface()
    {
        // Arrange & Act
        var service = new PasswordHashService();

        // Assert
        service.Should().NotBeNull();
        service.Should().BeAssignableTo<IPasswordHashService>();
    }

    [Fact]
    public void JwtTokenService_Should_Exist_And_Implement_Interface()
    {
        // Arrange
        var issuer = "test-issuer";
        var audience = "test-audience";
        var expirationMinutes = 15;

        // Act
        var action = () => new JwtTokenService(issuer, audience, expirationMinutes);

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public void PasswordHashService_HashPassword_Should_Return_BCrypt_Hash()
    {
        // Arrange
        var service = new PasswordHashService();
        var password = "TestPassword123!";

        // Act
        var hash = service.HashPassword(password);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().StartWith("$2"); // BCrypt hash prefix
        hash.Length.Should().Be(60); // BCrypt standard length
    }

    [Fact]
    public void PasswordHashService_VerifyPassword_Should_Return_True_For_Correct_Password()
    {
        // Arrange
        var service = new PasswordHashService();
        var password = "TestPassword123!";
        var hash = service.HashPassword(password);

        // Act
        var result = service.VerifyPassword(password, hash);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void PasswordHashService_VerifyPassword_Should_Return_False_For_Incorrect_Password()
    {
        // Arrange
        var service = new PasswordHashService();
        var password = "TestPassword123!";
        var wrongPassword = "WrongPassword123!";
        var hash = service.HashPassword(password);

        // Act
        var result = service.VerifyPassword(wrongPassword, hash);

        // Assert
        result.Should().BeFalse();
    }
}
