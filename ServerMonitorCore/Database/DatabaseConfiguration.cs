using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace ServerMonitorCore.Database;

public sealed class DatabaseConfiguration {
    public string Host { get; init; } = default!;
    public int Port { get; init; }
    public string Username { get; init; } = default!;
    public string Password { get; init; } = default!;
    public string DatabaseName { get; init; } = default!;

    public string ConnectionString => $"Host={Host};Port={Port};Username={Username};Password={Password};";
    public string ConnectionDatabaseString => $"{ConnectionString};Database={DatabaseName};";
}

public static class DatabaseConfigurationHelper {
    public static NpgsqlConnection GetConnection(this DatabaseConfiguration configuration) => 
        new(configuration.ConnectionDatabaseString);
}

public static class DatabaseHelper {
    public static IHost PrepareDatabase(this IHost app) {
        using var scopedServices = app.Services.CreateScope();
        var serviceProvider = scopedServices.ServiceProvider;
        var repository = serviceProvider.GetRequiredService<DefaultMetricsRepository>();
        repository.InitializeDatabaseAsync().Wait();
        return app;
    }
}