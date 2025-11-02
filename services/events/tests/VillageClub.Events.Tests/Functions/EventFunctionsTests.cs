using System.Net;
using FluentAssertions;
using FluentValidation;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VillageClub.Contracts.Models;
using VillageClub.Events.Core.Models;
using VillageClub.Events.Core.Validators;
using VillageClub.Events.Data;
using VillageClub.Events.Functions;
using VillageClub.Events.Services;
using Xunit;

namespace VillageClub.Events.Tests.Functions;

/// <summary>
/// Integration tests for EventFunctions HTTP endpoints.
/// Tests full request/response cycle including validation, CRUD operations, and authorization.
/// Per Constitution Principle IV: Tests use real database (SQLite in-memory) over mocks.
/// </summary>
public class EventFunctionsTests : FunctionTestBase
{
    private readonly EventFunctions _eventFunctions;
    private readonly EventsDbContext _dbContext;
    private readonly Guid _testUserId = Guid.NewGuid();

    public EventFunctionsTests()
    {
        _dbContext = ServiceProvider.GetRequiredService<EventsDbContext>();
        _eventFunctions = new EventFunctions(
            ServiceProvider.GetRequiredService<ILogger<EventFunctions>>(),
            ServiceProvider.GetRequiredService<IEventService>(),
            ServiceProvider.GetRequiredService<IValidator<CreateEventRequest>>());
    }

    #region CreateEvent Tests

    [Fact]
    public async Task CreateEvent_WithValidRequest_Returns201Created()
    {
        // Arrange
        SetupAuthContext(_testUserId, "committee@example.com", "Committee", "Treasurer");
        var request = CreateHttpRequest("POST", new CreateEventRequest
        {
            Title = "Quiz Night",
            Description = "Monthly quiz night",
            EventType = EventType.SpecialEvent,
            StartDateTime = DateTime.UtcNow.AddDays(7),
            EndDateTime = DateTime.UtcNow.AddDays(7).AddHours(3),
        });

        // Act
        var response = await _eventFunctions.CreateEvent(request, MockFunctionContext.Object);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await ReadResponseBody<EventDto>(response);
        result.Should().NotBeNull();
        result!.Title.Should().Be("Quiz Night");
        result.Status.Should().Be(EventStatus.Draft);
        result.CreatedById.Should().Be(_testUserId);
    }

    [Fact]
    public async Task CreateEvent_WithoutAuthentication_Returns401Unauthorized()
    {
        // Arrange - no auth context set up
        var request = CreateHttpRequest("POST", new CreateEventRequest
        {
            Title = "Quiz Night",
            EventType = EventType.SpecialEvent,
            StartDateTime = DateTime.UtcNow.AddDays(7),
            EndDateTime = DateTime.UtcNow.AddDays(7).AddHours(3),
        });

        // Act
        var response = await _eventFunctions.CreateEvent(request, MockFunctionContext.Object);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var error = await ReadResponseBody<ErrorResponse>(response);
        error.Should().NotBeNull();
        error!.Code.Should().Be("UNAUTHORIZED");
    }

    [Fact]
    public async Task CreateEvent_WithNonCommitteeRole_Returns403Forbidden()
    {
        // Arrange - Member role (not Committee)
        SetupAuthContext(_testUserId, "member@example.com", "Member");
        var request = CreateHttpRequest("POST", new CreateEventRequest
        {
            Title = "Quiz Night",
            EventType = EventType.SpecialEvent,
            StartDateTime = DateTime.UtcNow.AddDays(7),
            EndDateTime = DateTime.UtcNow.AddDays(7).AddHours(3),
        });

        // Act
        var response = await _eventFunctions.CreateEvent(request, MockFunctionContext.Object);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var error = await ReadResponseBody<ErrorResponse>(response);
        error.Should().NotBeNull();
        error!.Code.Should().Be("FORBIDDEN");
    }

    [Fact]
    public async Task CreateEvent_WithInvalidData_Returns400BadRequest()
    {
        // Arrange
        SetupAuthContext(_testUserId, "committee@example.com", "Committee", "Treasurer");
        var request = CreateHttpRequest("POST", new CreateEventRequest
        {
            Title = string.Empty, // Invalid: empty title
            EventType = EventType.SpecialEvent,
            StartDateTime = DateTime.UtcNow.AddDays(7),
            EndDateTime = DateTime.UtcNow.AddDays(7).AddHours(3),
        });

        // Act
        var response = await _eventFunctions.CreateEvent(request, MockFunctionContext.Object);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadResponseBody<ErrorResponse>(response);
        error.Should().NotBeNull();
        error!.Code.Should().Be("VALIDATION_ERROR");
        error.ValidationErrors.Should().ContainKey("Title");
    }

    [Fact]
    public async Task CreateEvent_WithPastEndDateTime_Returns400BadRequest()
    {
        // Arrange
        SetupAuthContext(_testUserId, "committee@example.com", "Committee", "Treasurer");
        var request = CreateHttpRequest("POST", new CreateEventRequest
        {
            Title = "Past Event",
            EventType = EventType.SpecialEvent,
            StartDateTime = DateTime.UtcNow.AddDays(-2),
            EndDateTime = DateTime.UtcNow.AddDays(-1), // Past end date
        });

        // Act
        var response = await _eventFunctions.CreateEvent(request, MockFunctionContext.Object);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadResponseBody<ErrorResponse>(response);
        error.Should().NotBeNull();
        error!.Code.Should().Be("VALIDATION_ERROR");
        error.ValidationErrors.Should().ContainKey("StartDateTime");
        error.ValidationErrors.Should().ContainKey("Duration");
    }
    #endregion

    #region UpdateEvent Tests

    [Fact]
    public async Task UpdateEvent_WithValidRequest_Returns200OK()
    {
        // Arrange
        SetupAuthContext(_testUserId, "committee@example.com", "Committee", "Treasurer");
        var existingEvent = await CreateTestEvent("Original Event");
        var request = CreateHttpRequest("PUT", new UpdateEventRequest
        {
            Title = "Updated Event",
            Description = "Updated description",
        });

        // Act
        var response = await _eventFunctions.UpdateEvent(request, existingEvent.Id.ToString(), MockFunctionContext.Object);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await ReadResponseBody<EventDto>(response);
        result.Should().NotBeNull();
        result!.Title.Should().Be("Updated Event");
        result.Description.Should().Be("Updated description");
    }

    [Fact]
    public async Task UpdateEvent_WhenEventDoesNotExist_Returns404NotFound()
    {
        // Arrange
        SetupAuthContext(_testUserId, "committee@example.com", "Committee", "Treasurer");
        var nonExistentId = Guid.NewGuid();
        var request = CreateHttpRequest("PUT", new UpdateEventRequest
        {
            Title = "Updated Event",
        });

        // Act
        var response = await _eventFunctions.UpdateEvent(request, nonExistentId.ToString(), MockFunctionContext.Object);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var error = await ReadResponseBody<ErrorResponse>(response);
        error.Should().NotBeNull();
        error!.Code.Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task UpdateEvent_WithoutAuthentication_Returns401Unauthorized()
    {
        // Arrange - no auth context
        var existingEvent = await CreateTestEvent("Original Event");
        var request = CreateHttpRequest("PUT", new UpdateEventRequest
        {
            Title = "Updated Event",
        });

        // Act
        var response = await _eventFunctions.UpdateEvent(request, existingEvent.Id.ToString(), MockFunctionContext.Object);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region DeleteEvent Tests

    [Fact]
    public async Task DeleteEvent_WhenEventExists_Returns204NoContent()
    {
        // Arrange
        SetupAuthContext(_testUserId, "committee@example.com", "Committee", "Treasurer");
        var existingEvent = await CreateTestEvent("Event to Delete");

        var request = CreateHttpRequest("DELETE");

        // Act
        var response = await _eventFunctions.DeleteEvent(request, existingEvent.Id.ToString(), MockFunctionContext.Object);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify event is deleted
        var deleted = await _dbContext.Events.FindAsync(existingEvent.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteEvent_WhenEventDoesNotExist_Returns404NotFound()
    {
        // Arrange
        SetupAuthContext(_testUserId, "committee@example.com", "Committee", "Treasurer");
        var nonExistentId = Guid.NewGuid();
        var request = CreateHttpRequest("DELETE");

        // Act
        var response = await _eventFunctions.DeleteEvent(request, nonExistentId.ToString(), MockFunctionContext.Object);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteEvent_WithoutAuthentication_Returns401Unauthorized()
    {
        // Arrange - no auth context
        var existingEvent = await CreateTestEvent("Event to Delete");
        var request = CreateHttpRequest("DELETE");

        // Act
        var response = await _eventFunctions.DeleteEvent(request, existingEvent.Id.ToString(), MockFunctionContext.Object);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion
    #region PublishEvent Tests

    [Fact]
    public async Task PublishEvent_FromDraftStatus_Returns200OK()
    {
        // Arrange
        SetupAuthContext(_testUserId, "committee@example.com", "Committee", "Treasurer");
        var draftEvent = await CreateTestEvent("Draft Event", EventStatus.Draft);
        var request = CreateHttpRequest("POST");

        // Act
        var response = await _eventFunctions.PublishEvent(request, draftEvent.Id.ToString(), MockFunctionContext.Object);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await ReadResponseBody<EventDto>(response);
        result.Should().NotBeNull();
        result!.Status.Should().Be(EventStatus.Published);
    }

    [Fact]
    public async Task PublishEvent_WhenEventDoesNotExist_Returns404NotFound()
    {
        // Arrange
        SetupAuthContext(_testUserId, "committee@example.com", "Committee", "Treasurer");
        var nonExistentId = Guid.NewGuid();
        var request = CreateHttpRequest("POST");

        // Act
        var response = await _eventFunctions.PublishEvent(request, nonExistentId.ToString(), MockFunctionContext.Object);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region CompleteEvent Tests

    [Fact]
    public async Task CompleteEvent_FromPublishedStatus_Returns200OK()
    {
        // Arrange
        SetupAuthContext(_testUserId, "committee@example.com", "Committee", "Treasurer");
        var publishedEvent = await CreateTestEvent("Published Event", EventStatus.Published);
        var request = CreateHttpRequest("POST");

        // Act
        var response = await _eventFunctions.CompleteEvent(request, publishedEvent.Id.ToString(), MockFunctionContext.Object);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await ReadResponseBody<EventDto>(response);
        result.Should().NotBeNull();
        result!.Status.Should().Be(EventStatus.Completed);
    }

    [Fact]
    public async Task CompleteEvent_FromDraftStatus_Returns400BadRequest()
    {
        // Arrange
        SetupAuthContext(_testUserId, "committee@example.com", "Committee", "Treasurer");
        var draftEvent = await CreateTestEvent("Draft Event", EventStatus.Draft);
        var request = CreateHttpRequest("POST");

        // Act
        var response = await _eventFunctions.CompleteEvent(request, draftEvent.Id.ToString(), MockFunctionContext.Object);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region CancelEvent Tests

    [Fact]
    public async Task CancelEvent_FromDraftStatus_Returns200OK()
    {
        // Arrange
        SetupAuthContext(_testUserId, "committee@example.com", "Committee", "Treasurer");
        var draftEvent = await CreateTestEvent("Draft Event", EventStatus.Draft);
        var request = CreateHttpRequest("POST");

        // Act
        var response = await _eventFunctions.CancelEvent(request, draftEvent.Id.ToString(), MockFunctionContext.Object);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await ReadResponseBody<EventDto>(response);
        result.Should().NotBeNull();
        result!.Status.Should().Be(EventStatus.Cancelled);
    }

    [Fact]
    public async Task CancelEvent_FromPublishedStatus_Returns200OK()
    {
        // Arrange
        SetupAuthContext(_testUserId, "committee@example.com", "Committee", "Treasurer");
        var publishedEvent = await CreateTestEvent("Published Event", EventStatus.Published);
        var request = CreateHttpRequest("POST");

        // Act
        var response = await _eventFunctions.CancelEvent(request, publishedEvent.Id.ToString(), MockFunctionContext.Object);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await ReadResponseBody<EventDto>(response);
        result.Should().NotBeNull();
        result!.Status.Should().Be(EventStatus.Cancelled);
    }

    [Fact]
    public async Task CancelEvent_FromCompletedStatus_Returns400BadRequest()
    {
        // Arrange
        SetupAuthContext(_testUserId, "committee@example.com", "Committee", "Treasurer");
        var completedEvent = await CreateTestEvent("Completed Event", EventStatus.Completed);
        var request = CreateHttpRequest("POST");

        // Act
        var response = await _eventFunctions.CancelEvent(request, completedEvent.Id.ToString(), MockFunctionContext.Object);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion
    protected override void ConfigureServices(IServiceCollection services)
    {
        // Create and open a shared in-memory SQLite connection
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        // Add in-memory SQLite database (real database per Constitution)
        services.AddDbContext<EventsDbContext>(options =>
        {
            options.UseSqlite(connection); // Use the shared connection
            options.EnableSensitiveDataLogging();
        });

        // Add services
        services.AddScoped<IEventService, EventService>();

        // Add validators
        services.AddScoped<IValidator<CreateEventRequest>, CreateEventRequestValidator>();

        // Add logging
        services.AddLogging(builder => builder.AddConsole());

        // Ensure database is created on the shared connection
        var serviceProvider = services.BuildServiceProvider();
        var db = serviceProvider.GetRequiredService<EventsDbContext>();
        db.Database.EnsureCreated();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _dbContext?.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Helper Methods

    private async Task<Data.Entities.Event> CreateTestEvent(string title, EventStatus status = EventStatus.Draft)
    {
        var eventEntity = new Data.Entities.Event
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = "Test description",
            EventType = EventType.SpecialEvent.ToString(),
            StartDateTime = DateTime.UtcNow.AddDays(7),
            EndDateTime = DateTime.UtcNow.AddDays(7).AddHours(3),
            Status = status.ToString(),
            CreatedById = _testUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _dbContext.Events.Add(eventEntity);
        await _dbContext.SaveChangesAsync();
        return eventEntity;
    }

    #endregion
}
