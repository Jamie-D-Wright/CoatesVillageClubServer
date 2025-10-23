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
using VillageClub.Membership.Data;
using VillageClub.Membership.Middleware;
using VillageClub.Membership.Models;
using VillageClub.Membership.Services;
using VillageClub.Membership.Validators;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProcessId()
    .Enrich.WithThreadId()
    .Enrich.WithProperty("Application", "VillageClub.Membership")
    .WriteTo.Console()
    .WriteTo.ApplicationInsights(
        Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING") ?? string.Empty,
        TelemetryConverter.Traces)
    .CreateLogger();

try
{
    Log.Information("Starting Village Club Membership Service");

    var host = new HostBuilder()
        .ConfigureFunctionsWorkerDefaults(builder =>
        {
            // Register middleware in order: exception handling first, then authentication
            builder.UseMiddleware<ExceptionHandlingMiddleware>();
            builder.UseMiddleware<JwtAuthenticationMiddleware>();
        })
        .ConfigureOpenApi()
        .ConfigureServices(services =>
    {
        // Application Insights
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Database Context
        services.AddDbContext<MembershipDbContext>((serviceProvider, options) =>
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

        // Auth Library Services (framework-agnostic)
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
        services.AddScoped<IPasswordHashService, VillageClub.Auth.Services.PasswordHashService>();
        
        // Domain Services (Membership-specific)
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuthService, AuthService>();

        // Validators
        services.AddScoped<IValidator<LoginRequest>, LoginRequestValidator>();
        services.AddScoped<IValidator<CreateUserRequest>, CreateUserRequestValidator>();
        services.AddScoped<IValidator<UpdateUserRequest>, UpdateUserRequestValidator>();
        services.AddScoped<IValidator<RefreshTokenRequest>, RefreshTokenRequestValidator>();
        services.AddScoped<IValidator<ChangePasswordRequest>, ChangePasswordRequestValidator>();
    })
    .UseSerilog()
    .Build();

    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Village Club Membership Service terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
