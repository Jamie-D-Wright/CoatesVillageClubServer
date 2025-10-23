using System.Net;
using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VillageClub.Auth.Services;
using VillageClub.Contracts.Auth;
using VillageClub.Contracts.Enums;
using VillageClub.Contracts.Models;
using VillageClub.Membership.Data;
using VillageClub.Membership.Data.Entities;
using VillageClub.Membership.Functions;
using VillageClub.Membership.Models;
using VillageClub.Membership.Services;
using VillageClub.Membership.Validators;

namespace VillageClub.Membership.Tests.Functions;

/// <summary>
/// Integration tests for UserFunctions HTTP endpoints.
/// Tests full request/response cycle including validation, CRUD operations, and error handling.
/// </summary>
public class UserFunctionsTests : FunctionTestBase
{
    private readonly UserFunctions _userFunctions;
    private readonly MembershipDbContext _dbContext;
    private readonly IPasswordHashService _passwordHashService;

    public UserFunctionsTests()
    {
        _dbContext = ServiceProvider.GetRequiredService<MembershipDbContext>();
        _passwordHashService = ServiceProvider.GetRequiredService<IPasswordHashService>();
        _userFunctions = new UserFunctions(
            ServiceProvider.GetRequiredService<ILogger<UserFunctions>>(),
            ServiceProvider.GetRequiredService<IUserService>(),
            ServiceProvider.GetRequiredService<IValidator<CreateUserRequest>>(),
            ServiceProvider.GetRequiredService<IValidator<UpdateUserRequest>>());
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        // Add in-memory database
        services.AddDbContext<MembershipDbContext>(options =>
            options.UseInMemoryDatabase($"UserTestDb_{Guid.NewGuid()}"));

        // Add configuration
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = "ThisIsAVerySecureSecretKeyForTestingPurposesOnly123456789",
                ["Jwt:Issuer"] = "VillageClubTests",
                ["Jwt:Audience"] = "VillageClubTests",
                ["Jwt:ExpiryMinutes"] = "60",
            })
            .Build();

        services.AddSingleton<IConfiguration>(configuration);

        // Add object serializer for WriteAsJsonAsync support
        services.AddSingleton(new TestObjectSerializer());

        // Add services
        services.AddScoped<IPasswordHashService, PasswordHashService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuthService, AuthService>();

        // Add validators
        services.AddScoped<IValidator<CreateUserRequest>, CreateUserRequestValidator>();
        services.AddScoped<IValidator<UpdateUserRequest>, UpdateUserRequestValidator>();

        // Add logging
        services.AddLogging(builder => builder.AddConsole());
    }

    #region GetUsers Tests

    [Fact]
    public async Task GetUsers_ShouldReturnPaginatedList_WhenUsersExist()
    {
        // Arrange
        SetupAuthContext(Guid.NewGuid(), "committee@example.com", "Committee", "Treasurer");
        await SeedTestUsers(15);
        var request = CreateHttpRequest("GET", null, new Dictionary<string, string>
        {
            ["pageNumber"] = "1",
            ["pageSize"] = "10",
        });

        // Act
        var response = await _userFunctions.GetUsers(request, MockFunctionContext.Object);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await ReadResponseBody<PagedResult<UserDto>>(response);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(10);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalCount.Should().Be(15);
        result.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task GetUsers_ShouldReturnSecondPage_WhenPageNumberIsTwo()
    {
        // Arrange
        SetupAuthContext(Guid.NewGuid(), "committee@example.com", "Committee", "Treasurer");
        await SeedTestUsers(15);
        var request = CreateHttpRequest("GET", null, new Dictionary<string, string>
        {
            ["pageNumber"] = "2",
            ["pageSize"] = "10",
        });

        // Act
        var response = await _userFunctions.GetUsers(request, MockFunctionContext.Object);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await ReadResponseBody<PagedResult<UserDto>>(response);
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(5);
        result.Page.Should().Be(2);
        result.TotalCount.Should().Be(15);
    }

    [Fact]
    public async Task GetUsers_ShouldUseDefaultPageSize_WhenPageSizeNotSpecified()
    {
        // Arrange
        SetupAuthContext(Guid.NewGuid(), "committee@example.com", "Committee", "Treasurer");
        await SeedTestUsers(5);
        var request = CreateHttpRequest("GET");

        // Act
        var response = await _userFunctions.GetUsers(request, MockFunctionContext.Object);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await ReadResponseBody<PagedResult<UserDto>>(response);
        result.Should().NotBeNull();
        result!.PageSize.Should().Be(10); // Default page size
    }

    [Fact]
    public async Task GetUsers_ShouldReturnEmptyList_WhenNoUsersExist()
    {
        // Arrange
        SetupAuthContext(Guid.NewGuid(), "committee@example.com", "Committee", "Treasurer");
        var request = CreateHttpRequest("GET");

        // Act
        var response = await _userFunctions.GetUsers(request, MockFunctionContext.Object);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await ReadResponseBody<PagedResult<UserDto>>(response);
        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetUsers_ShouldUseDefaultPageSize_WhenLargerValueRequested()
    {
        // Arrange
        SetupAuthContext(Guid.NewGuid(), "committee@example.com", "Committee", "Treasurer");
        await SeedTestUsers(15);
        var request = CreateHttpRequest("GET", null, new Dictionary<string, string>
        {
            ["pageSize"] = "200",
        });

        // Act
        var response = await _userFunctions.GetUsers(request, MockFunctionContext.Object);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await ReadResponseBody<PagedResult<UserDto>>(response);
        result.Should().NotBeNull();
        result!.PageSize.Should().Be(10); // Defaults to 10 when value is out of range
    }

    #endregion

    #region GetUserById Tests

    [Fact]
    public async Task GetUserById_ShouldReturnUser_WhenUserExists()
    {
        // Arrange
        SetupAuthContext(Guid.NewGuid(), "committee@example.com", "Committee", "Treasurer");
        var user = await CreateTestUserInDb("test@example.com", "Test", "User");
        var request = CreateHttpRequest("GET");

        // Act
        var response = await _userFunctions.GetUserById(request, user.Id.ToString(), MockFunctionContext.Object);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await ReadResponseBody<UserDto>(response);
        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.Email.Should().Be(user.Email);
        result.FirstName.Should().Be(user.FirstName);
        result.LastName.Should().Be(user.LastName);
    }

    [Fact]
    public async Task GetUserById_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        SetupAuthContext(Guid.NewGuid(), "committee@example.com", "Committee", "Treasurer");
        var nonExistentId = Guid.NewGuid();
        var request = CreateHttpRequest("GET");

        // Act
        var response = await _userFunctions.GetUserById(request, nonExistentId.ToString(), MockFunctionContext.Object);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var error = await ReadResponseBody<ErrorResponse>(response);
        error.Should().NotBeNull();
        error!.Message.Should().Be("User not found");
    }

    [Fact]
    public async Task GetUserById_ShouldReturnBadRequest_WhenIdFormatIsInvalid()
    {
        // Arrange
        SetupAuthContext(Guid.NewGuid(), "committee@example.com", "Committee", "Treasurer");
        var request = CreateHttpRequest("GET");

        // Act
        var response = await _userFunctions.GetUserById(request, "invalid-id", MockFunctionContext.Object);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadResponseBody<ErrorResponse>(response);
        error.Should().NotBeNull();
        error!.Message.Should().Be("Invalid user ID format");
    }

    #endregion

    #region GetCurrentUser Tests

    [Fact]
    public async Task GetCurrentUser_ShouldReturnNotImplemented_WhenAuthenticationNotYetImplemented()
    {
        // Arrange
        var user = await CreateTestUserInDb("current@example.com", "Current", "User");
        SetupAuthContext(user.Id, "current@example.com", "Member");
        var request = CreateHttpRequest("GET");

        // Act
        var response = await _userFunctions.GetCurrentUser(request, MockFunctionContext.Object);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await ReadResponseBody<UserDto>(response);
        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.Email.Should().Be(user.Email);
    }

    #endregion

    #region CreateUser Tests

    [Fact]
    public async Task CreateUser_ShouldReturnCreatedUser_WhenRequestIsValid()
    {
        // Arrange
        SetupAuthContext(Guid.NewGuid(), "committee@example.com", "Committee", "Treasurer");
        var createRequest = new CreateUserRequest
        {
            Email = "newuser@example.com",
            Password = "Password123!",
            FirstName = "New",
            LastName = "User",
            Role = UserRole.Member,
        };
        var request = CreateHttpRequest("POST", createRequest);

        // Act
        var response = await _userFunctions.CreateUser(request, MockFunctionContext.Object);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await ReadResponseBody<UserDto>(response);
        result.Should().NotBeNull();
        result!.Email.Should().Be(createRequest.Email);
        result.FirstName.Should().Be(createRequest.FirstName);
        result.LastName.Should().Be(createRequest.LastName);
        result.Role.Should().Be(createRequest.Role);
        result.Id.Should().NotBeEmpty();

        // Verify Location header
        response.Headers.TryGetValues("Location", out var locationValues).Should().BeTrue();
        locationValues.Should().Contain($"/api/users/{result.Id}");
    }

    [Fact]
    public async Task CreateUser_ShouldReturnConflict_WhenEmailAlreadyExists()
    {
        // Arrange
        SetupAuthContext(Guid.NewGuid(), "committee@example.com", "Committee", "Treasurer");
        var email = "existing@example.com";
        await CreateTestUserInDb(email, "Existing", "User");

        var createRequest = new CreateUserRequest
        {
            Email = email,
            Password = "Password123!",
            FirstName = "New",
            LastName = "User",
            Role = UserRole.Member,
        };
        var request = CreateHttpRequest("POST", createRequest);

        // Act
        var response = await _userFunctions.CreateUser(request, MockFunctionContext.Object);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var error = await ReadResponseBody<ErrorResponse>(response);
        error.Should().NotBeNull();
        error!.Message.Should().Be("User with this email already exists");
    }

    [Fact]
    public async Task CreateUser_ShouldReturnBadRequest_WhenEmailIsInvalid()
    {
        // Arrange
        SetupAuthContext(Guid.NewGuid(), "committee@example.com", "Committee", "Treasurer");
        var createRequest = new CreateUserRequest
        {
            Email = "invalid-email",
            Password = "Password123!",
            FirstName = "Test",
            LastName = "User",
            Role = UserRole.Member,
        };
        var request = CreateHttpRequest("POST", createRequest);

        // Act
        var response = await _userFunctions.CreateUser(request, MockFunctionContext.Object);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadResponseBody<ErrorResponse>(response);
        error.Should().NotBeNull();
        error!.Message.Should().Be("Validation failed");
    }

    [Fact]
    public async Task CreateUser_ShouldReturnBadRequest_WhenPasswordIsWeak()
    {
        // Arrange
        SetupAuthContext(Guid.NewGuid(), "committee@example.com", "Committee", "Treasurer");
        var createRequest = new CreateUserRequest
        {
            Email = "test@example.com",
            Password = "weak",
            FirstName = "Test",
            LastName = "User",
            Role = UserRole.Member,
        };
        var request = CreateHttpRequest("POST", createRequest);

        // Act
        var response = await _userFunctions.CreateUser(request, MockFunctionContext.Object);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadResponseBody<ErrorResponse>(response);
        error.Should().NotBeNull();
        error!.Message.Should().Be("Validation failed");
    }

    #endregion

    #region UpdateUser Tests

    [Fact]
    public async Task UpdateUser_ShouldReturnUpdatedUser_WhenRequestIsValid()
    {
        // Arrange
        SetupAuthContext(Guid.NewGuid(), "committee@example.com", "Committee", "Treasurer");
        var user = await CreateTestUserInDb("original@example.com", "Original", "Name");
        var updateRequest = new UpdateUserRequest
        {
            FirstName = "Updated",
            LastName = "User",
            PhoneNumber = "1234567890",
        };
        var request = CreateHttpRequest("PUT", updateRequest);

        // Act
        var response = await _userFunctions.UpdateUser(request, user.Id.ToString(), MockFunctionContext.Object);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await ReadResponseBody<UserDto>(response);
        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.FirstName.Should().Be(updateRequest.FirstName);
        result.LastName.Should().Be(updateRequest.LastName);
        result.PhoneNumber.Should().Be(updateRequest.PhoneNumber);
    }

    [Fact]
    public async Task UpdateUser_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        SetupAuthContext(Guid.NewGuid(), "committee@example.com", "Committee", "Treasurer");
        var nonExistentId = Guid.NewGuid();
        var updateRequest = new UpdateUserRequest
        {
            FirstName = "Updated",
            LastName = "User",
        };
        var request = CreateHttpRequest("PUT", updateRequest);

        // Act
        var response = await _userFunctions.UpdateUser(request, nonExistentId.ToString(), MockFunctionContext.Object);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var error = await ReadResponseBody<ErrorResponse>(response);
        error.Should().NotBeNull();
        error!.Message.Should().Be("User not found");
    }

    [Fact]
    public async Task UpdateUser_ShouldReturnBadRequest_WhenIdFormatIsInvalid()
    {
        // Arrange
        SetupAuthContext(Guid.NewGuid(), "committee@example.com", "Committee", "Treasurer");
        var updateRequest = new UpdateUserRequest
        {
            FirstName = "Updated",
            LastName = "User",
        };
        var request = CreateHttpRequest("PUT", updateRequest);

        // Act
        var response = await _userFunctions.UpdateUser(request, "invalid-id", MockFunctionContext.Object);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadResponseBody<ErrorResponse>(response);
        error.Should().NotBeNull();
        error!.Message.Should().Be("Invalid user ID format");
    }

    #endregion

    #region DeleteUser Tests

    [Fact]
    public async Task DeleteUser_ShouldReturnNoContent_WhenUserExists()
    {
        // Arrange
        SetupAuthContext(Guid.NewGuid(), "committee@example.com", "Committee", "Treasurer");
        var user = await CreateTestUserInDb("delete@example.com", "Delete", "User");
        var request = CreateHttpRequest("DELETE");

        // Act
        var response = await _userFunctions.DeleteUser(request, user.Id.ToString(), MockFunctionContext.Object);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify user is soft deleted
        var deletedUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        deletedUser.Should().NotBeNull();
        deletedUser!.Status.Should().Be(UserStatus.Inactive);
    }

    [Fact]
    public async Task DeleteUser_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        SetupAuthContext(Guid.NewGuid(), "committee@example.com", "Committee", "Treasurer");
        var nonExistentId = Guid.NewGuid();
        var request = CreateHttpRequest("DELETE");

        // Act
        var response = await _userFunctions.DeleteUser(request, nonExistentId.ToString(), MockFunctionContext.Object);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var error = await ReadResponseBody<ErrorResponse>(response);
        error.Should().NotBeNull();
        error!.Message.Should().Be("User not found");
    }

    [Fact]
    public async Task DeleteUser_ShouldReturnBadRequest_WhenIdFormatIsInvalid()
    {
        // Arrange
        SetupAuthContext(Guid.NewGuid(), "committee@example.com", "Committee", "Treasurer");
        var request = CreateHttpRequest("DELETE");

        // Act
        var response = await _userFunctions.DeleteUser(request, "invalid-id", MockFunctionContext.Object);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadResponseBody<ErrorResponse>(response);
        error.Should().NotBeNull();
        error!.Message.Should().Be("Invalid user ID format");
    }

    #endregion

    #region Helper Methods

    private async Task<User> CreateTestUserInDb(string email, string firstName, string lastName)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            PasswordHash = _passwordHashService.HashPassword("Password123!"),
            Role = UserRole.Member,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        return user;
    }

    private async Task SeedTestUsers(int count)
    {
        for (int i = 1; i <= count; i++)
        {
            await CreateTestUserInDb($"user{i}@example.com", $"First{i}", $"Last{i}");
        }
    }

    #endregion
}
