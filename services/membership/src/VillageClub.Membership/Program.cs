using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VillageClub.Auth.Services;
using VillageClub.Contracts.Auth;
using VillageClub.Membership.Data;
using VillageClub.Membership.Models;
using VillageClub.Membership.Services;
using VillageClub.Membership.Validators;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
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
    .Build();

host.Run();
