using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using VillageClub.Contracts.Auth;
using VillageClub.Contracts.Models;
using VillageClub.Membership.Models;
using VillageClub.Membership.Services;

namespace VillageClub.Membership.Functions;

public class AuthFunctions
{
    private readonly ILogger<AuthFunctions> _logger;
    private readonly IAuthService _authService;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<CreateUserRequest> _registerValidator;
    private readonly IValidator<RefreshTokenRequest> _refreshTokenValidator;
    private readonly IValidator<ChangePasswordRequest> _changePasswordValidator;

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

    [Function("Login")]
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

    [Function("Register")]
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

    [Function("RefreshToken")]
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

    [Function("Logout")]
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

    [Function("ChangePassword")]
    public async Task<HttpResponseData> ChangePassword(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/change-password")] HttpRequestData req)
    {
        try
        {
            // TODO: Get userId from JWT token claims after implementing JWT middleware
            var userId = Guid.Empty; // Placeholder until JWT middleware is implemented

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

            var success = await _authService.ChangePasswordAsync(userId, changePasswordRequest);

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
