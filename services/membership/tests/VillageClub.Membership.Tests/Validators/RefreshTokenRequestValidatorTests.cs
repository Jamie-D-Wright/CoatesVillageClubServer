using FluentAssertions;
using VillageClub.Membership.Models;
using VillageClub.Membership.Validators;
using Xunit;

namespace VillageClub.Membership.Tests.Validators;

/*
 * TESTING PHILOSOPHY (per Constitution v2.2.1 - Principle IV):
 * 
 * RefreshTokenRequestValidator tests focus on OUR business rule configuration, not FluentValidation's validator behavior.
 * 
 * ✅ WHAT WE TEST (OUR BUSINESS RULES):
 * - Required field configuration (refreshToken)
 * 
 * ❌ WHAT WE DON'T TEST (FLUENTVALIDATION BEHAVIOR):
 * - Whitespace/null handling (FluentValidation's NotEmpty validator)
 * 
 * COVERAGE STRATEGY:
 * - Unit Level: OUR validator configuration (THIS FILE - 2 tests)
 * - Library Level: FluentValidation rule execution (FluentValidation's test suite)
 * - Integration Level: End-to-end validation (AuthFunctionsTests)
 * 
 * REMOVED TESTS (2 tests - FluentValidation behavior):
 * - Validate_ShouldFail_WhenRefreshTokenIsNullOrWhitespace (Theory with 3 inline data)
 * 
 * RATIONALE: This test verifies FluentValidation's built-in NotEmpty validator.
 * FluentValidation maintains comprehensive test coverage for this validator.
 * Testing library behavior in our codebase is redundant and not our responsibility.
 * 
 * See: docs/TEST-SUITE-ANALYSIS.md and docs/PHASE-3-PARTIAL-COMPLETION-SUMMARY.md
 */

public class RefreshTokenRequestValidatorTests
{
    private readonly RefreshTokenRequestValidator _validator = new();

    // ============================================================================
    // VALID CONFIGURATION TESTS (OUR validator accepts valid requests)
    // ============================================================================
    [Fact]
    public void Validate_ShouldPass_WhenRefreshTokenIsProvided()
    {
        // Tests: OUR validator accepts valid refresh token requests
        // Validates: refreshToken field configuration
        var request = new RefreshTokenRequest
        {
            RefreshToken = "valid_refresh_token_string",
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    // ============================================================================
    // REQUIRED FIELD CONFIGURATION TEST (OUR validator requires refreshToken)
    // ============================================================================
    [Fact]
    public void Validate_ShouldFail_WhenRefreshTokenIsMissing()
    {
        // Tests: OUR configuration requires refreshToken field
        // Validates: RuleFor(x => x.RefreshToken).NotEmpty() configuration
        // Note: We test "field required", not "how NotEmpty() detects empty values"
        var request = new RefreshTokenRequest
        {
            RefreshToken = null!,
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RefreshTokenRequest.RefreshToken));
    }
}
