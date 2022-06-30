using System.Collections.Immutable;
using Microsoft.Extensions.Options;
using Npgsql;
using ServerMonitorCore;

namespace ServerMonitorApp.Services;

public interface IMetricsRepository {
    Task<bool> InitializeDatabase();
    Task<ImmutableList<ServerMetrics>> GetMetricsAsync();
}

public sealed class DefaultMetricsRepository : IMetricsRepository {
    private readonly DatabaseConfiguration _configuration;
    private readonly ILogger _logger;

    public DefaultMetricsRepository(IOptions<DatabaseConfiguration> options, ILogger<DefaultMetricsRepository> logger) {
        _configuration = options.Value;
        _logger = logger;
    }

    public async Task<bool> InitializeDatabase() {
        try {
            await using var connection = new NpgsqlConnection(_configuration.ConnectionString);
            await connection.OpenAsync();

        }
        catch (Exception exception) {
            _logger.LogError(exception, "Failed to connect the database");
            return false;
        }
        return true;
    }

    public async Task<ImmutableList<ServerMetrics>> GetMetricsAsync() {
        var result = new List<ServerMetrics>();
        try {
            await using var connection = GetConnection();
            await connection.OpenAsync();
            const string query = "SELECT * FROM metrics";
            await using var command = new NpgsqlCommand(query, connection);
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync()) {
                //var metrics = new ServerMetrics()
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception exception) {
            _logger.LogError(exception, "Failed to get metrics from the database");
        }
        return result.ToImmutableList();
    }

    private NpgsqlConnection GetConnection() {
        var connectionString = $"{_configuration.ConnectionString}Database={_configuration.DatabaseName}";
        var connection = new NpgsqlConnection(connectionString);
        return connection;
    }
}
