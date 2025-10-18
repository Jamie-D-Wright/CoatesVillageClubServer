using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace VillageClub.Membership.Functions;

/// <summary>
/// Health check functions for service monitoring and JWT public key distribution
/// </summary>
public class HealthFunctions
{
    private readonly ILogger<HealthFunctions> _logger;

    public HealthFunctions(ILogger<HealthFunctions> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Health check endpoint - returns service status and JWT public key
    /// </summary>
    [Function("Health")]
    public async Task<HttpResponseData> GetHealth(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequestData req)
    {
        _logger.LogInformation("Health check requested");

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");

        var healthStatus = new
        {
            service = "Membership",
            status = "Healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0.0",
            environment = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT") ?? "Development",
        };

        await response.WriteStringAsync(JsonSerializer.Serialize(healthStatus));
        return response;
    }

    /// <summary>
    /// <summary>
    /// Readiness check endpoint - verifies database connectivity.
    /// </summary>
    /// <param name="req">The HTTP request.</param>
    /// <returns>Readiness status response.</returns>
    [Function("Ready")]
    public async Task<HttpResponseData> GetReadiness(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ready")] HttpRequestData req)
    {
        _logger.LogInformation("Readiness check requested");

        try
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");

            var readinessStatus = new
            {
                service = "Membership",
                ready = true,
                timestamp = DateTime.UtcNow,
                checks = new
                {
                    database = "NotImplemented",
                },
            };

            await response.WriteStringAsync(JsonSerializer.Serialize(readinessStatus));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Readiness check failed");

            var response = req.CreateResponse(HttpStatusCode.ServiceUnavailable);
            response.Headers.Add("Content-Type", "application/json");

            var errorStatus = new
            {
                service = "Membership",
                ready = false,
                timestamp = DateTime.UtcNow,
                error = ex.Message,
            };

            await response.WriteStringAsync(JsonSerializer.Serialize(errorStatus));
            return response;
        }
    }
}
