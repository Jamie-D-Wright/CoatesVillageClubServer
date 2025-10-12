using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using VillageClub.Functions.Health;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace VillageClub.Functions.Functions
{
    public class HealthFunction
    {
        private readonly HealthService _healthService;
        private readonly ILogger<HealthFunction> _logger;

        public HealthFunction(HealthService healthService, ILogger<HealthFunction> logger)
        {
            _healthService = healthService;
            _logger = logger;
        }

        [Function("Health")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequestData req)
        {
            _logger.LogInformation("Processing health check request");

            var status = _healthService.GetHealthStatus();
            _logger.LogDebug("Health status: {@Status}", status);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(status);

            _logger.LogInformation("Health check completed successfully");
            return response;
        }
    }
}