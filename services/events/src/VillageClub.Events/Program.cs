using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.OpenApi.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using VillageClub.Auth.Services;
using VillageClub.Contracts.Auth;
using VillageClub.Events.Core.Validators;
using VillageClub.Events.Data;
using VillageClub.Events.Middleware;
using VillageClub.Events.Services;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProcessId()
    .Enrich.WithThreadId()
    .Enrich.WithProperty("Application", "VillageClub.Events")
    .WriteTo.Console()
    .WriteTo.ApplicationInsights(
        Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING") ?? string.Empty,
        TelemetryConverter.Traces)
    .CreateLogger();

try
{
    Log.Information("Starting Village Club Events Service");

    var host = new HostBuilder()
        .ConfigureFunctionsWebApplication(builder =>
        {
            // Register middleware in order: exception handling first, then authentication
            builder.UseMiddleware<ExceptionHandlingMiddleware>();
            builder.UseMiddleware<JwtAuthenticationMiddleware>();
        })
        .ConfigureServices(services =>
        {
            // Application Insights
            services.AddApplicationInsightsTelemetryWorkerService();
            services.ConfigureFunctionsApplicationInsights();

            // Database Context
            services.AddDbContext<EventsDbContext>((serviceProvider, options) =>
            {
                var connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null);
                });
            });

            // Auth Library Services (shared from VillageClub.Auth)
            services.AddSingleton<IJwtTokenService>(sp =>
            {
                var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "VillageClub";
                var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "VillageClub.Api";
                var expirationMinutes = int.TryParse(Environment.GetEnvironmentVariable("JWT_EXPIRATION_MINUTES"), out var minutes) 
                    ? minutes 
                    : 15;
                var privateKey = Environment.GetEnvironmentVariable("JWT_PRIVATE_KEY");
            
                return new VillageClub.Auth.Services.JwtTokenService(issuer, audience, expirationMinutes, privateKey);
            });

            // Domain Services (Events-specific)
            services.AddScoped<IEventService, EventService>();

            // Validators (from Events.Core library)
            services.AddScoped<IValidator<VillageClub.Events.Core.Models.CreateEventRequest>, CreateEventRequestValidator>();

            // JSON serialization configuration - camelCase for API responses
            services.Configure<JsonSerializerOptions>(options =>
            {
                options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            });
        })
    .UseSerilog()
    .Build();

    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Village Club Events Service terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
