using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using VillageClub.Contracts.Models;
using VillageClub.Events.Core.Models;
using VillageClub.Events.Middleware;
using VillageClub.Events.Services;

namespace VillageClub.Events.Functions;

/// <summary>
/// Azure Functions for event query operations (Get, List).
/// </summary>
public class EventQueryFunctions
{
    private readonly ILogger<EventQueryFunctions> _logger;
    private readonly IEventService _eventService;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventQueryFunctions"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="eventService">Event service instance.</param>
    public EventQueryFunctions(
        ILogger<EventQueryFunctions> logger,
        IEventService eventService)
    {
        _logger = logger;
        _eventService = eventService;
        _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    }

    /// <summary>
    /// Gets a single event by ID.
    /// </summary>
    /// <param name="req">HTTP request.</param>
    /// <param name="id">Event ID.</param>
    /// <param name="context">Function context.</param>
    /// <returns>Event details.</returns>
    [Function("GetEvent")]
    [OpenApiOperation(operationId: "GetEvent", tags: new[] { "Events" }, Summary = "Get event by ID", Description = "Retrieves a single event by its ID. Authenticated users only.")]
    [OpenApiSecurity("bearer_auth", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT")]
    [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Description = "Event ID")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(EventDto), Description = "Event found")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "Event not found")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "User not authenticated")]
    public async Task<HttpResponseData> GetEvent(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "events/{id}")] HttpRequestData req,
        string id,
        FunctionContext context)
    {
        try
        {
            // Check authentication - all authenticated users can view events
            var authResponse = await AuthorizationHelper.CheckAuthentication(context, req, _logger);
            if (authResponse != null)
            {
                return authResponse;
            }

            if (!Guid.TryParse(id, out var eventId))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid event ID format");
            }

            var eventDto = await _eventService.GetByIdAsync(eventId);

            if (eventDto == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.NotFound, "Event not found", "NOT_FOUND");
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await response.WriteStringAsync(JsonSerializer.Serialize(eventDto, _jsonOptions));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving event {EventId}", id);
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "An error occurred while retrieving the event");
        }
    }

    /// <summary>
    /// Gets a paginated list of events.
    /// </summary>
    /// <param name="req">HTTP request.</param>
    /// <param name="context">Function context.</param>
    /// <returns>Paginated list of events.</returns>
    [Function("GetEvents")]
    [OpenApiOperation(operationId: "GetEvents", tags: new[] { "Events" }, Summary = "Get events", Description = "Retrieves a paginated list of events, optionally filtered by status. Authenticated users only.")]
    [OpenApiSecurity("bearer_auth", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT")]
    [OpenApiParameter(name: "page", In = ParameterLocation.Query, Required = false, Type = typeof(int), Description = "Page number (default: 1)")]
    [OpenApiParameter(name: "pageSize", In = ParameterLocation.Query, Required = false, Type = typeof(int), Description = "Page size (default: 20, max: 100)")]
    [OpenApiParameter(name: "includeStatus", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "Comma-separated list of statuses to include (Draft, Published, Completed, Cancelled). Default: Published,Completed")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(PagedResult<EventDto>), Description = "Events retrieved successfully")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "Invalid query parameters")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "User not authenticated")]
    public async Task<HttpResponseData> GetEvents(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "events")] HttpRequestData req,
        FunctionContext context)
    {
        try
        {
            // Check authentication - all authenticated users can view events
            var authResponse = await AuthorizationHelper.CheckAuthentication(context, req, _logger);
            if (authResponse != null)
            {
                return authResponse;
            }

            // Parse query parameters
            var queryParams = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            
            var page = 1;
            var pageSize = 20;
            string? includeStatus = null;

            if (queryParams["page"] != null && (!int.TryParse(queryParams["page"], out page) || page < 1))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Page number must be a positive integer");
            }

            if (queryParams["pageSize"] != null)
            {
                if (!int.TryParse(queryParams["pageSize"], out pageSize) || pageSize < 1)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Page size must be a positive integer");
                }

                if (pageSize > 100)
                {
                    pageSize = 100;
                    _logger.LogInformation("Page size capped at 100");
                }
            }

            if (queryParams["includeStatus"] != null)
            {
                includeStatus = queryParams["includeStatus"];
            }

            // Get events
            var result = await _eventService.GetAllAsync(page, pageSize, includeStatus);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await response.WriteStringAsync(JsonSerializer.Serialize(result, _jsonOptions));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving events");
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "An error occurred while retrieving events");
        }
    }

    /// <summary>
    /// Creates an error response with standardized ErrorResponse format.
    /// </summary>
    private async Task<HttpResponseData> CreateErrorResponse(
        HttpRequestData req,
        HttpStatusCode statusCode,
        string errorMessage,
        string? errorCode = null)
    {
        var errorResponse = new ErrorResponse
        {
            Message = errorMessage,
            Code = errorCode ?? statusCode.ToString().ToUpperInvariant().Replace(" ", "_"),
        };

        var response = req.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        await response.WriteStringAsync(JsonSerializer.Serialize(errorResponse, _jsonOptions));
        return response;
    }
}
