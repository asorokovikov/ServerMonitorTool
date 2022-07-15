using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace ServerMonitorCore.Database;

public sealed class DatabaseConfiguration {
    public int Port { get; init; }
    public string Host { get; init; } = default!;
    public string Username { get; init; } = default!;
    public string Password { get; init; } = default!;
    public string DatabaseName { get; init; } = default!;
    public string MetricsTableName { get; init; } = default!;

    public string ConnectionString => $"Host={Host};Port={Port};Username={Username};Password={Password};";
    public string ConnectionDatabaseString => $"{ConnectionString};Database={DatabaseName};";
}

public static class DatabaseConfigurationHelper {
    public static NpgsqlConnection GetConnectionToDatabase(this DatabaseConfiguration configuration) =>
        new(configuration.ConnectionDatabaseString);

    public static NpgsqlConnection GetConnectionToPostgresql(this DatabaseConfiguration configuration) =>
        new(configuration.ConnectionString);
}

public static class DatabaseHelper {
    public static IHost PrepareDatabase(this IHost app) {
        using var scopedServices = app.Services.CreateScope();
        var serviceProvider = scopedServices.ServiceProvider;
        var repository = serviceProvider.GetRequiredService<PostgreMetricsRepository>();
        repository.EnsureDeletedAsync().Wait();
        repository.EnsureCreatedAsync().Wait();
        return app;
    }

    public static IServiceCollection
    AddDatabase(this IServiceCollection services) {
        services.AddTransient<IRepository<ServerMetrics>, PostgreMetricsRepository>();
        // services.AddTransient<IMetricsRepository, PostgreMetricsRepository>();
        services.AddTransient<PostgreMetricsRepository>();
        return services;
    }
}