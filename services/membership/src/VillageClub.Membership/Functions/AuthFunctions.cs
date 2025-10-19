using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using VillageClub.Contracts.Auth;
using VillageClub.Contracts.Models;
using VillageClub.Membership.Middleware;
using VillageClub.Membership.Models;
using VillageClub.Membership.Services;

namespace VillageClub.Membership.Functions;

/// <summary>
/// Authentication functions for user login, registration, token management, and password changes.
/// </summary>
public class AuthFunctions
{
    private readonly ILogger<AuthFunctions> _logger;
    private readonly IAuthService _authService;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<CreateUserRequest> _registerValidator;
    private readonly IValidator<RefreshTokenRequest> _refreshTokenValidator;
    private readonly IValidator<ChangePasswordRequest> _changePasswordValidator;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthFunctions"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="authService">The authentication service.</param>
    /// <param name="loginValidator">The login request validator.</param>
    /// <param name="registerValidator">The registration request validator.</param>
    /// <param name="refreshTokenValidator">The refresh token request validator.</param>
    /// <param name="changePasswordValidator">The change password request validator.</param>
    public AuthFunctions(
        ILogger<AuthFunctions> logger,
        IAuthService authService,
        IValidator<LoginRequest> loginValidator,
        IValidator<CreateUserRequest> registerValidator,
        IValidator<RefreshTokenRequest> refreshTokenValidator,
        IValidator<ChangePasswordRequest> changePasswordValidator)
    {
        _logger = logger;
        _authService = authService;
        _loginValidator = loginValidator;
        _registerValidator = registerValidator;
        _refreshTokenValidator = refreshTokenValidator;
        _changePasswordValidator = changePasswordValidator;
    }

    /// <summary>
    /// Authenticates a user and returns a JWT token.
    /// </summary>
    /// <param name="req">The HTTP request containing login credentials.</param>
    /// <returns>Login response with JWT access token and refresh token.</returns>
    [Function("Login")]
    [OpenApiOperation(operationId: "Login", tags: new[] { "Authentication" }, Summary = "User login", Description = "Authenticates a user with email and password, returning JWT tokens.")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(LoginRequest), Required = true, Description = "Login credentials")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(AuthResponse), Description = "Login successful")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "Invalid request")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "Invalid credentials")]
    public async Task<HttpResponseData> Login(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/login")] HttpRequestData req)
    {
        try
        {
            var loginRequest = await JsonSerializer.DeserializeAsync<LoginRequest>(req.Body);
            if (loginRequest == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request body");
            }

            var validationResult = await _loginValidator.ValidateAsync(loginRequest);
            if (!validationResult.IsValid)
            {
                return await CreateValidationErrorResponse(req, validationResult.Errors.Select(e => e.ErrorMessage).ToArray());
            }

            var result = await _authService.LoginAsync(loginRequest);
            if (result == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, "Invalid email or password");
            }

            return await CreateSuccessResponse(req, result, HttpStatusCode.OK);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "An error occurred during login");
        }
    }

    /// <summary>
    /// Registers a new user account.
    /// </summary>
    /// <param name="req">The HTTP request containing registration details.</param>
    /// <returns>Authentication response with JWT tokens for the newly created user.</returns>
    [Function("Register")]
    [OpenApiOperation(operationId: "Register", tags: new[] { "Authentication" }, Summary = "Register new user", Description = "Creates a new user account and returns JWT tokens.")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(CreateUserRequest), Required = true, Description = "User registration details")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(AuthResponse), Description = "User created successfully")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "Invalid request or user already exists")]
    public async Task<HttpResponseData> Register(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/register")] HttpRequestData req)
    {
        try
        {
            var registerRequest = await JsonSerializer.DeserializeAsync<CreateUserRequest>(req.Body);
            if (registerRequest == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request body");
            }

            var validationResult = await _registerValidator.ValidateAsync(registerRequest);
            if (!validationResult.IsValid)
            {
                return await CreateValidationErrorResponse(req, validationResult.Errors.Select(e => e.ErrorMessage).ToArray());
            }

            var result = await _authService.RegisterAsync(registerRequest);
            return await CreateSuccessResponse(req, result, HttpStatusCode.Created);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Registration failed");
            return await CreateErrorResponse(req, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "An error occurred during registration");
        }
    }

    /// <summary>
    /// Refreshes an expired access token using a valid refresh token.
    /// </summary>
    /// <param name="req">The HTTP request containing the refresh token.</param>
    /// <returns>New JWT tokens.</returns>
    [Function("RefreshToken")]
    [OpenApiOperation(operationId: "RefreshToken", tags: new[] { "Authentication" }, Summary = "Refresh access token", Description = "Obtains a new access token using a valid refresh token.")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(RefreshTokenRequest), Required = true, Description = "Refresh token")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(AuthResponse), Description = "Token refreshed successfully")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "Invalid request")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "Invalid or expired refresh token")]
    public async Task<HttpResponseData> RefreshToken(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/refresh")] HttpRequestData req)
    {
        try
        {
            var refreshRequest = await JsonSerializer.DeserializeAsync<RefreshTokenRequest>(req.Body);
            if (refreshRequest == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request body");
            }

            var validationResult = await _refreshTokenValidator.ValidateAsync(refreshRequest);
            if (!validationResult.IsValid)
            {
                return await CreateValidationErrorResponse(req, validationResult.Errors.Select(e => e.ErrorMessage).ToArray());
            }

            var result = await _authService.RefreshTokenAsync(refreshRequest);
            if (result == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, "Invalid or expired refresh token");
            }

            return await CreateSuccessResponse(req, result, HttpStatusCode.OK);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "An error occurred during token refresh");
        }
    }

    /// <summary>
    /// Logs out a user by revoking their refresh token.
    /// </summary>
    /// <param name="req">The HTTP request containing the refresh token to revoke.</param>
    /// <returns>No content response.</returns>
    [Function("Logout")]
    [OpenApiOperation(operationId: "Logout", tags: new[] { "Authentication" }, Summary = "User logout", Description = "Revokes a refresh token to log out the user.")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(RefreshTokenRequest), Required = true, Description = "Refresh token to revoke")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NoContent, Description = "Logout successful")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "Invalid request")]
    public async Task<HttpResponseData> Logout(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/logout")] HttpRequestData req)
    {
        try
        {
            var refreshRequest = await JsonSerializer.DeserializeAsync<RefreshTokenRequest>(req.Body);
            if (refreshRequest == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request body");
            }

            var validationResult = await _refreshTokenValidator.ValidateAsync(refreshRequest);
            if (!validationResult.IsValid)
            {
                return await CreateValidationErrorResponse(req, validationResult.Errors.Select(e => e.ErrorMessage).ToArray());
            }

            await _authService.RevokeTokenAsync(refreshRequest.RefreshToken);
            
            var response = req.CreateResponse(HttpStatusCode.NoContent);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "An error occurred during logout");
        }
    }

    /// <summary>
    /// Changes the password for the authenticated user.
    /// </summary>
    /// <param name="req">The HTTP request containing the current and new passwords.</param>
    /// <param name="context">Function context containing authenticated user information.</param>
    /// <returns>No content response on success.</returns>
    [Function("ChangePassword")]
    [OpenApiOperation(operationId: "ChangePassword", tags: new[] { "Authentication" }, Summary = "Change password", Description = "Changes the password for the currently authenticated user.")]
    [OpenApiSecurity("bearer_auth", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(ChangePasswordRequest), Required = true, Description = "Current and new password")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NoContent, Description = "Password changed successfully")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "Invalid request or incorrect current password")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Unauthorized, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "User not authenticated")]
    public async Task<HttpResponseData> ChangePassword(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/change-password")] HttpRequestData req,
        FunctionContext context)
    {
        try
        {
            // Get user ID from authentication context (set by JWT middleware)
            var userId = context.GetUserId();
            if (userId == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, "User not authenticated");
            }

            var changePasswordRequest = await JsonSerializer.DeserializeAsync<ChangePasswordRequest>(req.Body);
            if (changePasswordRequest == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request body");
            }

            var validationResult = await _changePasswordValidator.ValidateAsync(changePasswordRequest);
            if (!validationResult.IsValid)
            {
                return await CreateValidationErrorResponse(req, validationResult.Errors.Select(e => e.ErrorMessage).ToArray());
            }

            var success = await _authService.ChangePasswordAsync(userId.Value, changePasswordRequest);

            if (!success)
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Current password is incorrect");
            }

            var response = req.CreateResponse(HttpStatusCode.NoContent);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password change");
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "An error occurred during password change");
        }
    }

    private static async Task<HttpResponseData> CreateSuccessResponse<T>(HttpRequestData req, T data, HttpStatusCode statusCode)
    {
        var response = req.CreateResponse(statusCode);
        await response.WriteAsJsonAsync(data);
        return response;
    }

    private static async Task<HttpResponseData> CreateErrorResponse(HttpRequestData req, HttpStatusCode statusCode, string message, string? code = null)
    {
        var response = req.CreateResponse(statusCode);
        await response.WriteAsJsonAsync(new ErrorResponse
        {
            Message = message,
            Code = code ?? statusCode.ToString().ToUpperInvariant(),
            Timestamp = DateTime.UtcNow,
        });
        return response;
    }

    private static async Task<HttpResponseData> CreateValidationErrorResponse(HttpRequestData req, string[] errors)
    {
        var response = req.CreateResponse(HttpStatusCode.BadRequest);
        await response.WriteAsJsonAsync(new ErrorResponse
        {
            Message = "Validation failed",
            Code = "VALIDATION_ERROR",
            ValidationErrors = new Dictionary<string, string[]>
            {
                { "general", errors },
            },
            Timestamp = DateTime.UtcNow,
        });
        return response;
    }
}
