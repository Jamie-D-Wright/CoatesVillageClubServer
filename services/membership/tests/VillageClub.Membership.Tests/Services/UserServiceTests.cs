using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using VillageClub.Contracts.Enums;
using VillageClub.Contracts.Models;
using VillageClub.Membership.Data;
using VillageClub.Membership.Data.Entities;
using VillageClub.Membership.Models;
using VillageClub.Membership.Services;
using Xunit;

namespace VillageClub.Membership.Tests.Services;

public class UserServiceTests : IDisposable
{
    private readonly MembershipDbContext _context;
    private readonly Mock<IPasswordHashService> _passwordHashServiceMock;
    private readonly UserService _sut;

    public UserServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<MembershipDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new MembershipDbContext(options);

        // Setup mocks
        _passwordHashServiceMock = new Mock<IPasswordHashService>();

        _sut = new UserService(_context, _passwordHashServiceMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnUser_WhenUserExists()
    {
        // Arrange
        var user = CreateTestUser("test@example.com", UserRole.Member);
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByIdAsync(user.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.Email.Should().Be(user.Email);
        result.FirstName.Should().Be(user.FirstName);
        result.LastName.Should().Be(user.LastName);
        result.Role.Should().Be(user.Role);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenUserDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _sut.GetByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldReturnUser_WhenUserExists()
    {
        // Arrange
        var user = CreateTestUser("test@example.com", UserRole.Member);
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByEmailAsync("test@example.com");

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("test@example.com");
        result.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldReturnNull_WhenUserDoesNotExist()
    {
        // Arrange
        var email = "nonexistent@example.com";

        // Act
        var result = await _sut.GetByEmailAsync(email);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnPagedResults_WithCorrectPagination()
    {
        // Arrange
        for (int i = 1; i <= 25; i++)
        {
            var user = CreateTestUser($"user{i:D2}@example.com", UserRole.Member);
            await _context.Users.AddAsync(user);
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetAllAsync(page: 2, pageSize: 10);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(25);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(10);
        result.Items.Should().HaveCount(10);
        result.Items.First().Email.Should().Be("user11@example.com"); // Ordered by email
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnOrderedByEmail()
    {
        // Arrange
        await _context.Users.AddAsync(CreateTestUser("zebra@example.com", UserRole.Member));
        await _context.Users.AddAsync(CreateTestUser("alpha@example.com", UserRole.Member));
        await _context.Users.AddAsync(CreateTestUser("beta@example.com", UserRole.Member));
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetAllAsync(page: 1, pageSize: 10);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items.ElementAt(0).Email.Should().Be("alpha@example.com");
        result.Items.ElementAt(1).Email.Should().Be("beta@example.com");
        result.Items.ElementAt(2).Email.Should().Be("zebra@example.com");
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnEmptyList_WhenNoUsers()
    {
        // Act
        var result = await _sut.GetAllAsync(page: 1, pageSize: 10);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateUser_AndReturnDto()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Email = "newuser@example.com",
            Password = "Password123!",
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "01234567890",
            Address = "123 Test St",
            Role = UserRole.Volunteer,
            CommitteeRole = null,
        };

        _passwordHashServiceMock
            .Setup(x => x.HashPassword(request.Password))
            .Returns("hashed_password");

        // Act
        var result = await _sut.CreateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be(request.Email);
        result.FirstName.Should().Be(request.FirstName);
        result.LastName.Should().Be(request.LastName);
        result.PhoneNumber.Should().Be(request.PhoneNumber);
        result.Address.Should().Be(request.Address);
        result.Role.Should().Be(request.Role);
        result.Status.Should().Be(UserStatus.Active);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify user was saved to database
        var savedUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        savedUser.Should().NotBeNull();
        savedUser!.PasswordHash.Should().Be("hashed_password");
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowException_WhenEmailAlreadyExists()
    {
        // Arrange
        var existingUser = CreateTestUser("duplicate@example.com", UserRole.Member);
        await _context.Users.AddAsync(existingUser);
        await _context.SaveChangesAsync();

        var request = new CreateUserRequest
        {
            Email = "duplicate@example.com",
            Password = "Password123!",
            FirstName = "Jane",
            LastName = "Doe",
            Role = UserRole.Member,
        };

        // Act
        var act = async () => await _sut.CreateAsync(request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task CreateAsync_ShouldCallPasswordHashService()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Email = "test@example.com",
            Password = "Password123!",
            FirstName = "Test",
            LastName = "User",
            Role = UserRole.Member,
        };

        _passwordHashServiceMock
            .Setup(x => x.HashPassword(request.Password))
            .Returns("hashed_password");

        // Act
        await _sut.CreateAsync(request);

        // Assert
        _passwordHashServiceMock.Verify(
            x => x.HashPassword(request.Password),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateUser_WhenUserExists()
    {
        // Arrange
        var user = CreateTestUser("test@example.com", UserRole.Member);
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var request = new UpdateUserRequest
        {
            FirstName = "UpdatedFirstName",
            LastName = "UpdatedLastName",
            PhoneNumber = "09876543210",
            Address = "456 New St",
        };

        // Act
        var result = await _sut.UpdateAsync(user.Id, request);

        // Assert
        result.Should().NotBeNull();
        result!.FirstName.Should().Be("UpdatedFirstName");
        result.LastName.Should().Be("UpdatedLastName");
        result.PhoneNumber.Should().Be("09876543210");
        result.Address.Should().Be("456 New St");

        // Verify changes persisted
        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser!.FirstName.Should().Be("UpdatedFirstName");
        updatedUser.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnNull_WhenUserDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var request = new UpdateUserRequest
        {
            FirstName = "Test",
        };

        // Act
        var result = await _sut.UpdateAsync(nonExistentId, request);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ShouldOnlyUpdateProvidedFields()
    {
        // Arrange
        var user = CreateTestUser("test@example.com", UserRole.Member);
        user.FirstName = "OriginalFirst";
        user.LastName = "OriginalLast";
        user.PhoneNumber = "01111111111";
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var request = new UpdateUserRequest
        {
            FirstName = "UpdatedFirst",
            // LastName and PhoneNumber not provided
        };

        // Act
        var result = await _sut.UpdateAsync(user.Id, request);

        // Assert
        result.Should().NotBeNull();
        result!.FirstName.Should().Be("UpdatedFirst");
        result.LastName.Should().Be("OriginalLast"); // Should remain unchanged
        result.PhoneNumber.Should().Be("01111111111"); // Should remain unchanged
    }

    [Fact]
    public async Task UpdateAsync_ShouldTrackChanges_InChangesDictionary()
    {
        // Arrange
        var user = CreateTestUser("test@example.com", UserRole.Member);
        user.FirstName = "OldFirst";
        user.LastName = "OldLast";
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var request = new UpdateUserRequest
        {
            FirstName = "NewFirst",
            LastName = "NewLast",
        };

        // Act
        await _sut.UpdateAsync(user.Id, request);

        // Assert - Verify audit log was created with change tracking
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(al => al.Action == "User updated" && al.TargetUserId == user.Id);
        
        auditLog.Should().NotBeNull();
        auditLog!.Details.Should().Contain("FirstName");
        auditLog.Details.Should().Contain("OldFirst");
        auditLog.Details.Should().Contain("NewFirst");
        auditLog.Details.Should().Contain("LastName");
        auditLog.Details.Should().Contain("OldLast");
        auditLog.Details.Should().Contain("NewLast");
    }

    [Fact]
    public async Task DeleteAsync_ShouldSoftDeleteUser_WhenUserExists()
    {
        // Arrange
        var user = CreateTestUser("test@example.com", UserRole.Member);
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.DeleteAsync(user.Id);

        // Assert
        result.Should().BeTrue();

        // Verify user is soft deleted (Status = Inactive)
        var deletedUser = await _context.Users.FindAsync(user.Id);
        deletedUser.Should().NotBeNull();
        deletedUser!.Status.Should().Be(UserStatus.Inactive);
        deletedUser.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenUserDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _sut.DeleteAsync(nonExistentId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ShouldCreateAuditLog()
    {
        // Arrange
        var user = CreateTestUser("test@example.com", UserRole.Member);
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        await _sut.DeleteAsync(user.Id);

        // Assert
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(al => al.Action == "User deleted (soft)" && al.TargetUserId == user.Id);
        
        auditLog.Should().NotBeNull();
        auditLog!.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void ToDto_ShouldMapAllProperties()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = "hashed",
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "01234567890",
            Address = "123 Test St",
            Role = UserRole.Committee,
            CommitteeRole = CommitteeRole.Clerk,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow.AddDays(-1),
            LastLoginAt = DateTime.UtcNow.AddHours(-2),
        };

        // Act
        var result = _sut.ToDto(user);

        // Assert
        result.Id.Should().Be(user.Id);
        result.Email.Should().Be(user.Email);
        result.FirstName.Should().Be(user.FirstName);
        result.LastName.Should().Be(user.LastName);
        result.PhoneNumber.Should().Be(user.PhoneNumber);
        result.Address.Should().Be(user.Address);
        result.Role.Should().Be(user.Role);
        result.CommitteeRole.Should().Be(user.CommitteeRole);
        result.Status.Should().Be(user.Status);
        result.CreatedAt.Should().Be(user.CreatedAt);
        result.UpdatedAt.Should().Be(user.UpdatedAt);
        result.LastLoginAt.Should().Be(user.LastLoginAt);
    }

    [Fact]
    public async Task LogAuditAsync_ShouldCreateAuditLog()
    {
        // Arrange
        var actorId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var action = "Test Action";
        var details = "Test details";
        var ipAddress = "192.168.1.1";

        // Act
        await _sut.LogAuditAsync(action, actorId, targetUserId, details, ipAddress);

        // Assert
        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync();
        auditLog.Should().NotBeNull();
        auditLog!.Action.Should().Be(action);
        auditLog.ActorId.Should().Be(actorId);
        auditLog.TargetUserId.Should().Be(targetUserId);
        auditLog.Details.Should().Be(details);
        auditLog.IpAddress.Should().Be(ipAddress);
        auditLog.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task LogAuditAsync_ShouldAllowNullActorId()
    {
        // Arrange
        var targetUserId = Guid.NewGuid();

        // Act
        await _sut.LogAuditAsync("Test Action", null, targetUserId);

        // Assert
        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync();
        auditLog.Should().NotBeNull();
        auditLog!.ActorId.Should().BeNull();
    }

    private static User CreateTestUser(string email, UserRole role)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = "hashed_password",
            FirstName = "Test",
            LastName = "User",
            PhoneNumber = "01234567890",
            Address = "123 Test St",
            Role = role,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }
}
