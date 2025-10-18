using FluentAssertions;
using VillageClub.Contracts.Enums;
using VillageClub.Membership.Models;
using VillageClub.Membership.Validators;
using Xunit;

namespace VillageClub.Membership.Tests.Validators;

public class CreateUserRequestValidatorTests
{
    private readonly CreateUserRequestValidator _validator = new();

    [Fact]
    public void Validate_ShouldPass_WhenAllRequiredFieldsAreValidForVolunteer()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Email = "test@example.com",
            Password = "Password123!",
            FirstName = "John",
            LastName = "Doe",
            Role = UserRole.Volunteer,
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ShouldPass_WhenCommitteeRoleIsProvidedForCommitteeMember()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Email = "test@example.com",
            Password = "Password123!",
            FirstName = "John",
            LastName = "Doe",
            Role = UserRole.Committee,
            CommitteeRole = CommitteeRole.Treasurer,
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenCommitteeRoleIsNullForCommitteeMember()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Email = "test@example.com",
            Password = "Password123!",
            FirstName = "John",
            LastName = "Doe",
            Role = UserRole.Committee,
            CommitteeRole = null,
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUserRequest.CommitteeRole));
    }

    [Theory]
    [InlineData(UserRole.Member)]
    [InlineData(UserRole.Volunteer)]
    public void Validate_ShouldPass_WhenCommitteeRoleIsNullForNonCommitteeMember(UserRole role)
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Email = "test@example.com",
            Password = "Password123!",
            FirstName = "John",
            LastName = "Doe",
            Role = role,
            CommitteeRole = null,
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldFail_WhenEmailIsNullOrWhitespace(string email)
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Email = email,
            Password = "Password123!",
            FirstName = "John",
            LastName = "Doe",
            Role = UserRole.Member,
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUserRequest.Email));
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing@domain")]
    [InlineData("@nodomain.com")]
    public void Validate_ShouldFail_WhenEmailFormatIsInvalid(string email)
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Email = email,
            Password = "Password123!",
            FirstName = "John",
            LastName = "Doe",
            Role = UserRole.Member,
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUserRequest.Email));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldFail_WhenPasswordIsNullOrWhitespace(string password)
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Email = "test@example.com",
            Password = password,
            FirstName = "John",
            LastName = "Doe",
            Role = UserRole.Member,
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUserRequest.Password));
    }

    [Theory]
    [InlineData("short")]
    [InlineData("1234567")]
    public void Validate_ShouldFail_WhenPasswordIsTooShort(string password)
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Email = "test@example.com",
            Password = password,
            FirstName = "John",
            LastName = "Doe",
            Role = UserRole.Member,
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUserRequest.Password));
    }

    [Theory]
    [InlineData("NoDigitsHere!")]
    [InlineData("ALLUPPER123")]
    [InlineData("alllower123")]
    [InlineData("NoSpecialChar1")]
    public void Validate_ShouldFail_WhenPasswordDoesNotMeetComplexityRequirements(string password)
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Email = "test@example.com",
            Password = password,
            FirstName = "John",
            LastName = "Doe",
            Role = UserRole.Member,
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUserRequest.Password));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldFail_WhenFirstNameIsNullOrWhitespace(string firstName)
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Email = "test@example.com",
            Password = "Password123!",
            FirstName = firstName,
            LastName = "Doe",
            Role = UserRole.Member,
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUserRequest.FirstName));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldFail_WhenLastNameIsNullOrWhitespace(string lastName)
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Email = "test@example.com",
            Password = "Password123!",
            FirstName = "John",
            LastName = lastName,
            Role = UserRole.Member,
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUserRequest.LastName));
    }

    [Fact]
    public void Validate_ShouldFail_WhenRoleIsInvalid()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Email = "test@example.com",
            Password = "Password123!",
            FirstName = "John",
            LastName = "Doe",
            Role = (UserRole)999, // Invalid enum value
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUserRequest.Role));
    }

    [Theory]
    [InlineData(UserRole.Member)]
    [InlineData(UserRole.Volunteer)]
    [InlineData(UserRole.Committee)]
    public void Validate_ShouldPass_WhenRoleIsValid(UserRole role)
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Email = "test@example.com",
            Password = "Password123!",
            FirstName = "John",
            LastName = "Doe",
            Role = role,
            CommitteeRole = role == UserRole.Committee ? CommitteeRole.Treasurer : null,
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
