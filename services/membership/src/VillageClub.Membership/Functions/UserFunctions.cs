using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using VillageClub.Contracts.Models;
using VillageClub.Membership.Middleware;
using VillageClub.Membership.Models;
using VillageClub.Membership.Services;

namespace VillageClub.Membership.Functions;

/// <summary>
/// Azure Functions for user management operations.
/// </summary>
public class UserFunctions
{
    private readonly ILogger<UserFunctions> _logger;
    private readonly IUserService _userService;
    private readonly IValidator<CreateUserRequest> _createUserValidator;
    private readonly IValidator<UpdateUserRequest> _updateUserValidator;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserFunctions"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="userService">User service instance.</param>
    /// <param name="createUserValidator">Validator for create user requests.</param>
    /// <param name="updateUserValidator">Validator for update user requests.</param>
    public UserFunctions(
        ILogger<UserFunctions> logger,
        IUserService userService,
        IValidator<CreateUserRequest> createUserValidator,
        IValidator<UpdateUserRequest> updateUserValidator)
    {
        _logger = logger;
        _userService = userService;
        _createUserValidator = createUserValidator;
        _updateUserValidator = updateUserValidator;
    }

    /// <summary>
    /// Gets a paginated list of users.
    /// </summary>
    /// <param name="req">HTTP request.</param>
    /// <param name="context">Function context.</param>
    /// <returns>Paginated user list.</returns>
    [Function("GetUsers")]
    [OpenApiOperation(operationId: "GetUsers", tags: new[] { "Users" }, Summary = "List all users", Description = "Retrieves a paginated list of all users. Committee members only.")]
    [OpenApiSecurity("bearer_auth", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT")]
    [OpenApiParameter(name: "pageNumber", In = ParameterLocation.Query, Required = false, Type = typeof(int), Description = "Page number (default: 1)")]
    [OpenApiParameter(name: "pageSize", In = ParameterLocation.Query, Required = false, Type = typeof(int), Description = "Page size (default: 10, max: 100)")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(PagedResult<UserDto>), Description = "Paginated user list")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "User not authenticated")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Forbidden, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "User does not have Committee role")]
    public async Task<HttpResponseData> GetUsers(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users")] HttpRequestData req,
        FunctionContext context)
    {
        try
        {
            // Only Committee members can list all users
            var authResponse = AuthorizationHelper.CheckCommitteeRole(context, req, _logger);
            if (authResponse != null)
            {
                return authResponse;
            }

            // Parse query parameters
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var pageNumber = int.TryParse(query["pageNumber"], out var pn) && pn > 0 ? pn : 1;
            var pageSize = int.TryParse(query["pageSize"], out var ps) && ps > 0 && ps <= 100 ? ps : 10;

            var result = await _userService.GetAllAsync(pageNumber, pageSize);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users");
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "An error occurred while retrieving users");
        }
    }

    /// <summary>
    /// Gets a user by ID.
    /// </summary>
    /// <param name="req">HTTP request.</param>
    /// <param name="id">User ID.</param>
    /// <param name="context">Function context.</param>
    /// <returns>User details.</returns>
    [Function("GetUserById")]
    [OpenApiOperation(operationId: "GetUserById", tags: new[] { "Users" }, Summary = "Get user by ID", Description = "Retrieves a specific user by ID. Users can view their own profile; Committee members can view any user.")]
    [OpenApiSecurity("bearer_auth", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT")]
    [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "User ID (GUID)")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(UserDto), Description = "User found")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "Invalid user ID format")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "User not found")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "User not authenticated")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Forbidden, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "User cannot access this profile")]
    public async Task<HttpResponseData> GetUserById(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users/{id}")] HttpRequestData req,
        string id,
        FunctionContext context)
    {
        try
        {
            if (!Guid.TryParse(id, out var userId))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid user ID format");
            }

            // Users can view their own profile, Committee can view anyone
            var authResponse = AuthorizationHelper.CheckSelfOrCommittee(context, req, userId, _logger);
            if (authResponse != null)
            {
                return authResponse;
            }

            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.NotFound, "User not found");
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(user);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId}", id);
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "An error occurred while retrieving the user");
        }
    }

    /// <summary>
    /// Gets the current authenticated user's profile.
    /// </summary>
    /// <param name="req">HTTP request.</param>
    /// <param name="context">Function context.</param>
    /// <returns>Current user details.</returns>
    [Function("GetCurrentUser")]
    [OpenApiOperation(operationId: "GetCurrentUser", tags: new[] { "Users" }, Summary = "Get current user profile", Description = "Retrieves the profile of the currently authenticated user.")]
    [OpenApiSecurity("bearer_auth", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(UserDto), Description = "Current user profile")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "User not authenticated")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "User not found")]
    public async Task<HttpResponseData> GetCurrentUser(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users/me")] HttpRequestData req,
        FunctionContext context)
    {
        try
        {
            // Get user ID from authentication context
            var userId = context.GetUserId();
            if (userId == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, "User not authenticated");
            }

            var user = await _userService.GetByIdAsync(userId.Value);
            if (user == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.NotFound, "User not found");
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(user);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "An error occurred while retrieving the current user");
        }
    }

    /// <summary>
    /// Creates a new user.
    /// </summary>
    /// <param name="req">HTTP request.</param>
    /// <param name="context">Function context.</param>
    /// <returns>Created user details.</returns>
    [Function("CreateUser")]
    [OpenApiOperation(operationId: "CreateUser", tags: new[] { "Users" }, Summary = "Create new user", Description = "Creates a new user account. Committee members only.")]
    [OpenApiSecurity("bearer_auth", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(CreateUserRequest), Required = true, Description = "User creation details")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(UserDto), Description = "User created successfully")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "Invalid request")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "User not authenticated")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Forbidden, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "User does not have Committee role")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Conflict, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "User with this email already exists")]
    public async Task<HttpResponseData> CreateUser(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "users")] HttpRequestData req,
        FunctionContext context)
    {
        try
        {
            // Only Committee members can create users
            var authResponse = AuthorizationHelper.CheckCommitteeRole(context, req, _logger);
            if (authResponse != null)
            {
                return authResponse;
            }

            var createRequest = await JsonSerializer.DeserializeAsync<CreateUserRequest>(req.Body);
            if (createRequest == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request body");
            }

            var validationResult = await _createUserValidator.ValidateAsync(createRequest);
            if (!validationResult.IsValid)
            {
                return await CreateValidationErrorResponse(req, validationResult.Errors.Select(e => e.ErrorMessage).ToArray());
            }

            var user = await _userService.CreateAsync(createRequest);

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(user);
            response.Headers.Add("Location", $"/api/users/{user.Id}");
            return response;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("email already exists"))
        {
            _logger.LogWarning(ex, "Attempt to create user with existing email");
            return await CreateErrorResponse(req, HttpStatusCode.Conflict, "User with this email already exists");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "An error occurred while creating the user");
        }
    }

    /// <summary>
    /// Updates an existing user.
    /// </summary>
    /// <param name="req">HTTP request.</param>
    /// <param name="id">User ID to update.</param>
    /// <param name="context">Function context.</param>
    /// <returns>Updated user details.</returns>
    [Function("UpdateUser")]
    [OpenApiOperation(operationId: "UpdateUser", tags: new[] { "Users" }, Summary = "Update user", Description = "Updates an existing user. Users can update their own profile; Committee members can update any user.")]
    [OpenApiSecurity("bearer_auth", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT")]
    [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "User ID (GUID)")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(UpdateUserRequest), Required = true, Description = "User update details")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(UserDto), Description = "User updated successfully")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "Invalid request")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "User not authenticated")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Forbidden, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "User cannot update this profile")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "User not found")]
    public async Task<HttpResponseData> UpdateUser(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "users/{id}")] HttpRequestData req,
        string id,
        FunctionContext context)
    {
        try
        {
            if (!Guid.TryParse(id, out var userId))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid user ID format");
            }

            // Users can update their own profile, Committee can update anyone
            var authResponse = AuthorizationHelper.CheckSelfOrCommittee(context, req, userId, _logger);
            if (authResponse != null)
            {
                return authResponse;
            }

            var updateRequest = await JsonSerializer.DeserializeAsync<UpdateUserRequest>(req.Body);
            if (updateRequest == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request body");
            }

            var validationResult = await _updateUserValidator.ValidateAsync(updateRequest);
            if (!validationResult.IsValid)
            {
                return await CreateValidationErrorResponse(req, validationResult.Errors.Select(e => e.ErrorMessage).ToArray());
            }

            var user = await _userService.UpdateAsync(userId, updateRequest);
            if (user == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.NotFound, "User not found");
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(user);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", id);
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "An error occurred while updating the user");
        }
    }

    /// <summary>
    /// Soft deletes a user (marks as inactive).
    /// </summary>
    /// <param name="req">HTTP request.</param>
    /// <param name="id">User ID to delete.</param>
    /// <param name="context">Function context.</param>
    /// <returns>Success response.</returns>
    [Function("DeleteUser")]
    [OpenApiOperation(operationId: "DeleteUser", tags: new[] { "Users" }, Summary = "Delete user", Description = "Soft deletes a user by marking them as inactive. Committee members only.")]
    [OpenApiSecurity("bearer_auth", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT")]
    [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "User ID (GUID)")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NoContent, Description = "User deleted successfully")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "Invalid user ID format")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "User not authenticated")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Forbidden, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "User does not have Committee role")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "User not found")]
    public async Task<HttpResponseData> DeleteUser(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "users/{id}")] HttpRequestData req,
        string id,
        FunctionContext context)
    {
        try
        {
            if (!Guid.TryParse(id, out var userId))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid user ID format");
            }

            // Only Committee members can delete users
            var authResponse = AuthorizationHelper.CheckCommitteeRole(context, req, _logger);
            if (authResponse != null)
            {
                return authResponse;
            }

            var result = await _userService.DeleteAsync(userId);
            if (!result)
            {
                return await CreateErrorResponse(req, HttpStatusCode.NotFound, "User not found");
            }

            var response = req.CreateResponse(HttpStatusCode.NoContent);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", id);
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "An error occurred while deleting the user");
        }
    }

    private static async Task<HttpResponseData> CreateErrorResponse(HttpRequestData req, HttpStatusCode statusCode, string message)
    {
        var response = req.CreateResponse(statusCode);
        await response.WriteAsJsonAsync(new ErrorResponse
        {
            Message = message,
            Code = statusCode.ToString().ToUpperInvariant(),
        });
        return response;
    }

    private static async Task<HttpResponseData> CreateValidationErrorResponse(HttpRequestData req, string[] errors)
    {
        var response = req.CreateResponse(HttpStatusCode.BadRequest);
        var validationErrors = new Dictionary<string, string[]>
        {
            ["_general"] = errors,
        };
        
        await response.WriteAsJsonAsync(new ErrorResponse
        {
            Message = "Validation failed",
            Code = "VALIDATION_ERROR",
            ValidationErrors = validationErrors,
        });
        return response;
    }
}
