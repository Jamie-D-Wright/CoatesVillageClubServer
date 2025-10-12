using Microsoft.Extensions.Logging;
using FluentAssertions;
using VillageClub.Functions.Health;

namespace VillageClub.Functions.Tests
{
    public class LoggingTests
    {
        private readonly ILoggerFactory _loggerFactory;

        public LoggingTests()
        {
            _loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .SetMinimumLevel(LogLevel.Debug)
                    .AddApplicationInsights();
            });
        }

        [Fact]
        public void Logger_ShouldCreateValidLogger()
        {
            // Arrange & Act
            var logger = _loggerFactory.CreateLogger<HealthService>();

            // Assert
            logger.Should().NotBeNull();
        }

        [Fact]
        public void Logger_ShouldHandleAllLogLevels()
        {
            // Arrange
            var logger = _loggerFactory.CreateLogger<LoggingTests>();

            // Act & Assert
            Action debug = () => logger.LogDebug("Debug message");
            Action info = () => logger.LogInformation("Info message");
            Action warning = () => logger.LogWarning("Warning message");
            Action error = () => logger.LogError("Error message");
            Action critical = () => logger.LogCritical("Critical message");

            debug.Should().NotThrow();
            info.Should().NotThrow();
            warning.Should().NotThrow();
            error.Should().NotThrow();
            critical.Should().NotThrow();
        }
    }
}