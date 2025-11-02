using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VillageClub.Events.Functions;
using Xunit;

namespace VillageClub.Events.Tests.Functions;

/// <summary>
/// Tests for health check endpoints.
/// </summary>
public class HealthFunctionsTests : FunctionTestBase
{
    private readonly HealthFunctions _healthFunctions;

    /// <summary>
    /// Initializes a new instance of the <see cref="HealthFunctionsTests"/> class.
    /// </summary>
    public HealthFunctionsTests()
    {
        var logger = ServiceProvider.GetRequiredService<ILogger<HealthFunctions>>();
        _healthFunctions = new HealthFunctions(logger);
    }

    [Fact]
    public async Task GetHealth_ReturnsHealthyStatus()
    {
        // Arrange
        var request = CreateHttpRequest("GET", "/api/health");

        // Act
        var response = await _healthFunctions.GetHealth(request);

        // Assert
        var responseBody = await ReadResponseBodyAsString(response);
        var healthStatus = JsonSerializer.Deserialize<JsonElement>(responseBody);
        healthStatus.GetProperty("status").GetString().Should().Be("Healthy");
        healthStatus.GetProperty("service").GetString().Should().Be("Events");
        healthStatus.GetProperty("version").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetHealth_Returns200OK()
    {
        // Arrange
        var request = CreateHttpRequest("GET", "/api/health");

        // Act
        var response = await _healthFunctions.GetHealth(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetHealth_ReturnsJsonContentType()
    {
        // Arrange
        var request = CreateHttpRequest("GET", "/api/health");

        // Act
        var response = await _healthFunctions.GetHealth(request);

        // Assert
        response.Headers.TryGetValues("Content-Type", out var contentTypes);
        contentTypes.Should().Contain("application/json");
    }

    [Fact]
    public async Task GetReadiness_Returns200OK()
    {
        // Arrange
        var request = CreateHttpRequest("GET", "/api/ready");

        // Act
        var response = await _healthFunctions.GetReadiness(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseBody = await ReadResponseBodyAsString(response);
        var readyStatus = JsonSerializer.Deserialize<JsonElement>(responseBody);
        readyStatus.GetProperty("ready").GetBoolean().Should().BeTrue();
        readyStatus.GetProperty("service").GetString().Should().Be("Events");
    }

    [Fact]
    public async Task GetReadiness_ReturnsJsonContentType()
    {
        // Arrange
        var request = CreateHttpRequest("GET", "/api/ready");

        // Act
        var response = await _healthFunctions.GetReadiness(request);

        // Assert
        response.Headers.TryGetValues("Content-Type", out var contentTypes);
        contentTypes.Should().Contain("application/json");
    }

    /// <summary>
    /// Configures services for health function tests.
    /// </summary>
    /// <param name="services">The service collection.</param>
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
    }
}
