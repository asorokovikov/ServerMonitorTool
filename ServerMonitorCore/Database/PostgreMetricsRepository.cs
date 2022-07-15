using Npgsql;
using System.Net;
using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServerMonitorCore.Common;

using static ServerMonitorCore.Database.QueryBuilders.QueryBuilderColumnAttributes;
using static ServerMonitorCore.Database.QueryBuilders.QueryBuilderColumnType;

namespace ServerMonitorCore.Database;

public interface IRepository<TItem> where TItem : class {
    Task CreateAsync(TItem item);
    Task<IReadOnlyCollection<TItem>> GetAllAsync();
    Task<TItem?> GetAsync(int id);
    Task RemoveAsync(int id);
    Task UpdateAsync(TItem item);
}

public sealed class PostgreMetricsRepository : IRepository<ServerMetrics> {
    private readonly ILogger _logger;
    private readonly DatabaseConfiguration _config;

    private string TableName => _config.MetricsTableName;

    public PostgreMetricsRepository(
        IOptions<DatabaseConfiguration> options,
        ILogger<PostgreMetricsRepository> logger
    ) {
        _config = options.Value;
        _logger = logger;
    }

    public async Task EnsureCreatedAsync() {
        try {
            await using var connection = _config.GetConnectionToPostgresql();
            await connection.OpenAsync();
            var databases = await connection.GetDatabasesListAsync();
            if (databases.Contains(_config.DatabaseName))
                return;
            await connection.CreateDatabaseAsync(_config.DatabaseName);
            await CreateMetricsTableAsync();
        }
        catch (Exception exception) {
            _logger.LogError(exception, "Failed to initialize the database");
        }
    }

    public async Task EnsureDeletedAsync() {
        try {
            await using var connection = _config.GetConnectionToPostgresql();
            await connection.OpenAsync();
            await connection.DeleteDatabaseIfExistsAsync(_config.DatabaseName);
        }
        catch (Exception exception) {
            _logger.LogError(exception, "Failed to delete the database");
        }
    }

    public async Task CreateAsync(ServerMetrics item) { 
        var query = $"INSERT INTO {TableName}" +
            "(ip_address, cpu_usage_percent, memory_available_mbytes, memory_total_mbytes, timestamp) " +
            "VALUES ($1, $2, $3, $4, $5)";
        try {
            await using var connection = _config.GetConnectionToDatabase();
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

    public async Task<ServerMetrics?> GetAsync(int id) {
        await using var connection = _config.GetConnectionToDatabase();
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"SELECT * FROM {TableName} WHERE ID = $1", connection);
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
        var query = QueryBuilder.Select(TableName).Build();
        var result = new List<ServerMetrics>();

        await using var connection = _config.GetConnectionToDatabase();
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
        var query = $"SELECT DISTINCT ON (ip_address) * FROM {TableName} ORDER BY ip_address, timestamp DESC";
        var result = new List<ServerMetrics>();

        await using var connection = _config.GetConnectionToDatabase();
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

    public async Task 
    CreateMetricsTableAsync(CancellationToken cancellationToken = default) {
        var query = QueryBuilder.CreateTable(TableName)
            .WithColumn("metrics_id", Serial, PrimaryKey)
            .WithColumn("ip_address", IpAddress, NotNull)
            .WithColumn("cpu_usage_percent", Real, NotNull)
            .WithColumn("memory_available_mbytes", Integer, NotNull)
            .WithColumn("memory_total_mbytes", Integer, NotNull)
            .WithColumn("timestamp", Timestamp, NotNull)
            .Build();
        try {
            await using var connection = _config.GetConnectionToDatabase();
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand(query, connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception exception) {
            _logger.LogError(exception, exception.Message);
        }
    }
}

public static class NpgsqlHelper {

    public static async Task<IReadOnlyCollection<string>>
    GetDatabasesListAsync(this NpgsqlConnection connection, CancellationToken cancellationToken = default) {
        var result = new List<string>();
        await using var command = connection.CreateCommand(QueryBuilder.DatabasesListQuery);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) 
            result.Add(reader.GetString(0));
        return result;
    }

    public static async Task 
    DeleteDatabaseIfExistsAsync(this NpgsqlConnection connection, string databaseName, CancellationToken cancellationToken = default) {
        var query = QueryBuilder.DropDatabaseIfExistsQuery(databaseName);
        await using var command = connection.CreateCommand(query);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public static async Task
    CreateDatabaseAsync(this NpgsqlConnection connection, string databaseName, CancellationToken cancellationToken = default) {
        var query = QueryBuilder.CreateDatabaseQuery(databaseName);
        await using var command = connection.CreateCommand(query);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public static NpgsqlCommand 
    CreateCommand(this NpgsqlConnection connection, string query) => new (query, connection);

    public static ServerMetrics ReadServerMetrics(this NpgsqlDataReader reader) {
        var ipAddress = reader["ip_address"] as IPAddress;
        var processorUsagePercent = reader["cpu_usage_percent"] as float?;
        var availableMemoryMBytes = reader["memory_available_mbytes"] as int?;
        var totalMemoryMBytes = reader["memory_total_mbytes"] as int?;
        var timestamp = reader["timestamp"] as DateTime?;
        return new (
            snapshot: new (
                machineName: ipAddress?.MapToIPv4().ToString() ?? "none",
                processorUsagePercent: processorUsagePercent ?? 0f,
                availableMemoryMBytes: availableMemoryMBytes ?? 0,
                totalMemoryMBytes: totalMemoryMBytes ?? 0,
                drives: ImmutableList<DriveMetrics>.Empty),
            connectionId: string.Empty,
            ipAddress: ipAddress ?? IPAddress.None, 
            timestamp: new DateTimeOffset(timestamp ?? DateTime.MinValue)
        );
    }
}
