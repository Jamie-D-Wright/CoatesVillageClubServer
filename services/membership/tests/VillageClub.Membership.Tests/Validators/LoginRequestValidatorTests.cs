using FluentAssertions;
using VillageClub.Contracts.Enums;
using VillageClub.Membership.Models;
using VillageClub.Membership.Validators;
using Xunit;

namespace VillageClub.Membership.Tests.Validators;

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    [Fact]
    public void Validate_ShouldPass_WhenAllFieldsAreValid()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "Password123!",
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldFail_WhenEmailIsNullOrWhitespace(string email)
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = email,
            Password = "Password123!",
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginRequest.Email));
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing@domain")]
    [InlineData("@nodomain.com")]
    [InlineData("spaces in@email.com")]
    public void Validate_ShouldFail_WhenEmailFormatIsInvalid(string email)
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = email,
            Password = "Password123!",
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginRequest.Email));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldFail_WhenPasswordIsNullOrWhitespace(string password)
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = password,
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginRequest.Password));
    }

    [Fact]
    public void Validate_ShouldPass_WhenPasswordIsAnyNonEmptyString()
    {
        // Arrange - Login doesn't enforce password complexity, just non-empty
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "weak",
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
