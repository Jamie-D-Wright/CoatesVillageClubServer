using FluentAssertions;
using VillageClub.Membership.Models;
using VillageClub.Membership.Validators;
using Xunit;

namespace VillageClub.Membership.Tests.Validators;

public class RefreshTokenRequestValidatorTests
{
    private readonly RefreshTokenRequestValidator _validator = new();

    [Fact]
    public void Validate_ShouldPass_WhenRefreshTokenIsProvided()
    {
        // Arrange
        var request = new RefreshTokenRequest
        {
            RefreshToken = "valid_refresh_token_string",
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
    public void Validate_ShouldFail_WhenRefreshTokenIsNullOrWhitespace(string refreshToken)
    {
        // Arrange
        var request = new RefreshTokenRequest
        {
            RefreshToken = refreshToken,
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RefreshTokenRequest.RefreshToken));
    }
}
