using FluentAssertions;
using VillageClub.Events.Core.Models;
using VillageClub.Events.Core.Validators;
using Xunit;

namespace VillageClub.Events.Core.Tests.Validators;

/// <summary>
/// Tests for CreateEventRequestValidator.
/// Following TDD: These tests are written BEFORE the validator implementation.
/// Expected: All tests will FAIL initially (RED phase).
/// </summary>
public class CreateEventRequestValidatorTests
{
    private readonly CreateEventRequestValidator _validator = new();

    [Fact]
    public void Should_Pass_When_All_Fields_Are_Valid()
    {
        // Arrange
        var request = new CreateEventRequest
        {
            Title = "Quiz Night",
            Description = "Monthly quiz fundraiser",
            EventType = EventType.SpecialEvent,
            StartDateTime = DateTime.UtcNow.AddDays(7),
            EndDateTime = DateTime.UtcNow.AddDays(7).AddHours(3),
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Should_Fail_When_Title_Is_Empty_Or_Null(string? title)
    {
        // Arrange
        var request = new CreateEventRequest
        {
            Title = title!,
            EventType = EventType.SpecialEvent,
            StartDateTime = DateTime.UtcNow.AddDays(7),
            EndDateTime = DateTime.UtcNow.AddDays(7).AddHours(3),
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateEventRequest.Title));
    }

    [Theory]
    [InlineData("Ab")]           // Too short (< 3 chars)
    public void Should_Fail_When_Title_Is_Too_Short(string title)
    {
        // Arrange
        var request = new CreateEventRequest
        {
            Title = title,
            EventType = EventType.SpecialEvent,
            StartDateTime = DateTime.UtcNow.AddDays(7),
            EndDateTime = DateTime.UtcNow.AddDays(7).AddHours(3),
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateEventRequest.Title));
    }

    [Fact]
    public void Should_Fail_When_Title_Is_Too_Long()
    {
        // Arrange
        var request = new CreateEventRequest
        {
            Title = new string('A', 201), // > 200 chars
            EventType = EventType.SpecialEvent,
            StartDateTime = DateTime.UtcNow.AddDays(7),
            EndDateTime = DateTime.UtcNow.AddDays(7).AddHours(3),
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateEventRequest.Title));
    }

    [Fact]
    public void Should_Fail_When_StartDateTime_Is_In_The_Past()
    {
        // Arrange
        var request = new CreateEventRequest
        {
            Title = "Past Event",
            EventType = EventType.SpecialEvent,
            StartDateTime = DateTime.UtcNow.AddDays(-1),
            EndDateTime = DateTime.UtcNow.AddHours(2),
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateEventRequest.StartDateTime));
    }

    [Fact]
    public void Should_Fail_When_EndDateTime_Is_Before_StartDateTime()
    {
        // Arrange
        var request = new CreateEventRequest
        {
            Title = "Invalid Event",
            EventType = EventType.SpecialEvent,
            StartDateTime = DateTime.UtcNow.AddDays(7),
            EndDateTime = DateTime.UtcNow.AddDays(7).AddHours(-1), // Before start
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateEventRequest.EndDateTime));
    }

    [Fact]
    public void Should_Fail_When_Duration_Is_Too_Short()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddDays(7);
        var request = new CreateEventRequest
        {
            Title = "Short Event",
            EventType = EventType.SpecialEvent,
            StartDateTime = startTime,
            EndDateTime = startTime.AddMinutes(15), // < 30 minutes
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_Duration_Is_Too_Long()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddDays(7);
        var request = new CreateEventRequest
        {
            Title = "Long Event",
            EventType = EventType.SpecialEvent,
            StartDateTime = startTime,
            EndDateTime = startTime.AddHours(13), // > 12 hours
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(0.5)]  // 30 minutes (minimum)
    [InlineData(1)]    // 1 hour
    [InlineData(3)]    // 3 hours
    [InlineData(12)]   // 12 hours (maximum)
    public void Should_Pass_When_Duration_Is_Valid(double hours)
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddDays(7);
        var request = new CreateEventRequest
        {
            Title = "Valid Event",
            EventType = EventType.SpecialEvent,
            StartDateTime = startTime,
            EndDateTime = startTime.AddHours(hours),
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
