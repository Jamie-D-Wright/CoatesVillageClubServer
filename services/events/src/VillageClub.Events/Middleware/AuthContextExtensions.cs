using Microsoft.Azure.Functions.Worker;

namespace VillageClub.Events.Middleware;

/// <summary>
/// Helper class to access authenticated user information from function context.
/// </summary>
public static class AuthContextExtensions
{
    /// <summary>
    /// Gets the authenticated user ID from the function context.
    /// </summary>
    /// <param name="context">Function context.</param>
    /// <returns>User ID if authenticated, null otherwise.</returns>
    public static Guid? GetUserId(this FunctionContext context)
    {
        if (context.Items.TryGetValue("UserId", out var userIdObj) && userIdObj is Guid userId)
        {
            return userId;
        }

        return null;
    }

    /// <summary>
    /// Gets the authenticated user email from the function context.
    /// </summary>
    /// <param name="context">Function context.</param>
    /// <returns>User email if authenticated, null otherwise.</returns>
    public static string? GetUserEmail(this FunctionContext context)
    {
        if (context.Items.TryGetValue("Email", out var emailObj) && emailObj is string email)
        {
            return email;
        }

        return null;
    }

    /// <summary>
    /// Gets the authenticated user role from the function context.
    /// </summary>
    /// <param name="context">Function context.</param>
    /// <returns>User role if authenticated, null otherwise.</returns>
    public static string? GetUserRole(this FunctionContext context)
    {
        if (context.Items.TryGetValue("Role", out var roleObj) && roleObj is string role)
        {
            return role;
        }

        return null;
    }

    /// <summary>
    /// Gets the authenticated user committee role from the function context.
    /// </summary>
    /// <param name="context">Function context.</param>
    /// <returns>Committee role if authenticated and applicable, null otherwise.</returns>
    public static string? GetCommitteeRole(this FunctionContext context)
    {
        if (context.Items.TryGetValue("CommitteeRole", out var committeeRoleObj) && committeeRoleObj is string committeeRole)
        {
            return committeeRole;
        }

        return null;
    }

    /// <summary>
    /// Checks if the authenticated user has the specified role.
    /// </summary>
    /// <param name="context">Function context.</param>
    /// <param name="role">Role to check.</param>
    /// <returns>True if user has the role, false otherwise.</returns>
    public static bool HasRole(this FunctionContext context, string role)
    {
        var userRole = context.GetUserRole();
        return userRole != null && userRole.Equals(role, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if the authenticated user is a committee member.
    /// </summary>
    /// <param name="context">Function context.</param>
    /// <returns>True if user is a committee member, false otherwise.</returns>
    public static bool IsCommittee(this FunctionContext context)
    {
        return context.HasRole("Committee");
    }

    /// <summary>
    /// Checks if the authenticated user is a volunteer.
    /// </summary>
    /// <param name="context">Function context.</param>
    /// <returns>True if user is a volunteer, false otherwise.</returns>
    public static bool IsVolunteer(this FunctionContext context)
    {
        return context.HasRole("Volunteer");
    }

    /// <summary>
    /// Checks if the authenticated user is a member.
    /// </summary>
    /// <param name="context">Function context.</param>
    /// <returns>True if user is a member, false otherwise.</returns>
    public static bool IsMember(this FunctionContext context)
    {
        return context.HasRole("Member");
    }
}
