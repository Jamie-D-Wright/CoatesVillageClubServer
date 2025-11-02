using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using VillageClub.Contracts.Models;

namespace VillageClub.Events.Middleware;

/// <summary>
/// Helper for role-based authorization in Azure Functions.
/// </summary>
public static class AuthorizationHelper
{
    /// <summary>
    /// Checks if the current user has the specified role and returns forbidden response if not.
    /// </summary>
    /// <param name="context">Function context.</param>
    /// <param name="request">HTTP request.</param>
    /// <param name="requiredRole">Required role.</param>
    /// <param name="logger">Logger.</param>
    /// <returns>Null if authorized, forbidden response if not authorized.</returns>
    public static async Task<HttpResponseData?> CheckRole(
        FunctionContext context,
        HttpRequestData request,
        string requiredRole,
        ILogger logger)
    {
        var userRole = context.GetUserRole();

        if (userRole == null)
        {
            logger.LogWarning("User role not found in context");
            return await CreateErrorResponse(request, HttpStatusCode.Unauthorized, "User not authenticated", "UNAUTHORIZED");
        }

        if (!userRole.Equals(requiredRole, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning(
                "User with role {UserRole} attempted to access endpoint requiring {RequiredRole}",
                userRole,
                requiredRole);

            return await CreateErrorResponse(
                request,
                HttpStatusCode.Forbidden,
                $"Access denied. Required role: {requiredRole}",
                "FORBIDDEN");
        }

        return null;
    }

    /// <summary>
    /// Checks if the current user has any of the specified roles and returns forbidden response if not.
    /// </summary>
    /// <param name="context">Function context.</param>
    /// <param name="request">HTTP request.</param>
    /// <param name="allowedRoles">Allowed roles.</param>
    /// <param name="logger">Logger.</param>
    /// <returns>Null if authorized, forbidden response if not authorized.</returns>
    public static async Task<HttpResponseData?> CheckAnyRole(
        FunctionContext context,
        HttpRequestData request,
        string[] allowedRoles,
        ILogger logger)
    {
        var userRole = context.GetUserRole();

        if (userRole == null)
        {
            logger.LogWarning("User role not found in context");
            return await CreateErrorResponse(request, HttpStatusCode.Unauthorized, "User not authenticated", "UNAUTHORIZED");
        }

        if (!allowedRoles.Contains(userRole, StringComparer.OrdinalIgnoreCase))
        {
            logger.LogWarning(
                "User with role {UserRole} attempted to access endpoint requiring one of: {AllowedRoles}",
                userRole,
                string.Join(", ", allowedRoles));

            return await CreateErrorResponse(
                request,
                HttpStatusCode.Forbidden,
                $"Access denied. Required roles: {string.Join(", ", allowedRoles)}",
                "FORBIDDEN");
        }

        return null;
    }

    /// <summary>
    /// Checks if the current user is a Committee member and returns forbidden response if not.
    /// </summary>
    /// <param name="context">Function context.</param>
    /// <param name="request">HTTP request.</param>
    /// <param name="logger">Logger.</param>
    /// <returns>Null if authorized, forbidden response if not authorized.</returns>
    public static async Task<HttpResponseData?> CheckCommitteeRole(
        FunctionContext context,
        HttpRequestData request,
        ILogger logger)
    {
        return await CheckRole(context, request, "Committee", logger);
    }

    /// <summary>
    /// Checks if the user is authenticated (any valid JWT token).
    /// </summary>
    /// <param name="context">Function context.</param>
    /// <param name="request">HTTP request.</param>
    /// <param name="logger">Logger.</param>
    /// <returns>Null if authenticated, unauthorized response if not authenticated.</returns>
    public static async Task<HttpResponseData?> CheckAuthentication(
        FunctionContext context,
        HttpRequestData request,
        ILogger logger)
    {
        var userId = context.GetUserId();

        if (userId == null)
        {
            logger.LogWarning("User ID not found in context - authentication required");
            return await CreateErrorResponse(request, HttpStatusCode.Unauthorized, "User not authenticated", "UNAUTHORIZED");
        }

        return null;
    }

    /// <summary>
    /// Creates a standardized error response.
    /// </summary>
    private static async Task<HttpResponseData> CreateErrorResponse(
        HttpRequestData request,
        HttpStatusCode statusCode,
        string message,
        string code)
    {
        var errorResponse = new ErrorResponse
        {
            Message = message,
            Code = code,
            Timestamp = DateTime.UtcNow,
        };

        var response = request.CreateResponse(statusCode);
        await response.WriteAsJsonAsync(errorResponse);
        return response;
    }
}
