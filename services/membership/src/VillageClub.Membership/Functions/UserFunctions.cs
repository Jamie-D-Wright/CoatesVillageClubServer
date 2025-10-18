using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using VillageClub.Contracts.Models;
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
    /// <returns>Paginated user list.</returns>
    [Function("GetUsers")]
    public async Task<HttpResponseData> GetUsers(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users")] HttpRequestData req)
    {
        try
        {
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
    /// <returns>User details.</returns>
    [Function("GetUserById")]
    public async Task<HttpResponseData> GetUserById(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users/{id}")] HttpRequestData req,
        string id)
    {
        try
        {
            if (!Guid.TryParse(id, out var userId))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid user ID format");
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
    /// <returns>Current user details.</returns>
    [Function("GetCurrentUser")]
    public async Task<HttpResponseData> GetCurrentUser(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users/me")] HttpRequestData req)
    {
        try
        {
            // TODO: Extract user ID from JWT token once authentication middleware is implemented
            // For now, return a placeholder error
            return await CreateErrorResponse(req, HttpStatusCode.NotImplemented, "Authentication not yet implemented");
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
    /// <returns>Created user details.</returns>
    [Function("CreateUser")]
    public async Task<HttpResponseData> CreateUser(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "users")] HttpRequestData req)
    {
        try
        {
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
            if (user == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.Conflict, "User with this email already exists");
            }

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(user);
            response.Headers.Add("Location", $"/api/users/{user.Id}");
            return response;
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
    /// <returns>Updated user details.</returns>
    [Function("UpdateUser")]
    public async Task<HttpResponseData> UpdateUser(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "users/{id}")] HttpRequestData req,
        string id)
    {
        try
        {
            if (!Guid.TryParse(id, out var userId))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid user ID format");
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
    /// <returns>Success response.</returns>
    [Function("DeleteUser")]
    public async Task<HttpResponseData> DeleteUser(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "users/{id}")] HttpRequestData req,
        string id)
    {
        try
        {
            if (!Guid.TryParse(id, out var userId))
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid user ID format");
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
