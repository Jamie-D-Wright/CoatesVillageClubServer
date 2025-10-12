using FluentAssertions;
using VillageClub.Functions.Health;
using VillageClub.Functions.Health.Models;

namespace VillageClub.Functions.Tests.Health
{
    public class HealthServiceTests
    {
        private readonly HealthService _healthService;

        public HealthServiceTests()
        {
            _healthService = new HealthService();
        }

        [Fact]
        public void GetHealthStatus_ShouldReturnValidStatus()
        {
            // Act
            var result = _healthService.GetHealthStatus();

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().BeOneOf(HealthStatus.States.Healthy, HealthStatus.States.Degraded, HealthStatus.States.Unhealthy);
            result.Uptime.Should().BeGreaterThan(TimeSpan.Zero);
            result.LastCheckTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            result.MemoryUsage.Should().BeGreaterThan(0);
            result.ThreadCount.Should().BeGreaterThan(0);
        }
    }
}