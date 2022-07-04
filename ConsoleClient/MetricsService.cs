using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServerMonitorCore;
using System.Diagnostics;

namespace ConsoleClient;

sealed class RetryPolicy : IRetryPolicy {
    private readonly int _reconnectDelayMs;

    public RetryPolicy(int reconnectDelayMs) =>
        _reconnectDelayMs = reconnectDelayMs;

    public TimeSpan? NextRetryDelay(RetryContext retryContext) =>
        retryContext.PreviousRetryCount == 0 ? TimeSpan.Zero : TimeSpan.FromMilliseconds(_reconnectDelayMs);
}

public sealed class MetricsService : BackgroundService {
    private readonly IMetricsProvider _metricsProvider;
    private readonly ILogger _logger;
    
    private HubConnection? _connection;
    private ServerConfiguration _configuration;

    public MetricsService(
        IMetricsProvider metricsProvider, 
        IOptions<ServerConfiguration> options, 
        ILogger<MetricsService> logger
    ) {
        _metricsProvider = metricsProvider;
        _configuration = options.Value;
        _logger = logger;
    }

    private IEnumerable<(HubConnection, string)> GetHubConnections() {
        foreach (var connectionString in _configuration.GetConnectionStrings()) {
            var connection = new HubConnectionBuilder()
                .WithUrl(connectionString)
                .WithAutomaticReconnect(new RetryPolicy(_configuration.ReconnectDelayMs))
                .Build();

            connection.Reconnecting += exception => {
                _logger.LogError(exception, $"The connection has been lost, trying reconnect every {_configuration.ReconnectDelayMs} ms");
                return Task.CompletedTask;
            };

            connection.On<ConfigurationMessage>("ReceiveConfiguration", newConfiguration => {
                _logger.LogInformation($"Received {newConfiguration}");
                _configuration = _configuration.ReplaceUpdateInterval(newConfiguration.UpdateIntervalSeconds * 1000);
            });
            yield return (connection, connectionString);
        }
    }

    protected override async Task
    ExecuteAsync(CancellationToken cancellationToken) {
        while (!cancellationToken.IsCancellationRequested) {
            try {
                if (_connection is { State: HubConnectionState.Connected }) {
                    var metrics = _metricsProvider.GetShapshot();
                    _logger.LogInformation($"Sending metrics: {metrics}");
                    await _connection.InvokeAsync("SendMetrics", metrics, cancellationToken);
                }
                await Task.Delay(_configuration.UpdateIntervalMs, cancellationToken);
            }
            catch when (cancellationToken.IsCancellationRequested) {
                break;
            }
            catch (Exception exception) {
                _logger.LogError(exception, "Failed to connect the server");

            }
        }
    }

    public override async Task
    StartAsync(CancellationToken cancellationToken) {
        while (true) {
            foreach (var (connection, connectionString) in GetHubConnections()) {
                try {
                    _logger.LogInformation($"Trying connect to the hub: {connectionString}");
                    _connection = connection;
                    await _connection.StartAsync(cancellationToken);
                    Debug.Assert(connection.State == HubConnectionState.Connected);
                    _logger.LogInformation($"The connection has been established");
                    await base.StartAsync(cancellationToken);
                    return;
                }
                catch when (cancellationToken.IsCancellationRequested) {
                    return;
                }
                catch (Exception exception) {
                    Debug.Assert(_connection?.State == HubConnectionState.Disconnected);
                    _logger.LogError(exception, message: $"Failed to connect the hub, trying again in {_configuration.ReconnectDelayMs} ms");
                    await _connection.DisposeAsync();
                    _connection = null;
                    await Task.Delay(_configuration.ReconnectDelayMs, cancellationToken);
                }
            }
        }
    }

    public override async Task
    StopAsync(CancellationToken cancellationToken) { 
        if (_connection != null)
            await _connection.DisposeAsync();
        await base.StopAsync(cancellationToken);
    }
}
