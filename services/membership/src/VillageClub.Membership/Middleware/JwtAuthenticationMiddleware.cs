using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using VillageClub.Contracts.Auth;

namespace VillageClub.Membership.Middleware;

/// <summary>
/// Middleware for JWT authentication in Azure Functions.
/// </summary>
public class JwtAuthenticationMiddleware : IFunctionsWorkerMiddleware
{
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<JwtAuthenticationMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="JwtAuthenticationMiddleware"/> class.
    /// </summary>
    /// <param name="jwtTokenService">JWT token service.</param>
    /// <param name="logger">Logger.</param>
    public JwtAuthenticationMiddleware(
        IJwtTokenService jwtTokenService,
        ILogger<JwtAuthenticationMiddleware> logger)
    {
        _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">Function context.</param>
    /// <param name="next">Next middleware.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        // Skip authentication for certain endpoints (health checks, login, etc.)
        var functionName = context.FunctionDefinition.Name;
        if (IsPublicEndpoint(functionName))
        {
            await next(context);
            return;
        }

        // Get the HTTP request data
        var httpRequestData = await context.GetHttpRequestDataAsync();
        if (httpRequestData == null)
        {
            await next(context);
            return;
        }

        // Extract token from Authorization header
        httpRequestData.Headers.TryGetValues("Authorization", out var authHeaderValues);
        var authHeader = authHeaderValues?.FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader))
        {
            _logger.LogWarning("Missing Authorization header");
            await WriteUnauthorizedResponse(httpRequestData, "Missing Authorization header");
            return;
        }

        // Validate Bearer token format
        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Invalid Authorization header format");
            await WriteUnauthorizedResponse(httpRequestData, "Invalid Authorization header format");
            return;
        }

        var token = authHeader.Substring("Bearer ".Length).Trim();

        // Validate the token
        var validationResult = _jwtTokenService.ValidateToken(token);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Token validation failed: {ErrorMessage}", validationResult.ErrorMessage);
            await WriteUnauthorizedResponse(httpRequestData, "Invalid or expired token");
            return;
        }

        // Store user information in context for downstream use
        context.Items["UserId"] = validationResult.UserId!;
        context.Items["Email"] = validationResult.Email!;
        context.Items["Role"] = validationResult.Role!;
        if (validationResult.CommitteeRole != null)
        {
            context.Items["CommitteeRole"] = validationResult.CommitteeRole;
        }

        _logger.LogInformation(
            "User authenticated: {UserId} ({Email}) with role {Role}",
            validationResult.UserId,
            validationResult.Email,
            validationResult.Role);

        await next(context);
    }

    /// <summary>
    /// Determines if the endpoint is public and doesn't require authentication.
    /// </summary>
    /// <param name="functionName">Function name.</param>
    /// <returns>True if public, false otherwise.</returns>
    private static bool IsPublicEndpoint(string functionName)
    {
        var publicEndpoints = new[]
        {
            "HealthCheck",
            "Login",
            "RefreshToken",
            "GetHealth",
        };

        return publicEndpoints.Contains(functionName, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Writes an unauthorized response.
    /// </summary>
    /// <param name="request">HTTP request data.</param>
    /// <param name="message">Error message.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private static async Task WriteUnauthorizedResponse(HttpRequestData request, string message)
    {
        var response = request.CreateResponse(HttpStatusCode.Unauthorized);
        await response.WriteAsJsonAsync(new { error = message });
    }
}
