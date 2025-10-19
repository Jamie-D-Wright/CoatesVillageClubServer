using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace VillageClub.Membership.Middleware;

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
    public static HttpResponseData? CheckRole(
        FunctionContext context,
        HttpRequestData request,
        string requiredRole,
        ILogger logger)
    {
        var userRole = context.GetUserRole();
        
        if (userRole == null)
        {
            logger.LogWarning("User role not found in context");
            var response = request.CreateResponse(HttpStatusCode.Unauthorized);
            response.WriteAsJsonAsync(new { error = "User not authenticated" }).GetAwaiter().GetResult();
            return response;
        }

        if (!userRole.Equals(requiredRole, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning(
                "User with role {UserRole} attempted to access endpoint requiring {RequiredRole}",
                userRole,
                requiredRole);
            
            var response = request.CreateResponse(HttpStatusCode.Forbidden);
            response.WriteAsJsonAsync(new { error = $"Access denied. Required role: {requiredRole}" }).GetAwaiter().GetResult();
            return response;
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
    public static HttpResponseData? CheckAnyRole(
        FunctionContext context,
        HttpRequestData request,
        string[] allowedRoles,
        ILogger logger)
    {
        var userRole = context.GetUserRole();
        
        if (userRole == null)
        {
            logger.LogWarning("User role not found in context");
            var response = request.CreateResponse(HttpStatusCode.Unauthorized);
            response.WriteAsJsonAsync(new { error = "User not authenticated" }).GetAwaiter().GetResult();
            return response;
        }

        if (!allowedRoles.Any(role => userRole.Equals(role, StringComparison.OrdinalIgnoreCase)))
        {
            logger.LogWarning(
                "User with role {UserRole} attempted to access endpoint requiring one of: {AllowedRoles}",
                userRole,
                string.Join(", ", allowedRoles));
            
            var response = request.CreateResponse(HttpStatusCode.Forbidden);
            response.WriteAsJsonAsync(new { error = $"Access denied. Required roles: {string.Join(", ", allowedRoles)}" }).GetAwaiter().GetResult();
            return response;
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
    public static HttpResponseData? CheckCommitteeRole(
        FunctionContext context,
        HttpRequestData request,
        ILogger logger)
    {
        return CheckRole(context, request, "Committee", logger);
    }

    /// <summary>
    /// Checks if the current user is the same as the specified user ID or is a Committee member.
    /// </summary>
    /// <param name="context">Function context.</param>
    /// <param name="request">HTTP request.</param>
    /// <param name="userId">User ID to check against.</param>
    /// <param name="logger">Logger.</param>
    /// <returns>Null if authorized, forbidden response if not authorized.</returns>
    public static HttpResponseData? CheckSelfOrCommittee(
        FunctionContext context,
        HttpRequestData request,
        Guid userId,
        ILogger logger)
    {
        var currentUserId = context.GetUserId();
        var isCommittee = context.IsCommittee();

        if (currentUserId == userId || isCommittee)
        {
            return null;
        }

        logger.LogWarning(
            "User {CurrentUserId} attempted to access resource for user {UserId} without proper authorization",
            currentUserId,
            userId);
        
        var response = request.CreateResponse(HttpStatusCode.Forbidden);
        response.WriteAsJsonAsync(new { error = "Access denied. You can only access your own resources or must be a Committee member" }).GetAwaiter().GetResult();
        return response;
    }
}
