using System.Threading.Channels;
using Microsoft.Extensions.Options;
using Npgsql;
using ServerMonitorApp.Notifications;
using ServerMonitorCore;
using ServerMonitorCore.Common;

namespace ServerMonitorApp.Services;

public sealed class DatabaseConfiguration {
    public string ConnectionString { get; init; } = default!;
    public string DatabaseName { get; init; } = default!;
}

public sealed class DatabaseService : BackgroundService {
    private readonly ILogger _logger;
    private readonly IDisposable disposable;
    private readonly DatabaseConfiguration _configuration;
    private readonly IMetricsRepository _repository;
    private readonly Channel<ServerMetrics> _channel;

    public DatabaseService(
        IMetricsRepository repository,
        INotificationManager<ServerMetrics> notificationManager,
        IOptions<DatabaseConfiguration> options,
        ILogger<DatabaseService> logger
    ) {
        _configuration = options.Value;
        _logger = logger;
        _repository = repository;
        _channel = Channel.CreateUnbounded<ServerMetrics>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = true});
        disposable = notificationManager.Subscribe(OnMetricsReceived);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        var isLoaded = await _repository.InitializeDatabase();
        //return;
        //var isInitialized = await CreateDatabaseIfNotExistAsync(stoppingToken);
        //return;
        //if (!isInitialized)
        //    return;
        //while (!stoppingToken.IsCancellationRequested) {
        //    try {
        //        await foreach (var metrics in _channel.Reader.ReadAllAsync(stoppingToken)) {
        //            await using var connection = new NpgsqlConnection(_configuration.ConnectionString);
        //            await connection.OpenAsync(stoppingToken);
        //            await SaveToDatabase( connection, metrics);
        //            await connection.CloseAsync();
        //        }
        //    }
        //    catch (OperationCanceledException) { }
        //    catch (Exception exception) {
        //        _logger.LogError(exception, "Failed to connect the database");
        //    }
        //    await Task.Delay(1000, stoppingToken);
        //}
    }

    public override async Task StopAsync(CancellationToken cancellationToken) {
        disposable.Dispose();
        await base.StopAsync(cancellationToken);
    }

    private async Task<bool> CreateDatabaseIfNotExistAsync(CancellationToken cancellationToken) {
        try {
            var connectionString = $"{_configuration.ConnectionString}Database={_configuration.DatabaseName}";
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            var databases = await GetDatabasesNameAsync(connection, cancellationToken);
            if (!databases.Contains(_configuration.DatabaseName)) {
                await CreateDatabaseAsync(connection, cancellationToken);
                await ConnectToDatabase(connection, cancellationToken);
                await CreateTableIfNotExists(connection, cancellationToken);
            }
        }
        catch (Exception exception) {
            _logger.LogError(exception, "Failed to connect the database");
            return false;
        }
        return true;
    }

    private async Task<List<string>> GetDatabasesNameAsync(NpgsqlConnection connection, CancellationToken cancellationToken) {
        var result = new List<string>();
        var query = "SELECT datname FROM pg_database;";
        await using var selectCommand = new NpgsqlCommand(query, connection);
        await using var reader = await selectCommand.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) {
            result.Add(reader.GetString(0));
        }
        return result;
    }

    private async Task CreateDatabaseAsync(NpgsqlConnection connection, CancellationToken cancellationToken) {
        var query = $"CREATE DATABASE {_configuration.DatabaseName} WITH OWNER = postgres ENCODING = 'UTF8' CONNECTION LIMIT = -1;";
        await using var command = new NpgsqlCommand(query, connection);
        _logger.LogInformation("Creating a new database");
        await command.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogInformation("The database has been successfully created");
    }

    private async Task ConnectToDatabase(NpgsqlConnection connection, CancellationToken cancellationToken) {
        await using var command = new NpgsqlCommand($"DATABASE {_configuration.DatabaseName}", connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task CreateTableIfNotExists(NpgsqlConnection connection, CancellationToken cancellationToken) {
        const string query = "CREATE TABLE IF NOT EXISTS metrics (metricId serial PRIMARY KEY, ipAddresss char(15) NOT NULL, processorUsagePercent real NOT NULL, availableMemoryMBytes int NOT NULL, totalMemoryMBytes int NOT NULL, timestamp TIMESTAMP NOT NULL);";
        await using var command = new NpgsqlCommand(query, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private ValueTask SaveToDatabase(NpgsqlConnection connection, ServerMetrics metrics) {
        //await using var command = new NpgsqlCommand("INSERT INTO", connection) {
        //    Parameters = {

        //    }INSERT INTO metrics(ipAddress, processorUsagePercent, availableMemoryMBytes, totalMemoryMBytes, timestamp)
        //VALUES('127.0.0.1', 15.6, 16000, 10000, '2016-06-22 19:10:25-07')
        //}
        //await command.ExecuteNonQueryAsync();
        return ValueTask.CompletedTask;
    }

    private void OnMetricsReceived(ServerMetrics message) {
        _channel.Writer.WriteAsync(message);
    }

}
