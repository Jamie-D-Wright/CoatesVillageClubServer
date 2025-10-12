using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Text.Json;
using VillageClub.Functions.Configuration;

namespace VillageClub.Functions.Functions
{
    public class InfoFunction
    {
        private readonly ServiceSettings _settings;

        public InfoFunction(ServiceSettings settings)
        {
            _settings = settings;
        }

        [Function("Info")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "info")] HttpRequestData req)
        {
            var info = new
            {
                ApplicationName = _settings.ApplicationName,
                Environment = _settings.EnvironmentName,
                Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
                StartupTime = DateTime.UtcNow,
                Configuration = new
                {
                    LogLevel = _settings.LogLevel,
                    Port = _settings.Port
                }
            };

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(info);

            return response;
        }
    }
}