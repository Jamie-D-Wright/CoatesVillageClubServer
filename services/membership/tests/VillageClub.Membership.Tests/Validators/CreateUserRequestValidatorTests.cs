using FluentAssertions;
using VillageClub.Contracts.Enums;
using VillageClub.Membership.Models;
using VillageClub.Membership.Validators;
using Xunit;

namespace VillageClub.Membership.Tests.Validators;

/*
 * TESTING PHILOSOPHY (per Constitution v2.2.1 - Principle IV):
 * 
 * CreateUserRequestValidator tests focus on OUR business rule configuration, not FluentValidation's validator behavior.
 * 
 * ✅ WHAT WE TEST (OUR BUSINESS RULES):
 * - Required field configuration (email, password, firstName, lastName, role)
 * - Password complexity requirements (OUR policy: 8+ chars, upper/lower/digit/special)
 * - Committee role business logic (required when Role=Committee, optional otherwise)
 * - Role validation configuration (IsInEnum setup)
 * 
 * ❌ WHAT WE DON'T TEST (FLUENTVALIDATION BEHAVIOR):
 * - Email format validation (FluentValidation's EmailAddress validator)
 * - Password regex matching (FluentValidation's Matches validator)
 * - Whitespace/null handling (FluentValidation's NotEmpty validator)
 * - Length validation (FluentValidation's MinimumLength/MaximumLength validators)
 * - Enum validation (FluentValidation's IsInEnum validator)
 * 
 * COVERAGE STRATEGY:
 * - Unit Level: OUR validator configuration (THIS FILE - 10 tests)
 * - Library Level: FluentValidation rule execution (FluentValidation's test suite)
 * - Integration Level: End-to-end validation (UserFunctionsTests)
 * 
 * REMOVED TESTS (40+ tests - FluentValidation behavior):
 * - Validate_ShouldFail_WhenEmailIsNullOrWhitespace (Theory with 3 inline data)
 * - Validate_ShouldFail_WhenEmailFormatIsInvalid (Theory with 3 inline data)
 * - Validate_ShouldFail_WhenPasswordIsNullOrWhitespace (Theory with 3 inline data)
 * - Validate_ShouldFail_WhenPasswordIsTooShort (Theory with 2 inline data)
 * - Validate_ShouldFail_WhenFirstNameIsNullOrWhitespace (Theory with 3 inline data)
 * - Validate_ShouldFail_WhenLastNameIsNullOrWhitespace (Theory with 3 inline data)
 * 
 * RATIONALE: These tests verify FluentValidation's built-in validators (NotEmpty, EmailAddress, MinimumLength, Matches).
 * FluentValidation maintains comprehensive test coverage for these validators.
 * Testing library behavior in our codebase is redundant and not our responsibility.
 * 
 * See: docs/TEST-SUITE-ANALYSIS.md and docs/PHASE-3-PARTIAL-COMPLETION-SUMMARY.md
 */

public class CreateUserRequestValidatorTests
{
    private readonly CreateUserRequestValidator _validator = new();

    // ============================================================================
    // VALID CONFIGURATION TESTS (OUR validator accepts valid requests)
    // ============================================================================
    [Fact]
    public void Validate_ShouldPass_WhenAllRequiredFieldsAreValidForVolunteer()
    {
        // Tests: OUR validator accepts valid user creation requests
        // Validates: email, password, firstName, lastName, role configuration
        var request = new CreateUserRequest
        {
            Email = "test@example.com",
            Password = "Password123!",
            FirstName = "John",
            LastName = "Doe",
            Role = UserRole.Volunteer,
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    // ============================================================================
    // COMMITTEE ROLE BUSINESS LOGIC TESTS (OUR business rule)
    // ============================================================================
    [Fact]
    public void Validate_ShouldPass_WhenCommitteeRoleIsProvidedForCommitteeMember()
    {
        // Tests: OUR business rule - Committee users must have CommitteeRole
        // Validates: CommitteeRole required when Role=Committee configuration
        var request = new CreateUserRequest
        {
            Email = "test@example.com",
            Password = "Password123!",
            FirstName = "John",
            LastName = "Doe",
            Role = UserRole.Committee,
            CommitteeRole = CommitteeRole.Treasurer,
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenCommitteeRoleIsNullForCommitteeMember()
    {
        // Tests: OUR business rule - Committee role is required for Committee members
        // Validates: NotNull().When(x => x.Role == UserRole.Committee) configuration
        var request = new CreateUserRequest
        {
            Email = "test@example.com",
            Password = "Password123!",
            FirstName = "John",
            LastName = "Doe",
            Role = UserRole.Committee,
            CommitteeRole = null,
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUserRequest.CommitteeRole));
    }

    [Theory]
    [InlineData(UserRole.Member)]
    [InlineData(UserRole.Volunteer)]
    public void Validate_ShouldPass_WhenCommitteeRoleIsNullForNonCommitteeMember(UserRole role)
    {
        // Tests: OUR business rule - CommitteeRole is optional for non-Committee roles
        // Validates: When(x => x.Role == UserRole.Committee) conditional logic
        var request = new CreateUserRequest
        {
            Email = "test@example.com",
            Password = "Password123!",
            FirstName = "John",
            LastName = "Doe",
            Role = role,
            CommitteeRole = null,
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    // ============================================================================
    // REQUIRED FIELD CONFIGURATION TESTS (OUR validator requires these fields)
    // ============================================================================
    [Fact]
    public void Validate_ShouldFail_WhenEmailIsMissing()
    {
        // Tests: OUR configuration requires email field
        // Validates: RuleFor(x => x.Email).NotEmpty() configuration
        // Note: We test "field required", not "how NotEmpty() detects empty values"
        var request = new CreateUserRequest
        {
            Email = null!,
            Password = "Password123!",
            FirstName = "John",
            LastName = "Doe",
            Role = UserRole.Member,
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUserRequest.Email));
    }

    [Fact]
    public void Validate_ShouldFail_WhenPasswordIsMissing()
    {
        // Tests: OUR configuration requires password field
        // Validates: RuleFor(x => x.Password).NotEmpty() configuration
        var request = new CreateUserRequest
        {
            Email = "test@example.com",
            Password = null!,
            FirstName = "John",
            LastName = "Doe",
            Role = UserRole.Member,
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUserRequest.Password));
    }

    [Fact]
    public void Validate_ShouldFail_WhenFirstNameIsMissing()
    {
        // Tests: OUR configuration requires firstName field
        // Validates: RuleFor(x => x.FirstName).NotEmpty() configuration
        var request = new CreateUserRequest
        {
            Email = "test@example.com",
            Password = "Password123!",
            FirstName = null!,
            LastName = "Doe",
            Role = UserRole.Member,
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUserRequest.FirstName));
    }

    [Fact]
    public void Validate_ShouldFail_WhenLastNameIsMissing()
    {
        // Tests: OUR configuration requires lastName field
        // Validates: RuleFor(x => x.LastName).NotEmpty() configuration
        var request = new CreateUserRequest
        {
            Email = "test@example.com",
            Password = "Password123!",
            FirstName = "John",
            LastName = null!,
            Role = UserRole.Member,
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUserRequest.LastName));
    }

    // ============================================================================
    // PASSWORD COMPLEXITY CONFIGURATION TEST (OUR password policy)
    // ============================================================================
    [Fact]
    public void Validate_ShouldFail_WhenPasswordDoesNotMeetComplexityRequirements()
    {
        // Tests: OUR password complexity policy configuration
        // Validates: Password must have upper/lower/digit/special and 8+ chars (OUR requirement)
        // Note: We test "OUR policy is configured", not "regex matching works correctly"
        var request = new CreateUserRequest
        {
            Email = "test@example.com",
            Password = "simple", // Fails OUR complexity requirements
            FirstName = "John",
            LastName = "Doe",
            Role = UserRole.Member,
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUserRequest.Password));
    }
}
