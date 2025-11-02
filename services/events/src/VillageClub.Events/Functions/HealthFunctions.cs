using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace VillageClub.Events.Functions;

/// <summary>
/// Health check functions for service monitoring.
/// </summary>
public class HealthFunctions
{
    private readonly ILogger<HealthFunctions> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HealthFunctions"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public HealthFunctions(ILogger<HealthFunctions> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Health check endpoint - returns service status.
    /// </summary>
    /// <param name="req">The HTTP request.</param>
    /// <returns>Health status response.</returns>
    [Function("Health")]
    [OpenApiOperation(operationId: "GetHealth", tags: new[] { "Health" }, Summary = "Get service health status", Description = "Returns the health status of the Events service including version information.")]
    [OpenApiSecurity("bearer_auth", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(object), Description = "Service is healthy")]
    public async Task<HttpResponseData> GetHealth(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequestData req)
    {
        _logger.LogInformation("Health check requested");

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");

        var healthStatus = new
        {
            service = "Events",
            status = "Healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0.0",
            environment = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT") ?? "Development",
        };

        await response.WriteStringAsync(JsonSerializer.Serialize(healthStatus));
        return response;
    }

    /// <summary>
    /// Readiness check endpoint - verifies database connectivity.
    /// </summary>
    /// <param name="req">The HTTP request.</param>
    /// <returns>Readiness status response.</returns>
    [Function("Ready")]
    [OpenApiOperation(operationId: "GetReady", tags: new[] { "Health" }, Summary = "Get service readiness status", Description = "Verifies that the Events service is ready to handle requests, including database connectivity.")]
    [OpenApiSecurity("bearer_auth", SecuritySchemeType.Http, Scheme = OpenApiSecuritySchemeType.Bearer, BearerFormat = "JWT")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(object), Description = "Service is ready")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.ServiceUnavailable, contentType: "application/json", bodyType: typeof(object), Description = "Service is not ready")]
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
                service = "Events",
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
                service = "Events",
                ready = false,
                timestamp = DateTime.UtcNow,
                error = ex.Message,
            };

            await response.WriteStringAsync(JsonSerializer.Serialize(errorStatus));
            return response;
        }
    }
}
