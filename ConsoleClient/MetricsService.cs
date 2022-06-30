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
    private readonly HubConnection _connection;
    private readonly ILogger _logger;

    private ServerConfiguration _configuration;

    public MetricsService(
        IMetricsProvider metricsProvider, 
        IOptions<ServerConfiguration> options, 
        ILogger<MetricsService> logger
    ) {
        _metricsProvider = metricsProvider;
        _configuration = options.Value;
        _logger = logger;

        _connection = new HubConnectionBuilder()
            .WithUrl(_configuration.HubUrl)
            .WithAutomaticReconnect(new RetryPolicy(_configuration.ReconnectDelayMs))
            .Build();

        _connection.Reconnecting += exception => {
            _logger.LogError(exception, $"The connection has been lost, trying reconnect every {_configuration.ReconnectDelayMs} ms");
            return Task.CompletedTask;
        };

        _connection.On<ConfigurationMessage>("SendConfiguration", (newConfiguration) => {
            _logger.LogInformation($"Received {newConfiguration}");
            _configuration = _configuration.ReplaceUpdateInterval(newConfiguration.UpdateIntervalSeconds * 1000);
        });
    }

    protected override async Task
    ExecuteAsync(CancellationToken cancellationToken) {
        while (!cancellationToken.IsCancellationRequested) {
            try {
                if (_connection.State == HubConnectionState.Connected) {
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
            try {
                await _connection.StartAsync(cancellationToken);
                Debug.Assert(_connection.State == HubConnectionState.Connected);
                _logger.LogInformation($"Connected to the hub: {_configuration.HubUrl}");
                break;
            }
            catch when (cancellationToken.IsCancellationRequested) {
                break;
            }
            catch (Exception exception) {
                Debug.Assert(_connection.State == HubConnectionState.Disconnected);
                _logger.LogError(exception, message: $"Failed to connect the hub, trying again in {_configuration.ReconnectDelayMs} ms");
                await Task.Delay(_configuration.ReconnectDelayMs, cancellationToken);
            }
        }
        await base.StartAsync(cancellationToken);
    }

    public override async Task
    StopAsync(CancellationToken cancellationToken) { 
        await _connection.DisposeAsync();
        await base.StopAsync(cancellationToken);
    }
}
