using FluentAssertions;
using VillageClub.Membership.Models;
using VillageClub.Membership.Validators;
using Xunit;

namespace VillageClub.Membership.Tests.Validators;

/*
 * CONSTITUTION COMPLIANCE NOTE (v2.2.1 - Principle IV: Third-Party Components)
 * 
 * This test class focuses on OUR validator configuration, NOT FluentValidation's behavior.
 * 
 * TESTING PHILOSOPHY:
 * - ✅ Test: Required field configuration (email, password)
 * - ✅ Test: Business rule configuration (login accepts any non-empty password)
 * - ❌ DON'T Test: Email format validation (FluentValidation's EmailAddress validator behavior)
 * - ❌ DON'T Test: Whitespace handling (FluentValidation's NotEmpty validator behavior)
 * - ❌ DON'T Test: Null handling (FluentValidation's NotNull validator behavior)
 * 
 * WHY SIMPLIFIED (Phase 3 - 2025-10-19):
 * Per Constitution v2.2.1: "Validation frameworks (e.g., FluentValidation) - assume validation
 * rules execute correctly. Tests MUST test OUR configuration and setup of these libraries."
 * 
 * FluentValidation has comprehensive tests validating EmailAddress, NotEmpty, etc. work correctly.
 * We test that we CONFIGURED the right rules for our business requirements.
 * 
 * COVERAGE STRATEGY:
 * - Unit Level: OUR validator configuration (THIS FILE - 3 tests)
 * - Library Level: FluentValidation rule execution (FluentValidation's test suite)
 * - Integration Level: End-to-end validation (AuthFunctionsTests with real validators)
 * 
 * TESTS REMOVED (Phase 3):
 * - Validate_ShouldFail_WhenEmailIsNullOrWhitespace (3 inline data) - Tests FluentValidation's NotEmpty
 * - Validate_ShouldFail_WhenEmailFormatIsInvalid (4 inline data) - Tests FluentValidation's EmailAddress
 * - Validate_ShouldFail_WhenPasswordIsNullOrWhitespace (3 inline data) - Tests FluentValidation's NotEmpty
 * 
 * These scenarios ARE validated:
 * - In FluentValidation's test suite (library-level unit tests)
 * - In AuthFunctionsTests (integration tests with real validators)
 */

/// <summary>
/// Tests for LoginRequest validator configuration.
/// Validates OUR business rules, not FluentValidation's built-in validator behavior.
/// </summary>
public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    // ============================================================================
    // TEST 1: Valid Configuration (OUR BUSINESS RULE)
    // ============================================================================

    /// <summary>
    /// Validates that OUR validator accepts valid login requests with proper email and password.
    /// Tests OUR configuration of required fields.
    /// </summary>
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

        // Assert - Verify OUR validator configuration accepts valid inputs
        result.IsValid.Should().BeTrue("valid email and password should pass validation");
        result.Errors.Should().BeEmpty("no validation errors should be present for valid request");
    }

    // ============================================================================
    // TEST 2: Email Required Configuration (OUR BUSINESS RULE)
    // ============================================================================

    /// <summary>
    /// Validates that OUR validator requires email field.
    /// Tests OUR configuration that email is mandatory, not FluentValidation's implementation.
    /// </summary>
    [Fact]
    public void Validate_ShouldFail_WhenEmailIsMissing()
    {
        // Arrange - Test OUR requirement that email is mandatory
        var request = new LoginRequest
        {
            Email = null!,  // Missing required field
            Password = "Password123!",
        };

        // Act
        var result = _validator.Validate(request);

        // Assert - Verify OUR validator is configured to require email
        result.IsValid.Should().BeFalse("email is required per our business rules");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginRequest.Email),
            "validation error should indicate email field is the problem");
    }

    // ============================================================================
    // TEST 3: Password Required Configuration (OUR BUSINESS RULE)
    // ============================================================================

    /// <summary>
    /// Validates that OUR validator requires password field.
    /// Tests OUR configuration that password is mandatory, not FluentValidation's implementation.
    /// </summary>
    [Fact]
    public void Validate_ShouldFail_WhenPasswordIsMissing()
    {
        // Arrange - Test OUR requirement that password is mandatory
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = null!,  // Missing required field
        };

        // Act
        var result = _validator.Validate(request);

        // Assert - Verify OUR validator is configured to require password
        result.IsValid.Should().BeFalse("password is required per our business rules");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginRequest.Password),
            "validation error should indicate password field is the problem");
    }

    // ============================================================================
    // TEST 4: Login Password Policy (OUR BUSINESS RULE)
    // ============================================================================

    /// <summary>
    /// Validates OUR business rule that login accepts any non-empty password (no complexity check).
    /// This is different from registration which enforces password complexity.
    /// Tests OUR decision not to enforce complexity at login time.
    /// </summary>
    [Fact]
    public void Validate_ShouldPass_WhenPasswordIsAnyNonEmptyString()
    {
        // Arrange - Login doesn't enforce password complexity, just non-empty (OUR RULE)
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "weak",  // Weak password is OK for login
        };

        // Act
        var result = _validator.Validate(request);

        // Assert - Verify OUR policy: login accepts any non-empty password
        result.IsValid.Should().BeTrue(
            "login should accept any non-empty password (complexity checked only at registration)");
    }
}
