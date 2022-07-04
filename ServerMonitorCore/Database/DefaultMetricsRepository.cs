using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using ServerMonitorCore.Common;

namespace ServerMonitorCore.Database;

public interface IRepository<T>  {
    Task CreateAsync(T item);
    Task<IReadOnlyCollection<T>> GetAllAsync();
    Task<T> GetAsync(int id);
    Task RemoveAsync(int id);
    Task UpdateAsync(T item);
}

public interface IMetricsRepository : IRepository<ServerMetrics> {
    Task<IReadOnlyCollection<ServerMetrics>> GetLatestMetrics();
}

public sealed class DefaultMetricsRepository : IMetricsRepository, IRepository<ServerMetrics> {
    private readonly DatabaseConfiguration _configuration;
    private readonly ILogger _logger;
    private const string TableName = "metrics";

    public DefaultMetricsRepository(
        IOptions<DatabaseConfiguration> options, 
        ILogger<DefaultMetricsRepository> logger
    ) {
        _configuration = options.Value;
        _logger = logger;
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

    public async Task CreateAsync(ServerMetrics item) {
        const string query = $"INSERT INTO {TableName}" +
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

    public async Task<ServerMetrics> GetAsync(int id) {
        const string query = $"SELECT * FROM {TableName} WHERE ID = $1";

        await using var connection = _configuration.GetConnection();
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(query, connection);
        command.Parameters.Add(id);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) {
            var result = reader.ReadServerMetrics();
            _logger.LogInformation($"Retrieved item from the database: {result.ToJson()}");
            return result;
        }
        throw new InvalidOperationException();
    }

    public async Task<IReadOnlyCollection<ServerMetrics>> GetAllAsync() {
        const string query = $"SELECT * FROM {TableName}";
        var result = new List<ServerMetrics>();

        await using var connection = _configuration.GetConnection();
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(query, connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) {
            result.Add(reader.ReadServerMetrics());
        }
        _logger.LogInformation($"Retrieved items from the database: {result.Count}");
        return result;
    }

    public async Task<IReadOnlyCollection<ServerMetrics>> GetLatestMetrics() {
        const string query = $"SELECT DISTINCT ON (ipaddress) * FROM {TableName} ORDER BY ipaddress, timestamp DESC";
        var result = new List<ServerMetrics>();

        await using var connection = _configuration.GetConnection();
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(query, connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) {
            result.Add(reader.ReadServerMetrics());
        }
        _logger.LogInformation($"Retrieved items from the database: {result.Count}");
        return result;
    }

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

public static class NpgsqlDataReaderHelper {
    public static ServerMetrics ReadServerMetrics(this NpgsqlDataReader reader) {
        var ipAddress = reader["ipAddress"] as string;
        var processorUsagePercent = reader["processorUsagePercent"] as float?;
        var availableMemoryMBytes = reader["availableMemoryMBytes"] as int?;
        var totalMemoryMBytes = reader["totalMemoryMBytes"] as int?;
        var timestamp = reader["timestamp"] as DateTime?;
        return new (
            snapshot: new (
                machineName: ipAddress ?? string.Empty,
                processorUsagePercent: processorUsagePercent ?? 0f,
                availableMemoryMBytes: availableMemoryMBytes ?? 0,
                totalMemoryMBytes: totalMemoryMBytes ?? 0,
                drives: ImmutableList<DriveMetrics>.Empty),
            connectionId: string.Empty,
            ipAddress: ipAddress ?? string.Empty,
            timestamp: new DateTimeOffset(timestamp ?? DateTime.MinValue)
        );
    }
}