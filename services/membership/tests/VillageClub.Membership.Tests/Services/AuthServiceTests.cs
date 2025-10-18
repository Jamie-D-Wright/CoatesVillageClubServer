using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using VillageClub.Auth.Services;
using VillageClub.Contracts.Auth;
using VillageClub.Contracts.Enums;
using VillageClub.Membership.Data;
using VillageClub.Membership.Data.Entities;
using VillageClub.Membership.Models;
using VillageClub.Membership.Services;
using Xunit;

namespace VillageClub.Membership.Tests.Services;

public class AuthServiceTests : IDisposable
{
    private readonly MembershipDbContext _context;
    private readonly Mock<IJwtTokenService> _jwtTokenServiceMock;
    private readonly Mock<IPasswordHashService> _passwordHashServiceMock;
    private readonly Mock<IUserService> _userServiceMock;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<MembershipDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new MembershipDbContext(options);

        // Setup mocks
        _jwtTokenServiceMock = new Mock<IJwtTokenService>();
        _passwordHashServiceMock = new Mock<IPasswordHashService>();
        _userServiceMock = new Mock<IUserService>();

        _sut = new AuthService(
            _context,
            _jwtTokenServiceMock.Object,
            _passwordHashServiceMock.Object,
            _userServiceMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region LoginAsync Tests

    [Fact]
    public async Task LoginAsync_ShouldReturnAuthResponse_WhenCredentialsAreValid()
    {
        // Arrange
        var user = CreateTestUser("test@example.com", "hashedPassword", UserRole.Member);
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var loginRequest = new LoginRequest
        {
            Email = "test@example.com",
            Password = "ValidPassword123!",
        };

        _passwordHashServiceMock
            .Setup(x => x.VerifyPassword("ValidPassword123!", "hashedPassword"))
            .Returns(true);

        _jwtTokenServiceMock
            .Setup(x => x.GenerateAccessToken(user.Id, user.Email, user.Role.ToString(), null))
            .Returns("access_token");

        _jwtTokenServiceMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns("refresh_token");

        _userServiceMock
            .Setup(x => x.ToDto(It.IsAny<User>()))
            .Returns(new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role,
            });

        // Act
        var result = await _sut.LoginAsync(loginRequest);

        // Assert
        result.Should().NotBeNull();
        result!.AccessToken.Should().Be("access_token");
        result.RefreshToken.Should().Be("refresh_token");
        result.User.Should().NotBeNull();
        result.User.Email.Should().Be("test@example.com");

        // Verify refresh token was saved
        var savedToken = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == "refresh_token");
        savedToken.Should().NotBeNull();
        savedToken!.UserId.Should().Be(user.Id);

        // Verify LastLoginAt was updated
        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser!.LastLoginAt.Should().NotBeNull();
        updatedUser.LastLoginAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnNull_WhenEmailDoesNotExist()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "Password123!",
        };

        // Act
        var result = await _sut.LoginAsync(loginRequest);

        // Assert
        result.Should().BeNull();
        _passwordHashServiceMock.Verify(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnNull_WhenPasswordIsIncorrect()
    {
        // Arrange
        var user = CreateTestUser("test@example.com", "hashedPassword", UserRole.Member);
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var loginRequest = new LoginRequest
        {
            Email = "test@example.com",
            Password = "WrongPassword123!",
        };

        _passwordHashServiceMock
            .Setup(x => x.VerifyPassword("WrongPassword123!", "hashedPassword"))
            .Returns(false);

        // Act
        var result = await _sut.LoginAsync(loginRequest);

        // Assert
        result.Should().BeNull();
        _jwtTokenServiceMock.Verify(x => x.GenerateAccessToken(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnNull_WhenUserIsInactive()
    {
        // Arrange
        var user = CreateTestUser("inactive@example.com", "hashedPassword", UserRole.Member);
        user.Status = UserStatus.Inactive;
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var loginRequest = new LoginRequest
        {
            Email = "inactive@example.com",
            Password = "ValidPassword123!",
        };

        _passwordHashServiceMock
            .Setup(x => x.VerifyPassword("ValidPassword123!", "hashedPassword"))
            .Returns(true);

        // Act
        var result = await _sut.LoginAsync(loginRequest);

        // Assert
        result.Should().BeNull();
        _jwtTokenServiceMock.Verify(x => x.GenerateAccessToken(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_ShouldIncludeCommitteeRole_WhenUserIsCommittee()
    {
        // Arrange
        var user = CreateTestUser("committee@example.com", "hashedPassword", UserRole.Committee);
        user.CommitteeRole = CommitteeRole.Treasurer;
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var loginRequest = new LoginRequest
        {
            Email = "committee@example.com",
            Password = "ValidPassword123!",
        };

        _passwordHashServiceMock
            .Setup(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        _jwtTokenServiceMock
            .Setup(x => x.GenerateAccessToken(user.Id, user.Email, user.Role.ToString(), CommitteeRole.Treasurer.ToString()))
            .Returns("access_token");

        _jwtTokenServiceMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns("refresh_token");

        _userServiceMock
            .Setup(x => x.ToDto(It.IsAny<User>()))
            .Returns(new UserDto { Id = user.Id, Email = user.Email, Role = user.Role });

        // Act
        var result = await _sut.LoginAsync(loginRequest);

        // Assert
        result.Should().NotBeNull();
        _jwtTokenServiceMock.Verify(x => x.GenerateAccessToken(
            user.Id,
            user.Email,
            UserRole.Committee.ToString(),
            CommitteeRole.Treasurer.ToString()), Times.Once);
    }

    #endregion

    #region RegisterAsync Tests

    [Fact]
    public async Task RegisterAsync_ShouldCreateUserAndReturnAuthResponse()
    {
        // Arrange
        var registerRequest = new CreateUserRequest
        {
            Email = "newuser@example.com",
            Password = "SecurePassword123!",
            FirstName = "John",
            LastName = "Doe",
            Role = UserRole.Member,
        };

        _passwordHashServiceMock
            .Setup(x => x.HashPassword("SecurePassword123!"))
            .Returns("hashedPassword");

        _jwtTokenServiceMock
            .Setup(x => x.GenerateAccessToken(It.IsAny<Guid>(), "newuser@example.com", UserRole.Member.ToString(), null))
            .Returns("access_token");

        _jwtTokenServiceMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns("refresh_token");

        _userServiceMock
            .Setup(x => x.ToDto(It.IsAny<User>()))
            .Returns(new UserDto
            {
                Email = "newuser@example.com",
                FirstName = "John",
                LastName = "Doe",
                Role = UserRole.Member,
            });

        // Act
        var result = await _sut.RegisterAsync(registerRequest);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("access_token");
        result.RefreshToken.Should().Be("refresh_token");
        result.User.Should().NotBeNull();
        result.User.Email.Should().Be("newuser@example.com");

        // Verify user was created
        var createdUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "newuser@example.com");
        createdUser.Should().NotBeNull();
        createdUser!.PasswordHash.Should().Be("hashedPassword");
        createdUser.Status.Should().Be(UserStatus.Active);
        createdUser.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify refresh token was saved
        var refreshToken = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == "refresh_token");
        refreshToken.Should().NotBeNull();
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrowException_WhenEmailAlreadyExists()
    {
        // Arrange
        var existingUser = CreateTestUser("existing@example.com", "hashedPassword", UserRole.Member);
        await _context.Users.AddAsync(existingUser);
        await _context.SaveChangesAsync();

        var registerRequest = new CreateUserRequest
        {
            Email = "existing@example.com",
            Password = "Password123!",
            FirstName = "Jane",
            LastName = "Doe",
            Role = UserRole.Member,
        };

        // Act
        var act = async () => await _sut.RegisterAsync(registerRequest);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task RegisterAsync_ShouldCreateAuditLog()
    {
        // Arrange
        var registerRequest = new CreateUserRequest
        {
            Email = "newuser@example.com",
            Password = "SecurePassword123!",
            FirstName = "John",
            LastName = "Doe",
            Role = UserRole.Volunteer,
        };

        _passwordHashServiceMock
            .Setup(x => x.HashPassword(It.IsAny<string>()))
            .Returns("hashedPassword");

        _jwtTokenServiceMock
            .Setup(x => x.GenerateAccessToken(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("access_token");

        _jwtTokenServiceMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns("refresh_token");

        _userServiceMock
            .Setup(x => x.ToDto(It.IsAny<User>()))
            .Returns(new UserDto { Email = "newuser@example.com", Role = UserRole.Volunteer });

        // Act
        await _sut.RegisterAsync(registerRequest);

        // Assert - Verify audit log was created via mock
        _userServiceMock.Verify(
            x => x.LogAuditAsync("User registered", It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
    }

    #endregion

    #region RefreshTokenAsync Tests

    [Fact]
    public async Task RefreshTokenAsync_ShouldReturnNewTokens_WhenRefreshTokenIsValid()
    {
        // Arrange
        var user = CreateTestUser("test@example.com", "hashedPassword", UserRole.Member);
        await _context.Users.AddAsync(user);

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            User = user,
            Token = "valid_refresh_token",
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow,
        };
        await _context.RefreshTokens.AddAsync(refreshToken);
        await _context.SaveChangesAsync();

        var request = new RefreshTokenRequest { RefreshToken = "valid_refresh_token" };

        _jwtTokenServiceMock
            .Setup(x => x.GenerateAccessToken(user.Id, user.Email, user.Role.ToString(), null))
            .Returns("new_access_token");

        _jwtTokenServiceMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns("new_refresh_token");

        _userServiceMock
            .Setup(x => x.ToDto(It.IsAny<User>()))
            .Returns(new UserDto { Id = user.Id, Email = user.Email, Role = user.Role });

        // Act
        var result = await _sut.RefreshTokenAsync(request);

        // Assert
        result.Should().NotBeNull();
        result!.AccessToken.Should().Be("new_access_token");
        result.RefreshToken.Should().Be("new_refresh_token");

        // Verify old token was revoked
        var oldToken = await _context.RefreshTokens.FindAsync(refreshToken.Id);
        oldToken!.RevokedAt.Should().NotBeNull();
        oldToken.ReplacedByToken.Should().Be("new_refresh_token");

        // Verify new token was saved
        var newToken = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == "new_refresh_token");
        newToken.Should().NotBeNull();
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldReturnNull_WhenRefreshTokenDoesNotExist()
    {
        // Arrange
        var request = new RefreshTokenRequest { RefreshToken = "nonexistent_token" };

        // Act
        var result = await _sut.RefreshTokenAsync(request);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldReturnNull_WhenRefreshTokenIsExpired()
    {
        // Arrange
        var user = CreateTestUser("test@example.com", "hashedPassword", UserRole.Member);
        await _context.Users.AddAsync(user);

        var expiredToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "expired_token",
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-31),
        };
        await _context.RefreshTokens.AddAsync(expiredToken);
        await _context.SaveChangesAsync();

        var request = new RefreshTokenRequest { RefreshToken = "expired_token" };

        // Act
        var result = await _sut.RefreshTokenAsync(request);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldReturnNull_WhenRefreshTokenIsRevoked()
    {
        // Arrange
        var user = CreateTestUser("test@example.com", "hashedPassword", UserRole.Member);
        await _context.Users.AddAsync(user);

        var revokedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "revoked_token",
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow,
            RevokedAt = DateTime.UtcNow.AddHours(-1),
        };
        await _context.RefreshTokens.AddAsync(revokedToken);
        await _context.SaveChangesAsync();

        var request = new RefreshTokenRequest { RefreshToken = "revoked_token" };

        // Act
        var result = await _sut.RefreshTokenAsync(request);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region RevokeTokenAsync Tests

    [Fact]
    public async Task RevokeTokenAsync_ShouldRevokeToken_WhenTokenExists()
    {
        // Arrange
        var user = CreateTestUser("test@example.com", "hashedPassword", UserRole.Member);
        await _context.Users.AddAsync(user);

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "token_to_revoke",
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow,
        };
        await _context.RefreshTokens.AddAsync(refreshToken);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.RevokeTokenAsync("token_to_revoke");

        // Assert
        result.Should().BeTrue();

        var revokedToken = await _context.RefreshTokens.FindAsync(refreshToken.Id);
        revokedToken!.RevokedAt.Should().NotBeNull();
        revokedToken.RevokedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task RevokeTokenAsync_ShouldReturnFalse_WhenTokenDoesNotExist()
    {
        // Act
        var result = await _sut.RevokeTokenAsync("nonexistent_token");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region ChangePasswordAsync Tests

    [Fact]
    public async Task ChangePasswordAsync_ShouldChangePassword_WhenCurrentPasswordIsCorrect()
    {
        // Arrange
        var user = CreateTestUser("test@example.com", "old_hashed_password", UserRole.Member);
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var request = new ChangePasswordRequest
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmPassword = "NewPassword123!",
        };

        _passwordHashServiceMock
            .Setup(x => x.VerifyPassword("OldPassword123!", "old_hashed_password"))
            .Returns(true);

        _passwordHashServiceMock
            .Setup(x => x.HashPassword("NewPassword123!"))
            .Returns("new_hashed_password");

        // Act
        var result = await _sut.ChangePasswordAsync(user.Id, request);

        // Assert
        result.Should().BeTrue();

        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser!.PasswordHash.Should().Be("new_hashed_password");
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldReturnFalse_WhenCurrentPasswordIsIncorrect()
    {
        // Arrange
        var user = CreateTestUser("test@example.com", "hashed_password", UserRole.Member);
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var request = new ChangePasswordRequest
        {
            CurrentPassword = "WrongPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmPassword = "NewPassword123!",
        };

        _passwordHashServiceMock
            .Setup(x => x.VerifyPassword("WrongPassword123!", "hashed_password"))
            .Returns(false);

        // Act
        var result = await _sut.ChangePasswordAsync(user.Id, request);

        // Assert
        result.Should().BeFalse();

        var unchangedUser = await _context.Users.FindAsync(user.Id);
        unchangedUser!.PasswordHash.Should().Be("hashed_password");
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldCreateAuditLog_WhenPasswordChanged()
    {
        // Arrange
        var user = CreateTestUser("test@example.com", "old_password", UserRole.Member);
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var request = new ChangePasswordRequest
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmPassword = "NewPassword123!",
        };

        _passwordHashServiceMock
            .Setup(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        _passwordHashServiceMock
            .Setup(x => x.HashPassword(It.IsAny<string>()))
            .Returns("new_password");

        // Act
        await _sut.ChangePasswordAsync(user.Id, request);

        // Assert - Verify audit log was created via mock
        _userServiceMock.Verify(
            x => x.LogAuditAsync("Password changed", user.Id, user.Id, It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
    }

    #endregion

    #region Helper Methods

    private static User CreateTestUser(string email, string passwordHash, UserRole role)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = passwordHash,
            FirstName = "Test",
            LastName = "User",
            Role = role,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
        };
    }

    #endregion
}
