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
/// Integration tests for EventQueryFunctions.
/// These tests verify HTTP endpoints, validation, authorization, and filtering logic.
/// Per Constitution Principle IV: Tests use real database (SQLite in-memory) over mocks.
/// </summary>
public class EventQueryFunctionsTests : FunctionTestBase
{
    private readonly EventQueryFunctions _eventQueryFunctions;
    private readonly IEventService _eventService;
    private readonly EventsDbContext _dbContext;

    public EventQueryFunctionsTests()
    {
        _dbContext = ServiceProvider.GetRequiredService<EventsDbContext>();
        _eventService = ServiceProvider.GetRequiredService<IEventService>();
        _eventQueryFunctions = new EventQueryFunctions(
            ServiceProvider.GetRequiredService<ILogger<EventQueryFunctions>>(),
            _eventService);
    }

    #region GetEvent Tests

    [Fact]
    public async Task GetEvent_WithValidId_Returns200OK()
    {
        // Arrange
        var createRequest = CreateValidEventRequest();
        var createdEvent = await _eventService.CreateAsync(createRequest, Guid.NewGuid());
        SetupAuthContext(Guid.NewGuid(), "test@example.com", "Member");
        var req = CreateHttpRequest("GET", $"/api/v1/events/{createdEvent!.Id}");

        // Act
        var response = await _eventQueryFunctions.GetEvent(req, createdEvent.Id.ToString(), MockFunctionContext.Object);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.GetValues("Content-Type").First().Should().Contain("application/json");

        var eventDto = await ReadResponseBody<EventDto>(response);
        eventDto.Should().NotBeNull();
        eventDto!.Id.Should().Be(createdEvent.Id);
        eventDto.Title.Should().Be(createdEvent.Title);
    }

    [Fact]
    public async Task GetEvent_WithInvalidId_Returns400BadRequest()
    {
        // Arrange
        SetupAuthContext(Guid.NewGuid(), "test@example.com", "Member");
        var req = CreateHttpRequest("GET", "/api/v1/events/invalid-guid");

        // Act
        var response = await _eventQueryFunctions.GetEvent(req, "invalid-guid", MockFunctionContext.Object);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorResponse = await ReadResponseBody<ErrorResponse>(response);
        errorResponse.Should().NotBeNull();
        errorResponse!.Message.Should().Contain("Invalid event ID format");
    }

    [Fact]
    public async Task GetEvent_WhenEventDoesNotExist_Returns404NotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        SetupAuthContext(Guid.NewGuid(), "test@example.com", "Member");
        var req = CreateHttpRequest("GET", $"/api/v1/events/{nonExistentId}");

        // Act
        var response = await _eventQueryFunctions.GetEvent(req, nonExistentId.ToString(), MockFunctionContext.Object);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var errorResponse = await ReadResponseBody<ErrorResponse>(response);
        errorResponse.Should().NotBeNull();
        errorResponse!.Message.Should().Contain("Event not found");
        errorResponse.Code.Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task GetEvent_WithoutAuthentication_Returns401Unauthorized()
    {
        // Arrange
        var createRequest = CreateValidEventRequest();
        var createdEvent = await _eventService.CreateAsync(createRequest, Guid.NewGuid());
        // Do not call SetupAuthContext - test unauthenticated request
        var req = CreateHttpRequest("GET", $"/api/v1/events/{createdEvent!.Id}");

        // Act
        var response = await _eventQueryFunctions.GetEvent(req, createdEvent.Id.ToString(), MockFunctionContext.Object);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GetEvents Tests

    [Fact]
    public async Task GetEvents_WithDefaultParameters_Returns200OK()
    {
        // Arrange
        var createRequest = CreateValidEventRequest();
        var event1 = await _eventService.CreateAsync(createRequest, Guid.NewGuid());
        await _eventService.PublishAsync(event1!.Id); // Publish so it appears in default results

        SetupAuthContext(Guid.NewGuid(), "test@example.com", "Member");
        var req = CreateHttpRequest("GET", "/api/v1/events");

        // Act
        var response = await _eventQueryFunctions.GetEvents(req, MockFunctionContext.Object);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var pagedResult = await ReadResponseBody<PagedResult<EventDto>>(response);
        pagedResult.Should().NotBeNull();
        pagedResult!.Page.Should().Be(1);
        pagedResult.PageSize.Should().Be(20);
        pagedResult.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task GetEvents_WithPagination_ReturnsCorrectPage()
    {
        // Arrange - create multiple events
        for (int i = 0; i < 5; i++)
        {
            var createRequest = new CreateEventRequest
            {
                Title = $"Event {i}",
                Description = $"Description {i}",
                EventType = EventType.SpecialEvent,
                StartDateTime = DateTime.UtcNow.AddDays(i + 1),
                EndDateTime = DateTime.UtcNow.AddDays(i + 2),
            };
            var evt = await _eventService.CreateAsync(createRequest, Guid.NewGuid());
            await _eventService.PublishAsync(evt!.Id); // Publish to appear in results
        }

        SetupAuthContext(Guid.NewGuid(), "test@example.com", "Member");
        var req = CreateHttpRequest("GET", "/api/v1/events?page=1&pageSize=2");

        // Act
        var response = await _eventQueryFunctions.GetEvents(req, MockFunctionContext.Object);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var pagedResult = await ReadResponseBody<PagedResult<EventDto>>(response);
        pagedResult.Should().NotBeNull();
        pagedResult!.Page.Should().Be(1);
        pagedResult.PageSize.Should().Be(2);
        pagedResult.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetEvents_WithInvalidPage_Returns400BadRequest()
    {
        // Arrange
        SetupAuthContext(Guid.NewGuid(), "test@example.com", "Member");
        var req = CreateHttpRequest("GET", "/api/v1/events?page=-1");

        // Act
        var response = await _eventQueryFunctions.GetEvents(req, MockFunctionContext.Object);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorResponse = await ReadResponseBody<ErrorResponse>(response);
        errorResponse.Should().NotBeNull();
        errorResponse!.Message.Should().Contain("Page number must be a positive integer");
    }

    [Fact]
    public async Task GetEvents_WithInvalidPageSize_Returns400BadRequest()
    {
        // Arrange
        SetupAuthContext(Guid.NewGuid(), "test@example.com", "Member");
        var req = CreateHttpRequest("GET", "/api/v1/events?pageSize=0");

        // Act
        var response = await _eventQueryFunctions.GetEvents(req, MockFunctionContext.Object);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorResponse = await ReadResponseBody<ErrorResponse>(response);
        errorResponse.Should().NotBeNull();
        errorResponse!.Message.Should().Contain("Page size must be a positive integer");
    }

    [Fact]
    public async Task GetEvents_WithPageSizeOver100_CapsAt100()
    {
        // Arrange
        SetupAuthContext(Guid.NewGuid(), "test@example.com", "Member");
        var req = CreateHttpRequest("GET", "/api/v1/events?pageSize=200");

        // Act
        var response = await _eventQueryFunctions.GetEvents(req, MockFunctionContext.Object);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var pagedResult = await ReadResponseBody<PagedResult<EventDto>>(response);
        pagedResult.Should().NotBeNull();
        pagedResult!.PageSize.Should().Be(100); // Capped at 100
    }

    [Fact]
    public async Task GetEvents_WithStatusFilter_ReturnsFilteredEvents()
    {
        // Arrange - create events with different statuses
        var draftRequest = CreateValidEventRequest();
        var draftEvent = await _eventService.CreateAsync(draftRequest, Guid.NewGuid());

        var publishedRequest = CreateValidEventRequest();
        publishedRequest.Title = "Published Event";
        var publishedEvent = await _eventService.CreateAsync(publishedRequest, Guid.NewGuid());
        await _eventService.PublishAsync(publishedEvent!.Id);

        SetupAuthContext(Guid.NewGuid(), "test@example.com", "Member");
        var req = CreateHttpRequest("GET", "/api/v1/events?includeStatus=Draft");

        // Act
        var response = await _eventQueryFunctions.GetEvents(req, MockFunctionContext.Object);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var pagedResult = await ReadResponseBody<PagedResult<EventDto>>(response);
        pagedResult.Should().NotBeNull();
        pagedResult!.Items.Should().NotBeNull();
        
        // Only Draft events should be returned
        pagedResult.Items.Should().Contain(e => e.Id == draftEvent!.Id);
        pagedResult.Items.Should().NotContain(e => e.Id == publishedEvent.Id);
    }

    [Fact]
    public async Task GetEvents_WithoutAuthentication_Returns401Unauthorized()
    {
        // Arrange
        // Do not call SetupAuthContext - test unauthenticated request
        var req = CreateHttpRequest("GET", "/api/v1/events");

        // Act
        var response = await _eventQueryFunctions.GetEvents(req, MockFunctionContext.Object);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Helper Methods

    private CreateEventRequest CreateValidEventRequest()
    {
        return new CreateEventRequest
        {
            Title = "Test Event",
            Description = "Test Description",
            EventType = EventType.SpecialEvent,
            StartDateTime = DateTime.UtcNow.AddDays(1),
            EndDateTime = DateTime.UtcNow.AddDays(2),
        };
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
}
