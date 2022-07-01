using System.Collections.Immutable;
using Microsoft.Extensions.Options;
using Npgsql;
using ServerMonitorCore;

namespace ServerMonitorApp.Database;

public interface IRepository<T>  {
    Task CreateAsync(T item);
    Task<IReadOnlyCollection<T>> GetAllAsync();
    Task<T> GetAsync(int id);
    Task RemoveAsync(int id);
    Task UpdateAsync(T item);
}

public sealed class DefaultMetricsRepository : IRepository<ServerMetrics> {
    private readonly DatabaseConfiguration _configuration;
    private readonly ILogger _logger;

    public DefaultMetricsRepository(
        IOptions<DatabaseConfiguration> options, 
        ILogger<DefaultMetricsRepository> logger
    ) {
        _configuration = options.Value;
        _logger = logger;
    }

    public async Task CreateAsync(ServerMetrics item) {
        const string query = "INSERT INTO metrics" +
            "(ipaddress, processorUsagePercent, availableMemoryMBytes, totalMemoryMBytes, timestamp) " +
            "VALUES ($1, $2, $3, $4, $5)";
        try {
            await using var connection = _configuration.GetConnection();
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(query, connection: connection) {
                Parameters = {
                    new() { Value = item.IpAddress },
                    new() { Value = item.Snapshot.ProcessorUsagePercent },
                    new() { Value = item.Snapshot.AvailableMemoryMBytes },
                    new() { Value = item.Snapshot.TotalMemoryMBytes },
                    new() { Value = item.Timestamp.LocalDateTime },
                }
            };
            await command.ExecuteNonQueryAsync();
            _logger.LogInformation($"ServerMetrics has been added to the database");

        }
        catch (Exception exception) {
            _logger.LogError(exception, exception.Message);
        }
    }

    public async Task InitializeDatabaseAsync() {
        try {
            await using var connection = new NpgsqlConnection(_configuration.ConnectionString);
            await connection.OpenAsync();
            var databases = await GetDatabasesNameAsync(connection);
            if (databases.Contains(_configuration.DatabaseName))
                return;
            await CreateDatabaseAsync(connection);
            await CreateMetricsTableAsync();
        }
        catch (Exception exception) {
            _logger.LogError(exception, "Failed to initialize the database");
        }
    }

    public Task<IReadOnlyCollection<ServerMetrics>> GetAllAsync() => throw new NotImplementedException();

    public Task<ServerMetrics> GetAsync(int id) => throw new NotImplementedException();

    public Task RemoveAsync(int id) => throw new NotImplementedException();

    public Task UpdateAsync(ServerMetrics item) => throw new NotImplementedException();

    private async Task<List<string>> 
    GetDatabasesNameAsync(NpgsqlConnection connection, CancellationToken cancellationToken = default) {
        var result = new List<string>();
        var query = "SELECT datname FROM pg_database;";
        await using var selectCommand = new NpgsqlCommand(query, connection);
        await using var reader = await selectCommand.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) {
            result.Add(reader.GetString(0));
        }
        return result;
    }

    private async Task 
    CreateDatabaseAsync(NpgsqlConnection connection, CancellationToken cancellationToken = default) {
        var query = $"CREATE DATABASE {_configuration.DatabaseName} WITH OWNER = {_configuration.Username} ENCODING = 'UTF8' CONNECTION LIMIT = -1;";
        await using var command = new NpgsqlCommand(query, connection);
        _logger.LogInformation("Creating a new database");
        await command.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogInformation("The database has been successfully created");
    }

    private async Task 
    CreateMetricsTableAsync(CancellationToken cancellationToken = default) {
        try {
            await using var connection = _configuration.GetConnection();
            await connection.OpenAsync(cancellationToken);
            const string query = $"CREATE TABLE if not exists metrics" +
                $"(" +
                $"id serial PRIMARY KEY, " +
                $"ipAddress char(15) NOT NULL, " +
                $"processorUsagePercent real NOT NULL, " +
                $"availableMemoryMBytes int NOT NULL, " +
                $"totalMemoryMBytes int NOT NULL, " +
                $"timestamp TIMESTAMP NOT NULL" +
                $")";

            await using var command = new NpgsqlCommand(query, connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception exception) {
            _logger.LogError(exception, exception.Message);
        }
    }
}

public static class DatabaseHelper {
    public static WebApplication PrepareDatabase(this WebApplication app) {
        using var scopedServices = app.Services.CreateScope();
        var serviceProvider = scopedServices.ServiceProvider;
        var repository = serviceProvider.GetRequiredService<DefaultMetricsRepository>();
        repository.InitializeDatabaseAsync().Wait();
        return app;
    }
}