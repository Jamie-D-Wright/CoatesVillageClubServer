using VillageClub.Contracts.Models;
using VillageClub.Membership.Data.Entities;
using VillageClub.Membership.Models;

namespace VillageClub.Membership.Services;

/// <summary>
/// Service for user management operations (CRUD).
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Gets a user by ID.
    /// </summary>
    /// <param name="id">User ID.</param>
    /// <returns>User DTO or null if not found.</returns>
    Task<UserDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets a user by email.
    /// </summary>
    /// <param name="email">Email address.</param>
    /// <returns>User DTO or null if not found.</returns>
    Task<UserDto?> GetByEmailAsync(string email);

    /// <summary>
    /// Gets a paginated list of users.
    /// </summary>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <returns>Paged result of users.</returns>
    Task<PagedResult<UserDto>> GetAllAsync(int page, int pageSize);

    /// <summary>
    /// Creates a new user.
    /// </summary>
    /// <param name="request">Create user request.</param>
    /// <returns>Created user DTO.</returns>
    Task<UserDto> CreateAsync(CreateUserRequest request);

    /// <summary>
    /// Updates an existing user.
    /// </summary>
    /// <param name="id">User ID.</param>
    /// <param name="request">Update user request.</param>
    /// <returns>Updated user DTO or null if not found.</returns>
    Task<UserDto?> UpdateAsync(Guid id, UpdateUserRequest request);

    /// <summary>
    /// Deletes a user (soft delete by setting status to Inactive).
    /// </summary>
    /// <param name="id">User ID.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteAsync(Guid id);

    /// <summary>
    /// Converts a User entity to a UserDto.
    /// </summary>
    /// <param name="user">User entity.</param>
    /// <returns>User DTO.</returns>
    UserDto ToDto(User user);

    /// <summary>
    /// Logs an audit entry for a user action.
    /// </summary>
    /// <param name="action">Action description.</param>
    /// <param name="actorId">ID of user performing the action.</param>
    /// <param name="targetUserId">ID of user being acted upon (optional).</param>
    /// <param name="details">Additional details (optional).</param>
    /// <param name="ipAddress">IP address of the actor (optional).</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    Task LogAuditAsync(string action, Guid? actorId, Guid? targetUserId = null, string? details = null, string? ipAddress = null);
}
