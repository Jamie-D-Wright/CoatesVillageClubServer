using System.Net;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using VillageClub.Membership.Middleware;
using VillageClub.Membership.Tests.Functions;
using Xunit;

namespace VillageClub.Membership.Tests.Middleware;

/// <summary>
/// Tests for authorization helper.
/// </summary>
public class AuthorizationHelperTests
{
    private readonly Mock<ILogger> _mockLogger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizationHelperTests"/> class.
    /// </summary>
    public AuthorizationHelperTests()
    {
        _mockLogger = new Mock<ILogger>();
    }

    /// <summary>
    /// Test CheckRole returns null when user has required role.
    /// </summary>
    [Fact]
    public void CheckRole_WithCorrectRole_ReturnsNull()
    {
        // Arrange
        var context = CreateFunctionContext("Committee");
        var request = CreateHttpRequestData(context);

        // Act
        var result = AuthorizationHelper.CheckRole(context, request, "Committee", _mockLogger.Object);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Test CheckRole returns forbidden when user does not have required role.
    /// </summary>
    [Fact]
    public void CheckRole_WithIncorrectRole_ReturnsForbidden()
    {
        // Arrange
        var context = CreateFunctionContext("Volunteer");
        var request = CreateHttpRequestData(context);

        // Act
        var result = AuthorizationHelper.CheckRole(context, request, "Committee", _mockLogger.Object);

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// Test CheckRole is case insensitive.
    /// </summary>
    [Theory]
    [InlineData("Committee", "committee")]
    [InlineData("Committee", "COMMITTEE")]
    [InlineData("Volunteer", "volunteer")]
    public void CheckRole_IsCaseInsensitive(string userRole, string requiredRole)
    {
        // Arrange
        var context = CreateFunctionContext(userRole);
        var request = CreateHttpRequestData(context);

        // Act
        var result = AuthorizationHelper.CheckRole(context, request, requiredRole, _mockLogger.Object);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Test CheckRole returns unauthorized when user is not authenticated.
    /// </summary>
    [Fact]
    public void CheckRole_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var context = CreateFunctionContext(null);
        var request = CreateHttpRequestData(context);

        // Act
        var result = AuthorizationHelper.CheckRole(context, request, "Committee", _mockLogger.Object);

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Test CheckAnyRole returns null when user has one of the allowed roles.
    /// </summary>
    [Theory]
    [InlineData("Committee", new[] { "Committee", "Volunteer" })]
    [InlineData("Volunteer", new[] { "Committee", "Volunteer" })]
    [InlineData("Member", new[] { "Member", "Volunteer" })]
    public void CheckAnyRole_WithAllowedRole_ReturnsNull(string userRole, string[] allowedRoles)
    {
        // Arrange
        var context = CreateFunctionContext(userRole);
        var request = CreateHttpRequestData(context);

        // Act
        var result = AuthorizationHelper.CheckAnyRole(context, request, allowedRoles, _mockLogger.Object);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Test CheckAnyRole returns forbidden when user does not have any of the allowed roles.
    /// </summary>
    [Fact]
    public void CheckAnyRole_WithoutAllowedRole_ReturnsForbidden()
    {
        // Arrange
        var context = CreateFunctionContext("Member");
        var request = CreateHttpRequestData(context);

        // Act
        var result = AuthorizationHelper.CheckAnyRole(context, request, new[] { "Committee", "Volunteer" }, _mockLogger.Object);

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// Test CheckCommitteeRole returns null when user is committee.
    /// </summary>
    [Fact]
    public void CheckCommitteeRole_WithCommitteeUser_ReturnsNull()
    {
        // Arrange
        var context = CreateFunctionContext("Committee");
        var request = CreateHttpRequestData(context);

        // Act
        var result = AuthorizationHelper.CheckCommitteeRole(context, request, _mockLogger.Object);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Test CheckCommitteeRole returns forbidden when user is not committee.
    /// </summary>
    [Fact]
    public void CheckCommitteeRole_WithNonCommitteeUser_ReturnsForbidden()
    {
        // Arrange
        var context = CreateFunctionContext("Volunteer");
        var request = CreateHttpRequestData(context);

        // Act
        var result = AuthorizationHelper.CheckCommitteeRole(context, request, _mockLogger.Object);

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// Test CheckSelfOrCommittee returns null when accessing own resource.
    /// </summary>
    [Fact]
    public void CheckSelfOrCommittee_AccessingOwnResource_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var context = CreateFunctionContext("Volunteer", userId);
        var request = CreateHttpRequestData(context);

        // Act
        var result = AuthorizationHelper.CheckSelfOrCommittee(context, request, userId, _mockLogger.Object);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Test CheckSelfOrCommittee returns null when committee accesses another user's resource.
    /// </summary>
    [Fact]
    public void CheckSelfOrCommittee_CommitteeAccessingOtherResource_ReturnsNull()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var context = CreateFunctionContext("Committee", currentUserId);
        var request = CreateHttpRequestData(context);

        // Act
        var result = AuthorizationHelper.CheckSelfOrCommittee(context, request, targetUserId, _mockLogger.Object);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Test CheckSelfOrCommittee returns forbidden when non-committee user accesses another user's resource.
    /// </summary>
    [Fact]
    public void CheckSelfOrCommittee_NonCommitteeAccessingOtherResource_ReturnsForbidden()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var context = CreateFunctionContext("Volunteer", currentUserId);
        var request = CreateHttpRequestData(context);

        // Act
        var result = AuthorizationHelper.CheckSelfOrCommittee(context, request, targetUserId, _mockLogger.Object);

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// Creates a mock function context for testing.
    /// </summary>
    /// <param name="role">User role.</param>
    /// <param name="userId">Optional user ID.</param>
    /// <returns>Function context.</returns>
    private static FunctionContext CreateFunctionContext(string? role, Guid? userId = null)
    {
        var mockContext = new Mock<FunctionContext>();
        var items = new Dictionary<object, object>();
        
        if (role != null)
        {
            items["Role"] = role;
        }

        if (userId != null)
        {
            items["UserId"] = userId.Value;
        }

        mockContext.Setup(x => x.Items).Returns(items);
        
        // Set up IServiceProvider with serializer for WriteAsJsonAsync
        var services = new ServiceCollection();
        services.Configure<Microsoft.Azure.Functions.Worker.WorkerOptions>(options =>
        {
            options.Serializer = new TestJsonObjectSerializer();
        });
        var serviceProvider = services.BuildServiceProvider();
        mockContext.Setup(x => x.InstanceServices).Returns(serviceProvider);
        
        return mockContext.Object;
    }

    /// <summary>
    /// Creates a mock HTTP request data for testing.
    /// </summary>
    /// <param name="context">Function context.</param>
    /// <returns>HTTP request data.</returns>
    private static HttpRequestData CreateHttpRequestData(FunctionContext context)
    {
        return new MockHttpRequestData(context);
    }
}
