using ConsoleClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ServerMonitorCore;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureHostConfiguration(config => {
        config.AddJsonFile("config.json");
    })
    .ConfigureServices((context, services) => services
        .Configure<ServerConfiguration>(context.Configuration.GetSection(nameof(ServerConfiguration)))
        .AddHostedService<MetricsService>()
        .AddMetrics());

await builder.Build().RunAsync();




