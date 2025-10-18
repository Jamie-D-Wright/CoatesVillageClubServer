using FluentAssertions;
using VillageClub.Membership.Services;
using Xunit;

namespace VillageClub.Membership.Tests.Services;

public class PasswordHashServiceTests
{
    private readonly PasswordHashService _sut;

    public PasswordHashServiceTests()
    {
        _sut = new PasswordHashService();
    }

    [Fact]
    public void HashPassword_ShouldReturnHashedPassword()
    {
        // Arrange
        var password = "SecurePassword123!";

        // Act
        var hashedPassword = _sut.HashPassword(password);

        // Assert
        hashedPassword.Should().NotBeNullOrEmpty();
        hashedPassword.Should().NotBe(password);
        hashedPassword.Should().StartWith("$2a$"); // BCrypt hash prefix
    }

    [Fact]
    public void HashPassword_ShouldGenerateDifferentHashesForSamePassword()
    {
        // Arrange
        var password = "SecurePassword123!";

        // Act
        var hash1 = _sut.HashPassword(password);
        var hash2 = _sut.HashPassword(password);

        // Assert
        hash1.Should().NotBe(hash2, "BCrypt uses salt, so same password should produce different hashes");
    }

    [Fact]
    public void VerifyPassword_ShouldReturnTrue_WhenPasswordMatches()
    {
        // Arrange
        var password = "SecurePassword123!";
        var hashedPassword = _sut.HashPassword(password);

        // Act
        var result = _sut.VerifyPassword(password, hashedPassword);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_ShouldReturnFalse_WhenPasswordDoesNotMatch()
    {
        // Arrange
        var correctPassword = "SecurePassword123!";
        var incorrectPassword = "WrongPassword456!";
        var hashedPassword = _sut.HashPassword(correctPassword);

        // Act
        var result = _sut.VerifyPassword(incorrectPassword, hashedPassword);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_ShouldReturnFalse_WhenHashIsInvalid()
    {
        // Arrange
        var password = "SecurePassword123!";
        var invalidHash = "invalid-hash-format";

        // Act
        var result = _sut.VerifyPassword(password, invalidHash);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void HashPassword_ShouldThrowArgumentException_WhenPasswordIsNullOrEmpty(string? password)
    {
        // Act
        var action = () => _sut.HashPassword(password!);

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("Password cannot be null or empty*");
    }

    [Fact]
    public void VerifyPassword_ShouldReturnFalse_WhenPasswordIsNull()
    {
        // Arrange
        var hashedPassword = _sut.HashPassword("SomePassword123!");

        // Act
        var result = _sut.VerifyPassword(null!, hashedPassword);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_ShouldReturnFalse_WhenHashIsNull()
    {
        // Arrange
        var password = "SecurePassword123!";

        // Act
        var result = _sut.VerifyPassword(password, null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HashPassword_ShouldProduceHashWithCorrectWorkFactor()
    {
        // Arrange
        var password = "SecurePassword123!";

        // Act
        var hashedPassword = _sut.HashPassword(password);

        // Assert - BCrypt format: $2a$12$... (12 is the work factor)
        hashedPassword.Should().MatchRegex(@"^\$2a\$12\$");
    }
}
