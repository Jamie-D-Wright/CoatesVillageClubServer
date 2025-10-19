using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Moq;
using VillageClub.Membership.Middleware;
using Xunit;

namespace VillageClub.Membership.Tests.Middleware;

/// <summary>
/// Tests for AuthContext extensions.
/// </summary>
public class AuthContextExtensionsTests
{
    /// <summary>
    /// Test GetUserId returns user ID when set.
    /// </summary>
    [Fact]
    public void GetUserId_WhenUserIdSet_ReturnsUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var context = CreateFunctionContext();
        context.Items["UserId"] = userId;

        // Act
        var result = context.GetUserId();

        // Assert
        result.Should().Be(userId);
    }

    /// <summary>
    /// Test GetUserId returns null when not set.
    /// </summary>
    [Fact]
    public void GetUserId_WhenNotSet_ReturnsNull()
    {
        // Arrange
        var context = CreateFunctionContext();

        // Act
        var result = context.GetUserId();

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Test GetUserEmail returns email when set.
    /// </summary>
    [Fact]
    public void GetUserEmail_WhenEmailSet_ReturnsEmail()
    {
        // Arrange
        var email = "test@example.com";
        var context = CreateFunctionContext();
        context.Items["Email"] = email;

        // Act
        var result = context.GetUserEmail();

        // Assert
        result.Should().Be(email);
    }

    /// <summary>
    /// Test GetUserRole returns role when set.
    /// </summary>
    [Fact]
    public void GetUserRole_WhenRoleSet_ReturnsRole()
    {
        // Arrange
        var role = "Committee";
        var context = CreateFunctionContext();
        context.Items["Role"] = role;

        // Act
        var result = context.GetUserRole();

        // Assert
        result.Should().Be(role);
    }

    /// <summary>
    /// Test GetCommitteeRole returns committee role when set.
    /// </summary>
    [Fact]
    public void GetCommitteeRole_WhenCommitteeRoleSet_ReturnsCommitteeRole()
    {
        // Arrange
        var committeeRole = "Treasurer";
        var context = CreateFunctionContext();
        context.Items["CommitteeRole"] = committeeRole;

        // Act
        var result = context.GetCommitteeRole();

        // Assert
        result.Should().Be(committeeRole);
    }

    /// <summary>
    /// Test HasRole returns true for matching role (case insensitive).
    /// </summary>
    /// <param name="userRole">The user's role.</param>
    /// <param name="roleToCheck">The role to check against.</param>
    /// <param name="expected">Expected result.</param>
    [Theory]
    [InlineData("Committee", "Committee", true)]
    [InlineData("Committee", "committee", true)]
    [InlineData("Committee", "COMMITTEE", true)]
    [InlineData("Committee", "Volunteer", false)]
    [InlineData("Committee", "Member", false)]
    public void HasRole_ChecksRoleCaseInsensitive(string userRole, string roleToCheck, bool expected)
    {
        // Arrange
        var context = CreateFunctionContext();
        context.Items["Role"] = userRole;

        // Act
        var result = context.HasRole(roleToCheck);

        // Assert
        result.Should().Be(expected);
    }

    /// <summary>
    /// Test IsCommittee returns true when user is committee.
    /// </summary>
    [Fact]
    public void IsCommittee_WhenUserIsCommittee_ReturnsTrue()
    {
        // Arrange
        var context = CreateFunctionContext();
        context.Items["Role"] = "Committee";

        // Act
        var result = context.IsCommittee();

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Test IsCommittee returns false when user is not committee.
    /// </summary>
    [Fact]
    public void IsCommittee_WhenUserIsNotCommittee_ReturnsFalse()
    {
        // Arrange
        var context = CreateFunctionContext();
        context.Items["Role"] = "Volunteer";

        // Act
        var result = context.IsCommittee();

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Test IsVolunteer returns true when user is volunteer.
    /// </summary>
    [Fact]
    public void IsVolunteer_WhenUserIsVolunteer_ReturnsTrue()
    {
        // Arrange
        var context = CreateFunctionContext();
        context.Items["Role"] = "Volunteer";

        // Act
        var result = context.IsVolunteer();

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Test IsMember returns true when user is member.
    /// </summary>
    [Fact]
    public void IsMember_WhenUserIsMember_ReturnsTrue()
    {
        // Arrange
        var context = CreateFunctionContext();
        context.Items["Role"] = "Member";

        // Act
        var result = context.IsMember();

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Creates a mock function context for testing.
    /// </summary>
    /// <returns>Function context.</returns>
    private static FunctionContext CreateFunctionContext()
    {
        var mockContext = new Mock<FunctionContext>();
        mockContext.Setup(x => x.Items).Returns(new Dictionary<object, object>());
        return mockContext.Object;
    }
}
