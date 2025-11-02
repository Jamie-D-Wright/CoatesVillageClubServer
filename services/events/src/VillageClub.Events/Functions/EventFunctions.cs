using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
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
/// Azure Functions for event mutation operations (Create, Update, Delete, Publish, Complete, Cancel).
/// </summary>
public class EventFunctions
{
    private readonly ILogger<EventFunctions> _logger;
    private readonly IEventService _eventService;
    private readonly IValidator<CreateEventRequest> _createValidator;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventFunctions"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="eventService">Event service instance.</param>
    /// <param name="createValidator">Validator for create event requests.</param>
    public EventFunctions(
        ILogger<EventFunctions> logger,
        IEventService eventService,
        IValidator<CreateEventRequest> createValidator)
    {
        _logger = logger;
        _eventService = eventService;
        _createValidator = createValidator;
        _jsonOptions = new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        };
    }

    /// <summary>
    /// Creates a new event.
    /// </summary>
    /// <param name="req">HTTP request.</param>
    /// <param name="context">Function context.</param>
    /// <returns>Created event.</returns>
    [Function("CreateEvent")]
    [OpenApiOperation(operationId: "CreateEvent", tags: new[] { "Events" }, Summary = "Create a new event", Description = "Creates a new event in Draft status. Committee members only.")]
    [OpenApiSecurity("bearer_auth", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(CreateEventRequest), Required = true, Description = "Event details")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(EventDto), Description = "Event created successfully")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "Invalid request data")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "User not authenticated")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Forbidden, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "User does not have Committee role")]
    public async Task<HttpResponseData> CreateEvent(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "events")] HttpRequestData req,
        FunctionContext context)
    {
        try
        {
            // Check Committee role authorization
            var authResponse = await AuthorizationHelper.CheckCommitteeRole(context, req, _logger);
            if (authResponse != null)
            {
                return authResponse;
            }

            // Parse request body
            var requestBody = await req.ReadAsStringAsync();
            var createRequest = JsonSerializer.Deserialize<CreateEventRequest>(requestBody!, _jsonOptions);

            if (createRequest == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request body");
            }

            // Validate request
            var validationResult = await _createValidator.ValidateAsync(createRequest);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

                var errorResponse = new ErrorResponse
                {
                    Message = "Validation failed",
                    Code = "VALIDATION_ERROR",
                    ValidationErrors = errors,
                };
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                await response.WriteStringAsync(JsonSerializer.Serialize(errorResponse, _jsonOptions));
                return response;
            }

            // Get authenticated user ID from JWT claims
            var createdById = context.GetUserId()!.Value;

            // Create event
            var eventDto = await _eventService.CreateAsync(createRequest, createdById);

            var successResponse = req.CreateResponse(HttpStatusCode.Created);
            successResponse.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await successResponse.WriteStringAsync(JsonSerializer.Serialize(eventDto, _jsonOptions));
            return successResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating event");
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "An error occurred while creating the event");
        }
    }

    /// <summary>
    /// Updates an existing event.
    /// </summary>
    /// <param name="req">HTTP request.</param>
    /// <param name="id">Event ID.</param>
    /// <param name="context">Function context.</param>
    /// <returns>Updated event.</returns>
    [Function("UpdateEvent")]
    [OpenApiOperation(operationId: "UpdateEvent", tags: new[] { "Events" }, Summary = "Update an event", Description = "Updates an existing event. Committee members only.")]
    [OpenApiSecurity("bearer_auth", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT")]
    [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Description = "Event ID")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(UpdateEventRequest), Required = true, Description = "Updated event details")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(EventDto), Description = "Event updated successfully")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "Invalid request data")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "Event not found")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "User not authenticated")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Forbidden, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "User does not have Committee role")]
    public async Task<HttpResponseData> UpdateEvent(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "events/{id}")] HttpRequestData req,
        string id,
        FunctionContext context)
    {
        try
        {
            // Check Committee role authorization
            var authResponse = await AuthorizationHelper.CheckCommitteeRole(context, req, _logger);
            if (authResponse != null)
            {
                return authResponse;
            }

            if (!Guid.TryParse(id, out var eventId))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid event ID format");
            }

            // Parse request body
            var requestBody = await req.ReadAsStringAsync();
            var updateRequest = JsonSerializer.Deserialize<UpdateEventRequest>(requestBody!, _jsonOptions);

            if (updateRequest == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request body");
            }

            // Update event
            var eventDto = await _eventService.UpdateAsync(eventId, updateRequest);

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
            _logger.LogError(ex, "Error updating event {EventId}", id);
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "An error occurred while updating the event");
        }
    }

    /// <summary>
    /// Deletes an event.
    /// </summary>
    /// <param name="req">HTTP request.</param>
    /// <param name="id">Event ID.</param>
    /// <param name="context">Function context.</param>
    /// <returns>No content on success.</returns>
    [Function("DeleteEvent")]
    [OpenApiOperation(operationId: "DeleteEvent", tags: new[] { "Events" }, Summary = "Delete an event", Description = "Deletes an event. Committee members only.")]
    [OpenApiSecurity("bearer_auth", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT")]
    [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Description = "Event ID")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NoContent, Description = "Event deleted successfully")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "Event not found")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "User not authenticated")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Forbidden, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "User does not have Committee role")]
    public async Task<HttpResponseData> DeleteEvent(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "events/{id}")] HttpRequestData req,
        string id,
        FunctionContext context)
    {
        try
        {
            // Check Committee role authorization
            var authResponse = await AuthorizationHelper.CheckCommitteeRole(context, req, _logger);
            if (authResponse != null)
            {
                return authResponse;
            }

            if (!Guid.TryParse(id, out var eventId))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid event ID format");
            }

            var deleted = await _eventService.DeleteAsync(eventId);

            if (!deleted)
            {
                return await CreateErrorResponse(req, HttpStatusCode.NotFound, "Event not found", "NOT_FOUND");
            }

            return req.CreateResponse(HttpStatusCode.NoContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting event {EventId}", id);
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "An error occurred while deleting the event");
        }
    }

    /// <summary>
    /// Publishes a draft event.
    /// </summary>
    /// <param name="req">HTTP request.</param>
    /// <param name="id">Event ID.</param>
    /// <param name="context">Function context.</param>
    /// <returns>Published event.</returns>
    [Function("PublishEvent")]
    [OpenApiOperation(operationId: "PublishEvent", tags: new[] { "Events" }, Summary = "Publish an event", Description = "Transitions an event from Draft to Published status. Committee members only.")]
    [OpenApiSecurity("bearer_auth", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT")]
    [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Description = "Event ID")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(EventDto), Description = "Event published successfully")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "Invalid state transition or end date in past")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "Event not found")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "User not authenticated")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Forbidden, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "User does not have Committee role")]
    public async Task<HttpResponseData> PublishEvent(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "events/{id}/publish")] HttpRequestData req,
        string id,
        FunctionContext context)
    {
        try
        {
            // Check Committee role authorization
            var authResponse = await AuthorizationHelper.CheckCommitteeRole(context, req, _logger);
            if (authResponse != null)
            {
                return authResponse;
            }

            if (!Guid.TryParse(id, out var eventId))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid event ID format");
            }

            var eventDto = await _eventService.PublishAsync(eventId);

            if (eventDto == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.NotFound, "Event not found", "NOT_FOUND");
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await response.WriteStringAsync(JsonSerializer.Serialize(eventDto, _jsonOptions));
            return response;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid state transition for event {EventId}", id);
            return await CreateErrorResponse(req, HttpStatusCode.BadRequest, ex.Message, "INVALID_STATE_TRANSITION");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing event {EventId}", id);
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "An error occurred while publishing the event");
        }
    }

    /// <summary>
    /// Completes a published event.
    /// </summary>
    /// <param name="req">HTTP request.</param>
    /// <param name="id">Event ID.</param>
    /// <param name="context">Function context.</param>
    /// <returns>Completed event.</returns>
    [Function("CompleteEvent")]
    [OpenApiOperation(operationId: "CompleteEvent", tags: new[] { "Events" }, Summary = "Complete an event", Description = "Transitions an event from Published to Completed status. Committee members only.")]
    [OpenApiSecurity("bearer_auth", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT")]
    [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Description = "Event ID")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(EventDto), Description = "Event completed successfully")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "Invalid state transition")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "Event not found")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "User not authenticated")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Forbidden, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "User does not have Committee role")]
    public async Task<HttpResponseData> CompleteEvent(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "events/{id}/complete")] HttpRequestData req,
        string id,
        FunctionContext context)
    {
        try
        {
            // Check Committee role authorization
            var authResponse = await AuthorizationHelper.CheckCommitteeRole(context, req, _logger);
            if (authResponse != null)
            {
                return authResponse;
            }

            if (!Guid.TryParse(id, out var eventId))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid event ID format");
            }

            var eventDto = await _eventService.CompleteAsync(eventId);

            if (eventDto == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.NotFound, "Event not found", "NOT_FOUND");
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await response.WriteStringAsync(JsonSerializer.Serialize(eventDto, _jsonOptions));
            return response;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid state transition for event {EventId}", id);
            return await CreateErrorResponse(req, HttpStatusCode.BadRequest, ex.Message, "INVALID_STATE_TRANSITION");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing event {EventId}", id);
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "An error occurred while completing the event");
        }
    }

    /// <summary>
    /// Cancels an event.
    /// </summary>
    /// <param name="req">HTTP request.</param>
    /// <param name="id">Event ID.</param>
    /// <param name="context">Function context.</param>
    /// <returns>Cancelled event.</returns>
    [Function("CancelEvent")]
    [OpenApiOperation(operationId: "CancelEvent", tags: new[] { "Events" }, Summary = "Cancel an event", Description = "Transitions an event to Cancelled status. Committee members only.")]
    [OpenApiSecurity("bearer_auth", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT")]
    [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Description = "Event ID")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(EventDto), Description = "Event cancelled successfully")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "Invalid state transition")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "Event not found")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "User not authenticated")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Forbidden, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "User does not have Committee role")]
    public async Task<HttpResponseData> CancelEvent(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "events/{id}/cancel")] HttpRequestData req,
        string id,
        FunctionContext context)
    {
        try
        {
            // Check Committee role authorization
            var authResponse = await AuthorizationHelper.CheckCommitteeRole(context, req, _logger);
            if (authResponse != null)
            {
                return authResponse;
            }

            if (!Guid.TryParse(id, out var eventId))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid event ID format");
            }

            var eventDto = await _eventService.CancelAsync(eventId);

            if (eventDto == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.NotFound, "Event not found", "NOT_FOUND");
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            await response.WriteStringAsync(JsonSerializer.Serialize(eventDto, _jsonOptions));
            return response;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid state transition for event {EventId}", id);
            return await CreateErrorResponse(req, HttpStatusCode.BadRequest, ex.Message, "INVALID_STATE_TRANSITION");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling event {EventId}", id);
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "An error occurred while cancelling the event");
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
