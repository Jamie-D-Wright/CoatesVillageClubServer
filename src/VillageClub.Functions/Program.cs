using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VillageClub.Functions.Configuration;
using VillageClub.Functions.Health;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureAppConfiguration(config =>
    {
        config.SetBasePath(Directory.GetCurrentDirectory())
              .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
              .AddEnvironmentVariables();

        var keyVaultName = config.Build().GetValue<string>("KeyVaultName");
        if (!string.IsNullOrEmpty(keyVaultName))
        {
            config.AddKeyVaultConfiguration(keyVaultName);
        }
    })
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        
        var serviceSettings = new ServiceSettings();
        context.Configuration.GetSection("ServiceSettings").Bind(serviceSettings);
        serviceSettings.Validate();
        services.AddSingleton(serviceSettings);
        
        services.AddSingleton<HealthService>();
    })
    .Build();

host.Run();
