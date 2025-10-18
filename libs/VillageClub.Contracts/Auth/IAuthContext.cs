namespace VillageClub.Contracts.Auth;

/// <summary>
/// Provides access to the current authenticated user's context
/// </summary>
public interface IAuthContext
{
    /// <summary>
    /// Current user's unique identifier
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// Current user's email
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// Current user's role
    /// </summary>
    string? Role { get; }

    /// <summary>
    /// Current user's committee role (if applicable)
    /// </summary>
    string? CommitteeRole { get; }

    /// <summary>
    /// Whether the current user is authenticated
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Whether the current user has the specified role
    /// </summary>
    /// <param name="role">Role to check</param>
    /// <returns>True if user has the role</returns>
    bool IsInRole(string role);

    /// <summary>
    /// Whether the current user is a committee member
    /// </summary>
    /// <returns>True if user is committee member</returns>
    bool IsCommittee();

    /// <summary>
    /// Whether the current user is a volunteer or committee member
    /// </summary>
    /// <returns>True if user is volunteer or committee</returns>
    bool IsVolunteerOrCommittee();
}
