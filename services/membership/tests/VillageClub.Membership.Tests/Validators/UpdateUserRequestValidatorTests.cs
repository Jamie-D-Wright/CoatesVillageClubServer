using FluentAssertions;
using VillageClub.Membership.Models;
using VillageClub.Membership.Validators;
using Xunit;

namespace VillageClub.Membership.Tests.Validators;

public class UpdateUserRequestValidatorTests
{
    private readonly UpdateUserRequestValidator _validator = new();

    [Fact]
    public void Validate_ShouldPass_WhenAllFieldsAreValid()
    {
        // Arrange
        var request = new UpdateUserRequest
        {
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "01234567890",
            Address = "123 Test St",
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ShouldPass_WhenAllFieldsAreNull()
    {
        // Arrange - UpdateUserRequest allows all fields to be optional
        var request = new UpdateUserRequest
        {
            FirstName = null,
            LastName = null,
            PhoneNumber = null,
            Address = null,
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldPass_WhenOnlySomeFieldsAreProvided()
    {
        // Arrange
        var request = new UpdateUserRequest
        {
            FirstName = "John",
            LastName = null,
            PhoneNumber = null,
            Address = null,
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldFail_WhenFirstNameIsEmptyOrWhitespace(string firstName)
    {
        // Arrange - Empty/whitespace not allowed, but null is OK (means no update)
        var request = new UpdateUserRequest
        {
            FirstName = firstName,
            LastName = "Doe",
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateUserRequest.FirstName));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldFail_WhenLastNameIsEmptyOrWhitespace(string lastName)
    {
        // Arrange
        var request = new UpdateUserRequest
        {
            FirstName = "John",
            LastName = lastName,
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateUserRequest.LastName));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldFail_WhenPhoneNumberIsEmptyOrWhitespace(string phoneNumber)
    {
        // Arrange
        var request = new UpdateUserRequest
        {
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = phoneNumber,
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateUserRequest.PhoneNumber));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldFail_WhenAddressIsEmptyOrWhitespace(string address)
    {
        // Arrange
        var request = new UpdateUserRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Address = address,
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateUserRequest.Address));
    }

    [Fact]
    public void Validate_ShouldPass_WhenMaxLengthsAreRespected()
    {
        // Arrange
        var request = new UpdateUserRequest
        {
            FirstName = new string('A', 100), // Max length
            LastName = new string('B', 100),  // Max length
            PhoneNumber = new string('1', 20), // Max length
            Address = new string('C', 500),   // Max length
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenFirstNameExceedsMaxLength()
    {
        // Arrange
        var request = new UpdateUserRequest
        {
            FirstName = new string('A', 101), // Exceeds max length
            LastName = "Doe",
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateUserRequest.FirstName));
    }

    [Fact]
    public void Validate_ShouldFail_WhenLastNameExceedsMaxLength()
    {
        // Arrange
        var request = new UpdateUserRequest
        {
            FirstName = "John",
            LastName = new string('B', 101), // Exceeds max length
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateUserRequest.LastName));
    }

    [Fact]
    public void Validate_ShouldFail_WhenPhoneNumberExceedsMaxLength()
    {
        // Arrange
        var request = new UpdateUserRequest
        {
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = new string('1', 21), // Exceeds max length
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateUserRequest.PhoneNumber));
    }

    [Fact]
    public void Validate_ShouldFail_WhenAddressExceedsMaxLength()
    {
        // Arrange
        var request = new UpdateUserRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Address = new string('C', 501), // Exceeds max length
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateUserRequest.Address));
    }
}
