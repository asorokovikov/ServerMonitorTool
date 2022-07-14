using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ServerMonitorCore.Notifications;

namespace ServerMonitorCore.Database;

public sealed class MetricsProcessingService : BackgroundService {
    private readonly IRepository<ServerMetrics, int> _repository;
    private readonly Channel<ServerMetrics> _channel;
    private readonly IDisposable _disposable;
    private readonly ILogger _logger;

    public MetricsProcessingService(
        IRepository<ServerMetrics, int> repository,
        INotificationManager<ServerMetrics> notificationManager,
        ILogger<MetricsProcessingService> logger
    ) {
        _channel = Channel.CreateUnbounded<ServerMetrics>(new() { SingleReader = true, SingleWriter = true});
        _disposable = notificationManager.Subscribe(x => _channel.Writer.WriteAsync(x));
        _repository = repository;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        _logger.LogInformation($"{nameof(MetricsProcessingService)} is running");
        while (!stoppingToken.IsCancellationRequested) {
            try {
                var item = await _channel.Reader.ReadAsync(stoppingToken).ConfigureAwait(false);
                await _repository.CreateAsync(item).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
            catch (Exception exception) {
                _logger.LogError(exception, exception.Message);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken) {
        _logger.LogInformation($"{nameof(MetricsProcessingService)} is stopping");
        _disposable.Dispose();
        await base.StopAsync(cancellationToken);
    }
}
