namespace ServerMonitorApp.Notifications;

public sealed class NotificationService<T> : BackgroundService {
    private readonly IBackgroundQueue<T> _queue;
    private readonly INotificationPublisher<T> _publisher;
    private readonly ILogger<NotificationService<T>> _logger;

    public NotificationService(
        IBackgroundQueue<T> queue, 
        INotificationPublisher<T> publisher, 
        ILogger<NotificationService<T>> logger
    ) {
        _queue = queue;
        _publisher = publisher;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken cancellationToken) {
        _logger.LogInformation($"{nameof(NotificationService<T>)}<{typeof(T).Name}> is running");
        return ProcessQueueAsync(cancellationToken);
    }

    private async Task ProcessQueueAsync(CancellationToken cancellationToken) {
        while (!cancellationToken.IsCancellationRequested) {
            try {
                var item = await _queue.DequeueAsync(cancellationToken);
                await _publisher.PublishAsync(item);
            }
            catch (OperationCanceledException) { }
            catch (Exception exception) {
                _logger.LogError(exception, "Error occurred processing item");
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken) {
        _logger.LogInformation($"{nameof(NotificationService<T>)} is stopping.");
        await base.StopAsync(cancellationToken);
    }
}