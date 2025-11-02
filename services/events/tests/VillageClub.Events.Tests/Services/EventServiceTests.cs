using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VillageClub.Events.Core.Models;
using VillageClub.Events.Data;
using VillageClub.Events.Data.Entities;
using VillageClub.Events.Services;
using Xunit;

namespace VillageClub.Events.Tests.Services;

/// <summary>
/// Tests for EventService that orchestrates Events.Core library with database operations.
/// Tests use in-memory SQLite database for realistic testing per Constitution.
/// </summary>
public class EventServiceTests : IDisposable
{
    private readonly EventsDbContext _context;
    private readonly EventService _service;
    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly SqliteConnection _connection; // Store connection reference

    public EventServiceTests()
    {
        // Use in-memory SQLite for realistic database testing
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open(); // Must open before creating context

        var options = new DbContextOptionsBuilder<EventsDbContext>()
            .UseSqlite(_connection)
            .EnableSensitiveDataLogging() // Helpful for debugging test failures
            .Options;

        _context = new EventsDbContext(options);
        
        // Don't use migrations for SQLite tests - SQL Server migration syntax incompatible
        // (NVARCHAR(MAX), GETUTCDATE(), schemas). Let EF create schema from model.
        _context.Database.EnsureCreated();

        _service = new EventService(_context);
    }

    public void Dispose()
    {
        _context?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WhenEventExists_ReturnsEventDto()
    {
        // Arrange
        var eventEntity = CreateTestEvent("Test Event");
        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByIdAsync(eventEntity.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(eventEntity.Id);
        result.Title.Should().Be("Test Event");
        result.EventType.Should().Be(EventType.SpecialEvent);
        result.Status.Should().Be(EventStatus.Draft);
    }

    [Fact]
    public async Task GetByIdAsync_WhenEventDoesNotExist_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _service.GetByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WithNoFilters_ReturnsAllEvents()
    {
        // Arrange
        var event1 = CreateTestEvent("Event 1", EventType.SpecialEvent);
        var event2 = CreateTestEvent("Event 2", EventType.RegularBarNight);
        var event3 = CreateTestEvent("Event 3", EventType.Fundraiser);

        _context.Events.AddRange(event1, event2, event3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllAsync(1, 50);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(3);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(50);
    }

    [Fact]
    public async Task GetAllAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        for (int i = 1; i <= 25; i++)
        {
            var eventEntity = CreateTestEvent($"Event {i}");
            _context.Events.Add(eventEntity);
        }

        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllAsync(2, 10);

        // Assert
        result.Items.Should().HaveCount(10);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(10);
        result.TotalCount.Should().Be(25);
        result.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task GetAllAsync_OrdersByStartDateDescending()
    {
        // Arrange
        var startDate = new DateTime(2025, 11, 1, 20, 0, 0, DateTimeKind.Utc);
        var event1 = CreateTestEvent("Event 1", startDateTime: startDate.AddDays(1));
        var event2 = CreateTestEvent("Event 2", startDateTime: startDate.AddDays(3));
        var event3 = CreateTestEvent("Event 3", startDateTime: startDate.AddDays(2));

        _context.Events.AddRange(event1, event2, event3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllAsync(1, 50);

        // Assert
        result.Items.Should().HaveCount(3);
        var itemsList = result.Items.ToList();
        itemsList[0].Title.Should().Be("Event 2"); // Latest first
        itemsList[1].Title.Should().Be("Event 3");
        itemsList[2].Title.Should().Be("Event 1");
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesEventInDatabase()
    {
        // Arrange
        var request = new CreateEventRequest
        {
            Title = "New Event",
            Description = "Test description",
            EventType = EventType.SpecialEvent,
            StartDateTime = DateTime.UtcNow.AddDays(7),
            EndDateTime = DateTime.UtcNow.AddDays(7).AddHours(3),
        };

        // Act
        var result = await _service.CreateAsync(request, _testUserId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.Title.Should().Be("New Event");
        result.Description.Should().Be("Test description");
        result.EventType.Should().Be(EventType.SpecialEvent);
        result.Status.Should().Be(EventStatus.Draft);
        result.CreatedById.Should().Be(_testUserId);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.PublishedAt.Should().BeNull();

        // Verify in database
        var dbEvent = await _context.Events.FindAsync(result.Id);
        dbEvent.Should().NotBeNull();
        dbEvent!.Title.Should().Be("New Event");
    }

    [Fact]
    public async Task CreateAsync_SetsStatusToDraft()
    {
        // Arrange
        var request = new CreateEventRequest
        {
            Title = "Draft Event",
            Description = "Test",
            EventType = EventType.RegularBarNight,
            StartDateTime = DateTime.UtcNow.AddDays(7),
            EndDateTime = DateTime.UtcNow.AddDays(7).AddHours(2),
        };

        // Act
        var result = await _service.CreateAsync(request, _testUserId);

        // Assert
        result.Status.Should().Be(EventStatus.Draft);
        result.PublishedAt.Should().BeNull();
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidRequest_UpdatesEvent()
    {
        // Arrange
        var eventEntity = CreateTestEvent("Original Title");
        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdateEventRequest
        {
            Title = "Updated Title",
            Description = "Updated Description",
            EventType = EventType.Fundraiser,
            StartDateTime = DateTime.UtcNow.AddDays(10),
            EndDateTime = DateTime.UtcNow.AddDays(10).AddHours(4),
        };

        // Act
        var result = await _service.UpdateAsync(eventEntity.Id, updateRequest);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Updated Title");
        result.Description.Should().Be("Updated Description");
        result.EventType.Should().Be(EventType.Fundraiser);
        result.UpdatedAt.Should().BeAfter(result.CreatedAt);

        // Verify in database
        var dbEvent = await _context.Events.FindAsync(eventEntity.Id);
        dbEvent!.Title.Should().Be("Updated Title");
    }

    [Fact]
    public async Task UpdateAsync_WithPartialRequest_UpdatesOnlyProvidedFields()
    {
        // Arrange
        var eventEntity = CreateTestEvent("Original Title", EventType.SpecialEvent);
        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync();

        var updateRequest = new UpdateEventRequest
        {
            Title = "Updated Title Only",
            // Other fields are null - should not be updated
        };

        // Act
        var result = await _service.UpdateAsync(eventEntity.Id, updateRequest);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Updated Title Only");
        result.EventType.Should().Be(EventType.SpecialEvent); // Unchanged
        result.Description.Should().Be("Test description"); // Unchanged
    }

    [Fact]
    public async Task UpdateAsync_WhenEventDoesNotExist_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var updateRequest = new UpdateEventRequest
        {
            Title = "Updated Title",
        };

        // Act
        var result = await _service.UpdateAsync(nonExistentId, updateRequest);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesUpdatedAtTimestamp()
    {
        // Arrange
        var eventEntity = CreateTestEvent("Original Title");
        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync();

        var originalUpdatedAt = eventEntity.UpdatedAt;
        await Task.Delay(100); // Ensure time difference

        var updateRequest = new UpdateEventRequest
        {
            Title = "Updated Title",
        };

        // Act
        var result = await _service.UpdateAsync(eventEntity.Id, updateRequest);

        // Assert
        result!.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WhenEventExists_DeletesEvent()
    {
        // Arrange
        var eventEntity = CreateTestEvent("Event to Delete");
        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteAsync(eventEntity.Id);

        // Assert
        result.Should().BeTrue();

        // Verify in database
        var dbEvent = await _context.Events.FindAsync(eventEntity.Id);
        dbEvent.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WhenEventDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _service.DeleteAsync(nonExistentId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region PublishAsync Tests

    [Fact]
    public async Task PublishAsync_FromDraftWithFutureEndDate_PublishesEvent()
    {
        // Arrange
        var futureDate = DateTime.UtcNow.AddDays(7);
        var eventEntity = CreateTestEvent("Event to Publish", startDateTime: futureDate, endDateTime: futureDate.AddHours(3), status: EventStatus.Draft);
        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.PublishAsync(eventEntity.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(EventStatus.Published);
        result.PublishedAt.Should().NotBeNull();
        result.PublishedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify in database
        var dbEvent = await _context.Events.FindAsync(eventEntity.Id);
        dbEvent!.Status.Should().Be(EventStatus.Published.ToString());
        dbEvent.PublishedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task PublishAsync_FromDraftWithPastEndDate_ReturnsNull()
    {
        // Arrange
        var pastDate = DateTime.UtcNow.AddDays(-1);
        var eventEntity = CreateTestEvent("Past Event", startDateTime: pastDate.AddHours(-3), endDateTime: pastDate, status: EventStatus.Draft);
        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync();

        // Act & Assert
        var act = async () => await _service.PublishAsync(eventEntity.Id);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot publish event with end date in the past.");

        // Verify status unchanged in database
        var dbEvent = await _context.Events.FindAsync(eventEntity.Id);
        dbEvent!.Status.Should().Be(EventStatus.Draft.ToString());
    }

    [Fact]
    public async Task PublishAsync_FromCompleted_ReturnsNull()
    {
        // Arrange
        var futureDate = DateTime.UtcNow.AddDays(7);
        var eventEntity = CreateTestEvent("Completed Event", startDateTime: futureDate, endDateTime: futureDate.AddHours(3), status: EventStatus.Completed);
        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync();

        // Act & Assert
        var act = async () => await _service.PublishAsync(eventEntity.Id);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot publish event from Completed status. Only Draft events can be published.");

        // Verify status unchanged
        var dbEvent = await _context.Events.FindAsync(eventEntity.Id);
        dbEvent!.Status.Should().Be(EventStatus.Completed.ToString());
    }

    [Fact]
    public async Task PublishAsync_WhenEventDoesNotExist_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _service.PublishAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region CompleteAsync Tests

    [Fact]
    public async Task CompleteAsync_FromPublished_CompletesEvent()
    {
        // Arrange
        var eventEntity = CreateTestEvent("Event to Complete", status: EventStatus.Published);
        eventEntity.PublishedAt = DateTime.UtcNow.AddDays(-1);
        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CompleteAsync(eventEntity.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(EventStatus.Completed);

        // Verify in database
        var dbEvent = await _context.Events.FindAsync(eventEntity.Id);
        dbEvent!.Status.Should().Be(EventStatus.Completed.ToString());
    }

    [Fact]
    public async Task CompleteAsync_FromDraft_ReturnsNull()
    {
        // Arrange
        var eventEntity = CreateTestEvent("Draft Event", status: EventStatus.Draft);
        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync();

        // Act & Assert
        var act = async () => await _service.CompleteAsync(eventEntity.Id);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot transition from Draft to Completed status. Only Published events can be completed.");
    }

    [Fact]
    public async Task CompleteAsync_WhenEventDoesNotExist_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _service.CompleteAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region CancelAsync Tests

    [Fact]
    public async Task CancelAsync_FromDraft_CancelsEvent()
    {
        // Arrange
        var eventEntity = CreateTestEvent("Event to Cancel", status: EventStatus.Draft);
        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CancelAsync(eventEntity.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(EventStatus.Cancelled);

        // Verify in database
        var dbEvent = await _context.Events.FindAsync(eventEntity.Id);
        dbEvent!.Status.Should().Be(EventStatus.Cancelled.ToString());
    }

    [Fact]
    public async Task CancelAsync_FromPublished_CancelsEvent()
    {
        // Arrange
        var eventEntity = CreateTestEvent("Published Event to Cancel", status: EventStatus.Published);
        eventEntity.PublishedAt = DateTime.UtcNow.AddDays(-1);
        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CancelAsync(eventEntity.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(EventStatus.Cancelled);
    }

    [Fact]
    public async Task CancelAsync_FromCompleted_ReturnsNull()
    {
        // Arrange
        var eventEntity = CreateTestEvent("Completed Event", status: EventStatus.Completed);
        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync();

        // Act & Assert
        var act = async () => await _service.CancelAsync(eventEntity.Id);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot cancel event from Completed status. Only Draft or Published events can be cancelled.");
    }

    [Fact]
    public async Task CancelAsync_WhenEventDoesNotExist_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _service.CancelAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region ToDto Tests

    [Fact]
    public void ToDto_ConvertsEntityToDto()
    {
        // Arrange
        var eventEntity = CreateTestEvent("Test Event", EventType.Fundraiser);
        eventEntity.PublishedAt = DateTime.UtcNow;

        // Act
        var result = _service.ToDto(eventEntity);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(eventEntity.Id);
        result.Title.Should().Be("Test Event");
        result.Description.Should().Be("Test description");
        result.EventType.Should().Be(EventType.Fundraiser);
        result.Status.Should().Be(EventStatus.Draft);
        result.CreatedById.Should().Be(_testUserId);
        result.CreatedAt.Should().Be(eventEntity.CreatedAt);
        result.UpdatedAt.Should().Be(eventEntity.UpdatedAt);
        result.PublishedAt.Should().Be(eventEntity.PublishedAt);
    }

    #endregion

    #region Helper Methods

    private Event CreateTestEvent(
        string title,
        EventType eventType = EventType.SpecialEvent,
        DateTime? startDateTime = null,
        DateTime? endDateTime = null,
        EventStatus status = EventStatus.Draft)
    {
        var start = startDateTime ?? DateTime.UtcNow.AddDays(7);
        var end = endDateTime ?? start.AddHours(3);

        return new Event
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = "Test description",
            EventType = eventType.ToString(),
            StartDateTime = start,
            EndDateTime = end,
            Status = status.ToString(),
            CreatedById = _testUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            PublishedAt = status == EventStatus.Published ? DateTime.UtcNow : null,
        };
    }

    #endregion
}
