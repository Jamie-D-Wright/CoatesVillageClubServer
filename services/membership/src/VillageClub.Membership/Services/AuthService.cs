using Microsoft.EntityFrameworkCore;
using VillageClub.Auth.Services;
using VillageClub.Contracts.Auth;
using VillageClub.Contracts.Enums;
using VillageClub.Membership.Data;
using VillageClub.Membership.Data.Entities;
using VillageClub.Membership.Models;

namespace VillageClub.Membership.Services;

/// <summary>
/// Service for authentication operations (login, registration, token management).
/// </summary>
public class AuthService : IAuthService
{
    private readonly MembershipDbContext _context;

    private readonly IJwtTokenService _jwtTokenService;

    private readonly IPasswordHashService _passwordHashService;

    private readonly IUserService _userService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthService"/> class.
    /// </summary>
    /// <param name="context">Database context.</param>
    /// <param name="jwtTokenService">JWT token service.</param>
    /// <param name="passwordHashService">Password hash service.</param>
    /// <param name="userService">User service.</param>
    public AuthService(
        MembershipDbContext context,
        IJwtTokenService jwtTokenService,
        IPasswordHashService passwordHashService,
        IUserService userService)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _passwordHashService = passwordHashService;
        _userService = userService;
    }

    /// <summary>
    /// Authenticates a user with email and password.
    /// </summary>
    /// <param name="request">Login request containing email and password.</param>
    /// <returns>Authentication response with tokens and user data, or null if authentication fails.</returns>
    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null || !_passwordHashService.VerifyPassword(request.Password, user.PasswordHash))
        {
            return null;
        }

        if (user.Status != UserStatus.Active)
        {
            return null;
        }

        var accessToken = _jwtTokenService.GenerateAccessToken(
            user.Id,
            user.Email,
            user.Role.ToString(),
            user.CommitteeRole?.ToString());

        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        var accessExpiresAt = DateTime.UtcNow.AddMinutes(15); // Default from JwtTokenService
        var refreshExpiresAt = DateTime.UtcNow.AddDays(30); // Default from JwtTokenService

        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = refreshExpiresAt,
            CreatedAt = DateTime.UtcNow,
        };

        _context.RefreshTokens.Add(refreshTokenEntity);

        user.LastLoginAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _userService.LogAuditAsync("User login", user.Id, user.Id);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = accessExpiresAt,
            User = _userService.ToDto(user),
        };
    }

    /// <summary>
    /// Registers a new user.
    /// </summary>
    /// <param name="request">User registration request.</param>
    /// <returns>Authentication response with tokens and user data.</returns>
    public async Task<AuthResponse> RegisterAsync(CreateUserRequest request)
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

        var accessToken = _jwtTokenService.GenerateAccessToken(
            user.Id,
            user.Email,
            user.Role.ToString(),
            user.CommitteeRole?.ToString());

        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        var accessExpiresAt = DateTime.UtcNow.AddMinutes(15);
        var refreshExpiresAt = DateTime.UtcNow.AddDays(30);

        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = refreshExpiresAt,
            CreatedAt = DateTime.UtcNow,
        };

        _context.RefreshTokens.Add(refreshTokenEntity);

        await _context.SaveChangesAsync();

        await _userService.LogAuditAsync("User registered", user.Id, user.Id);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = accessExpiresAt,
            User = _userService.ToDto(user),
        };
    }

    /// <summary>
    /// Refreshes an access token using a refresh token.
    /// </summary>
    /// <param name="request">Refresh token request.</param>
    /// <returns>New authentication response with refreshed tokens, or null if refresh token is invalid.</returns>
    public async Task<AuthResponse?> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var refreshToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

        if (refreshToken == null || refreshToken.RevokedAt != null || refreshToken.ExpiresAt < DateTime.UtcNow)
        {
            return null;
        }

        var user = refreshToken.User;

        if (user == null || user.Status != UserStatus.Active)
        {
            return null;
        }

        var newAccessToken = _jwtTokenService.GenerateAccessToken(
            user.Id,
            user.Email,
            user.Role.ToString(),
            user.CommitteeRole?.ToString());

        var newRefreshToken = _jwtTokenService.GenerateRefreshToken();
        var accessExpiresAt = DateTime.UtcNow.AddMinutes(15);
        var refreshExpiresAt = DateTime.UtcNow.AddDays(30);

        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.ReplacedByToken = newRefreshToken;

        var newRefreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = newRefreshToken,
            ExpiresAt = refreshExpiresAt,
            CreatedAt = DateTime.UtcNow,
        };

        _context.RefreshTokens.Add(newRefreshTokenEntity);

        await _context.SaveChangesAsync();

        return new AuthResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = accessExpiresAt,
            User = _userService.ToDto(user),
        };
    }

    /// <summary>
    /// Revokes a refresh token (logout).
    /// </summary>
    /// <param name="token">Refresh token to revoke.</param>
    /// <returns>True if token was revoked, false if token was not found.</returns>
    public async Task<bool> RevokeTokenAsync(string token)
    {
        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token);

        if (refreshToken == null)
        {
            return false;
        }

        refreshToken.RevokedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _userService.LogAuditAsync("User logout", refreshToken.UserId, refreshToken.UserId);

        return true;
    }

    /// <summary>
    /// Changes a user's password.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="request">Change password request.</param>
    /// <returns>True if password was changed, false if current password is incorrect.</returns>
    public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
        {
            return false;
        }

        if (!_passwordHashService.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            return false;
        }

        user.PasswordHash = _passwordHashService.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _userService.LogAuditAsync("Password changed", userId, userId);

        return true;
    }

    /// <summary>
    /// Records user login for audit purposes.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <returns>Task.</returns>
    public async Task RecordLoginAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);

        if (user != null)
        {
            user.LastLoginAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }
    }
}
