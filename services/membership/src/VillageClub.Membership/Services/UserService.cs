using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VillageClub.Contracts.Enums;
using VillageClub.Contracts.Models;
using VillageClub.Membership.Data;
using VillageClub.Membership.Data.Entities;
using VillageClub.Membership.Models;

namespace VillageClub.Membership.Services;

/// <summary>
/// Service for user management operations (CRUD).
/// </summary>
public class UserService : IUserService
{
    private readonly MembershipDbContext _context;

    private readonly IPasswordHashService _passwordHashService;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserService"/> class.
    /// </summary>
    /// <param name="context">Database context.</param>
    /// <param name="passwordHashService">Password hash service.</param>
    public UserService(MembershipDbContext context, IPasswordHashService passwordHashService)
    {
        _context = context;
        _passwordHashService = passwordHashService;
    }

    /// <summary>
    /// Gets a user by ID.
    /// </summary>
    /// <param name="id">User ID.</param>
    /// <returns>User DTO or null if not found.</returns>
    public async Task<UserDto?> GetByIdAsync(Guid id)
    {
        var user = await _context.Users.FindAsync(id);

        return user == null ? null : ToDto(user);
    }

    /// <summary>
    /// Gets a user by email.
    /// </summary>
    /// <param name="email">Email address.</param>
    /// <returns>User DTO or null if not found.</returns>
    public async Task<UserDto?> GetByEmailAsync(string email)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email);

        return user == null ? null : ToDto(user);
    }

    /// <summary>
    /// Gets a paginated list of users.
    /// </summary>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <returns>Paged result of users.</returns>
    public async Task<PagedResult<UserDto>> GetAllAsync(int page, int pageSize)
    {
        var query = _context.Users.AsQueryable();

        var totalCount = await query.CountAsync();

        var users = await query
            .OrderBy(u => u.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<UserDto>
        {
            Items = users.Select(ToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }

    /// <summary>
    /// Creates a new user.
    /// </summary>
    /// <param name="request">Create user request.</param>
    /// <returns>Created user DTO.</returns>
    public async Task<UserDto> CreateAsync(CreateUserRequest request)
    {
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (existingUser != null)
        {
            throw new InvalidOperationException("A user with this email already exists");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            PasswordHash = _passwordHashService.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            Address = request.Address,
            Role = request.Role,
            CommitteeRole = request.CommitteeRole,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        await LogAuditAsync("User created", null, user.Id, JsonSerializer.Serialize(new
        {
            user.Email,
            user.Role,
            user.CommitteeRole,
        }));

        return ToDto(user);
    }

    /// <summary>
    /// Updates an existing user.
    /// </summary>
    /// <param name="id">User ID.</param>
    /// <param name="request">Update user request.</param>
    /// <returns>Updated user DTO or null if not found.</returns>
    public async Task<UserDto?> UpdateAsync(Guid id, UpdateUserRequest request)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            return null;
        }

        var changes = new Dictionary<string, string>();

        if (!string.IsNullOrEmpty(request.FirstName) && request.FirstName != user.FirstName)
        {
            changes["FirstName"] = $"{user.FirstName} -> {request.FirstName}";
            user.FirstName = request.FirstName;
        }

        if (!string.IsNullOrEmpty(request.LastName) && request.LastName != user.LastName)
        {
            changes["LastName"] = $"{user.LastName} -> {request.LastName}";
            user.LastName = request.LastName;
        }

        if (request.PhoneNumber != null && request.PhoneNumber != user.PhoneNumber)
        {
            changes["PhoneNumber"] = $"{user.PhoneNumber} -> {request.PhoneNumber}";
            user.PhoneNumber = request.PhoneNumber;
        }

        if (request.Address != null && request.Address != user.Address)
        {
            changes["Address"] = $"{user.Address} -> {request.Address}";
            user.Address = request.Address;
        }

        if (request.Role.HasValue && request.Role.Value != user.Role)
        {
            changes["Role"] = $"{user.Role} -> {request.Role.Value}";
            user.Role = request.Role.Value;
        }

        if (request.CommitteeRole.HasValue && request.CommitteeRole != user.CommitteeRole)
        {
            changes["CommitteeRole"] = $"{user.CommitteeRole} -> {request.CommitteeRole}";
            user.CommitteeRole = request.CommitteeRole;
        }

        if (request.Status.HasValue && request.Status.Value != user.Status)
        {
            changes["Status"] = $"{user.Status} -> {request.Status.Value}";
            user.Status = request.Status.Value;
        }

        if (changes.Count > 0)
        {
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await LogAuditAsync(
                "User updated",
                null,
                id,
                JsonSerializer.Serialize(changes));
        }

        return ToDto(user);
    }

    /// <summary>
    /// Deletes a user (soft delete by setting status to Inactive).
    /// </summary>
    /// <param name="id">User ID.</param>
    /// <returns>True if deleted, false if not found.</returns>
    public async Task<bool> DeleteAsync(Guid id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            return false;
        }

        user.Status = UserStatus.Inactive;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await LogAuditAsync("User deleted (soft)", null, id);

        return true;
    }

    /// <summary>
    /// Converts a User entity to a UserDto.
    /// </summary>
    /// <param name="user">User entity.</param>
    /// <returns>User DTO.</returns>
    public UserDto ToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            Address = user.Address,
            Role = user.Role,
            CommitteeRole = user.CommitteeRole,
            Status = user.Status,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            LastLoginAt = user.LastLoginAt,
        };
    }

    /// <summary>
    /// Logs an audit entry for a user action.
    /// </summary>
    /// <param name="action">Action description.</param>
    /// <param name="actorId">ID of user performing the action.</param>
    /// <param name="targetUserId">ID of user being acted upon (optional).</param>
    /// <param name="details">Additional details (optional).</param>
    /// <param name="ipAddress">IP address of the actor (optional).</param>
    /// <returns>Task.</returns>
    public async Task LogAuditAsync(
        string action,
        Guid? actorId,
        Guid? targetUserId = null,
        string? details = null,
        string? ipAddress = null)
    {
        var auditLog = new AuditLog
        {
            Timestamp = DateTime.UtcNow,
            ActorId = actorId,
            TargetUserId = targetUserId,
            Action = action,
            Details = details,
            IpAddress = ipAddress,
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();
    }
}
