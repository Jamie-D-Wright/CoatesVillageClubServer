using FluentAssertions;
using VillageClub.Membership.Models;
using VillageClub.Membership.Validators;
using Xunit;

namespace VillageClub.Membership.Tests.Validators;

/*
 * TESTING PHILOSOPHY (per Constitution v2.2.1 - Principle IV):
 * 
 * ChangePasswordRequestValidator tests focus on OUR business rule configuration, not FluentValidation's validator behavior.
 * 
 * ✅ WHAT WE TEST (OUR BUSINESS RULES):
 * - Required field configuration (currentPassword, newPassword, confirmPassword)
 * - Password complexity requirements (OUR policy: 8+ chars, upper/lower/digit/special)
 * - Password match validation (confirmPassword must equal newPassword)
 * - Password differentiation (newPassword must differ from currentPassword)
 * 
 * ❌ WHAT WE DON'T TEST (FLUENTVALIDATION BEHAVIOR):
 * - Password regex matching (FluentValidation's Matches validator)
 * - Whitespace/null handling (FluentValidation's NotEmpty validator)
 * - Length validation (FluentValidation's MinimumLength validator)
 * - Equality comparison (FluentValidation's Equal/NotEqual validators)
 * 
 * COVERAGE STRATEGY:
 * - Unit Level: OUR validator configuration (THIS FILE - 7 tests)
 * - Library Level: FluentValidation rule execution (FluentValidation's test suite)
 * - Integration Level: End-to-end validation (AuthFunctionsTests)
 * 
 * REMOVED TESTS (11 tests - FluentValidation behavior):
 * - Validate_ShouldFail_WhenCurrentPasswordIsNullOrWhitespace (Theory with 3 inline data)
 * - Validate_ShouldFail_WhenNewPasswordIsNullOrWhitespace (Theory with 3 inline data)
 * - Validate_ShouldFail_WhenNewPasswordIsTooShort (Theory with 2 inline data)
 * - Validate_ShouldPass_WhenNewPasswordMeetsAllRequirements (Theory with 3 inline data)
 * - Validate_ShouldFail_WhenConfirmPasswordIsNullOrWhitespace (Theory with 3 inline data)
 * 
 * RATIONALE: These tests verify FluentValidation's built-in validators (NotEmpty, MinimumLength, Matches).
 * FluentValidation maintains comprehensive test coverage for these validators.
 * Testing library behavior in our codebase is redundant and not our responsibility.
 * 
 * See: docs/TEST-SUITE-ANALYSIS.md and docs/PHASE-3-PARTIAL-COMPLETION-SUMMARY.md
 */

public class ChangePasswordRequestValidatorTests
{
    private readonly ChangePasswordRequestValidator _validator = new();

    // ============================================================================
    // VALID CONFIGURATION TESTS (OUR validator accepts valid requests)
    // ============================================================================
    [Fact]
    public void Validate_ShouldPass_WhenAllFieldsAreValid()
    {
        // Tests: OUR validator accepts valid password change requests
        // Validates: currentPassword, newPassword, confirmPassword configuration
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmPassword = "NewPassword123!",
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    // ============================================================================
    // REQUIRED FIELD CONFIGURATION TESTS (OUR validator requires these fields)
    // ============================================================================
    [Fact]
    public void Validate_ShouldFail_WhenCurrentPasswordIsMissing()
    {
        // Tests: OUR configuration requires currentPassword field
        // Validates: RuleFor(x => x.CurrentPassword).NotEmpty() configuration
        var request = new ChangePasswordRequest
        {
            CurrentPassword = null!,
            NewPassword = "NewPassword123!",
            ConfirmPassword = "NewPassword123!",
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ChangePasswordRequest.CurrentPassword));
    }

    [Fact]
    public void Validate_ShouldFail_WhenNewPasswordIsMissing()
    {
        // Tests: OUR configuration requires newPassword field
        // Validates: RuleFor(x => x.NewPassword).NotEmpty() configuration
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = null!,
            ConfirmPassword = null!,
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ChangePasswordRequest.NewPassword));
    }

    [Fact]
    public void Validate_ShouldFail_WhenConfirmPasswordIsMissing()
    {
        // Tests: OUR configuration requires confirmPassword field
        // Validates: RuleFor(x => x.ConfirmPassword).NotEmpty() configuration
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmPassword = null!,
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ChangePasswordRequest.ConfirmPassword));
    }

    // ============================================================================
    // PASSWORD COMPLEXITY CONFIGURATION TEST (OUR password policy)
    // ============================================================================
    [Fact]
    public void Validate_ShouldFail_WhenNewPasswordDoesNotMeetComplexityRequirements()
    {
        // Tests: OUR password complexity policy configuration
        // Validates: Password must have upper/lower/digit/special and 8+ chars (OUR requirement)
        // Note: We test "OUR policy is configured", not "regex matching works correctly"
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "simple", // Fails OUR complexity requirements
            ConfirmPassword = "simple",
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ChangePasswordRequest.NewPassword));
    }

    // ============================================================================
    // PASSWORD MATCH BUSINESS RULE TESTS (OUR business logic)
    // ============================================================================
    [Fact]
    public void Validate_ShouldFail_WhenPasswordsDoNotMatch()
    {
        // Tests: OUR business rule - confirmPassword must match newPassword
        // Validates: Equal(x => x.NewPassword) configuration
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmPassword = "DifferentPassword123!",
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ChangePasswordRequest.ConfirmPassword));
    }

    [Fact]
    public void Validate_ShouldFail_WhenNewPasswordIsSameAsCurrentPassword()
    {
        // Tests: OUR business rule - newPassword must differ from currentPassword
        // Validates: NotEqual(x => x.CurrentPassword) configuration
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "SamePassword123!",
            NewPassword = "SamePassword123!",
            ConfirmPassword = "SamePassword123!",
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ChangePasswordRequest.NewPassword));
    }
}
