using FluentAssertions;
using VillageClub.Events.Core.Models;
using VillageClub.Events.Core.Services;
using Xunit;

namespace VillageClub.Events.Core.Tests.Services;

/// <summary>
/// Tests for event state transition business logic.
/// </summary>
public class EventBusinessLogicTests
{
    [Fact]
    public void CanPublish_WhenDraft_ReturnsTrue()
    {
        // Arrange
        var eventStatus = EventStatus.Draft;
        var endDateTime = DateTime.UtcNow.AddDays(1);

        // Act
        var result = EventBusinessLogic.CanPublish(eventStatus, endDateTime);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanPublish_WhenPublished_ReturnsFalse()
    {
        // Arrange
        var eventStatus = EventStatus.Published;
        var endDateTime = DateTime.UtcNow.AddDays(1);

        // Act
        var result = EventBusinessLogic.CanPublish(eventStatus, endDateTime);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanPublish_WhenCancelled_ReturnsFalse()
    {
        // Arrange
        var eventStatus = EventStatus.Cancelled;
        var endDateTime = DateTime.UtcNow.AddDays(1);

        // Act
        var result = EventBusinessLogic.CanPublish(eventStatus, endDateTime);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanPublish_WhenCompleted_ReturnsFalse()
    {
        // Arrange
        var eventStatus = EventStatus.Completed;
        var endDateTime = DateTime.UtcNow.AddDays(1);

        // Act
        var result = EventBusinessLogic.CanPublish(eventStatus, endDateTime);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanPublish_WhenEndDateInPast_ReturnsFalse()
    {
        // Arrange
        var eventStatus = EventStatus.Draft;
        var endDateTime = DateTime.UtcNow.AddDays(-1);

        // Act
        var result = EventBusinessLogic.CanPublish(eventStatus, endDateTime);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanComplete_WhenPublished_ReturnsTrue()
    {
        // Arrange
        var eventStatus = EventStatus.Published;

        // Act
        var result = EventBusinessLogic.CanComplete(eventStatus);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanComplete_WhenDraft_ReturnsFalse()
    {
        // Arrange
        var eventStatus = EventStatus.Draft;

        // Act
        var result = EventBusinessLogic.CanComplete(eventStatus);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanComplete_WhenCancelled_ReturnsFalse()
    {
        // Arrange
        var eventStatus = EventStatus.Cancelled;

        // Act
        var result = EventBusinessLogic.CanComplete(eventStatus);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanComplete_WhenCompleted_ReturnsFalse()
    {
        // Arrange
        var eventStatus = EventStatus.Completed;

        // Act
        var result = EventBusinessLogic.CanComplete(eventStatus);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanCancel_WhenDraft_ReturnsTrue()
    {
        // Arrange
        var eventStatus = EventStatus.Draft;

        // Act
        var result = EventBusinessLogic.CanCancel(eventStatus);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanCancel_WhenPublished_ReturnsTrue()
    {
        // Arrange
        var eventStatus = EventStatus.Published;

        // Act
        var result = EventBusinessLogic.CanCancel(eventStatus);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanCancel_WhenCancelled_ReturnsFalse()
    {
        // Arrange
        var eventStatus = EventStatus.Cancelled;

        // Act
        var result = EventBusinessLogic.CanCancel(eventStatus);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanCancel_WhenCompleted_ReturnsFalse()
    {
        // Arrange
        var eventStatus = EventStatus.Completed;

        // Act
        var result = EventBusinessLogic.CanCancel(eventStatus);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateStateTransition_DraftToPublished_ReturnsSuccess()
    {
        // Arrange
        var currentStatus = EventStatus.Draft;
        var newStatus = EventStatus.Published;
        var endDateTime = DateTime.UtcNow.AddDays(1);

        // Act
        var result = EventBusinessLogic.ValidateStateTransition(currentStatus, newStatus, endDateTime);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ValidateStateTransition_PublishedToCompleted_ReturnsSuccess()
    {
        // Arrange
        var currentStatus = EventStatus.Published;
        var newStatus = EventStatus.Completed;
        var endDateTime = DateTime.UtcNow.AddDays(1);

        // Act
        var result = EventBusinessLogic.ValidateStateTransition(currentStatus, newStatus, endDateTime);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ValidateStateTransition_DraftToCancelled_ReturnsSuccess()
    {
        // Arrange
        var currentStatus = EventStatus.Draft;
        var newStatus = EventStatus.Cancelled;
        var endDateTime = DateTime.UtcNow.AddDays(1);

        // Act
        var result = EventBusinessLogic.ValidateStateTransition(currentStatus, newStatus, endDateTime);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ValidateStateTransition_PublishedToCancelled_ReturnsSuccess()
    {
        // Arrange
        var currentStatus = EventStatus.Published;
        var newStatus = EventStatus.Cancelled;
        var endDateTime = DateTime.UtcNow.AddDays(1);

        // Act
        var result = EventBusinessLogic.ValidateStateTransition(currentStatus, newStatus, endDateTime);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ValidateStateTransition_DraftToCompleted_ReturnsFailure()
    {
        // Arrange
        var currentStatus = EventStatus.Draft;
        var newStatus = EventStatus.Completed;
        var endDateTime = DateTime.UtcNow.AddDays(1);

        // Act
        var result = EventBusinessLogic.ValidateStateTransition(currentStatus, newStatus, endDateTime);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Draft");
        result.ErrorMessage.Should().Contain("Completed");
    }

    [Fact]
    public void ValidateStateTransition_PublishedToDraft_ReturnsFailure()
    {
        // Arrange
        var currentStatus = EventStatus.Published;
        var newStatus = EventStatus.Draft;
        var endDateTime = DateTime.UtcNow.AddDays(1);

        // Act
        var result = EventBusinessLogic.ValidateStateTransition(currentStatus, newStatus, endDateTime);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Published");
        result.ErrorMessage.Should().Contain("Draft");
    }

    [Fact]
    public void ValidateStateTransition_CancelledToPublished_ReturnsFailure()
    {
        // Arrange
        var currentStatus = EventStatus.Cancelled;
        var newStatus = EventStatus.Published;
        var endDateTime = DateTime.UtcNow.AddDays(1);

        // Act
        var result = EventBusinessLogic.ValidateStateTransition(currentStatus, newStatus, endDateTime);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Cancelled");
    }

    [Fact]
    public void ValidateStateTransition_CompletedToPublished_ReturnsFailure()
    {
        // Arrange
        var currentStatus = EventStatus.Completed;
        var newStatus = EventStatus.Published;
        var endDateTime = DateTime.UtcNow.AddDays(1);

        // Act
        var result = EventBusinessLogic.ValidateStateTransition(currentStatus, newStatus, endDateTime);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Completed");
    }

    [Fact]
    public void ValidateStateTransition_DraftToPublishedWithPastEndDate_ReturnsFailure()
    {
        // Arrange
        var currentStatus = EventStatus.Draft;
        var newStatus = EventStatus.Published;
        var endDateTime = DateTime.UtcNow.AddDays(-1);

        // Act
        var result = EventBusinessLogic.ValidateStateTransition(currentStatus, newStatus, endDateTime);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("past");
    }
}
