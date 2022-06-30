using System.Threading.Channels;

namespace ServerMonitorApp.Notifications;

public interface INotificationManager<out T> {
    IDisposable Subscribe(Action<T> listener);
}

public interface INotificationPublisher<in T> {
    void Publish(T item);
    Task WaitToListener();
}

public interface IBackgroundQueue<T> {
    ValueTask EnqueueAsync(T value);
    ValueTask<T> DequeueAsync(CancellationToken cancellationToken);
}

public sealed class DefaultBackgroundQueue<T> : IBackgroundQueue<T> {
    private readonly Channel<T> _queue;

    public DefaultBackgroundQueue() {
        UnboundedChannelOptions options = new() { SingleReader = true };
        _queue = Channel.CreateUnbounded<T>(options);
    }

    public async ValueTask EnqueueAsync(T value) =>
        await _queue.Writer.WriteAsync(value);

    public async ValueTask<T> DequeueAsync(CancellationToken cancellationToken) =>
        await _queue.Reader.ReadAsync(cancellationToken);
}

public sealed class DefaultNotificationManager<T> : INotificationManager<T>, INotificationPublisher<T> {
    private readonly TaskCompletionSource _taskCompletionSource = new();
    private event Action<T>? NotificationEvent;

    public IDisposable Subscribe(Action<T> listener) {
        var disposable = new DisposableToken<T>(this, listener);
        NotificationEvent += disposable.OnNotify;
        _taskCompletionSource.TrySetResult();
        return disposable;
    }

    public void Publish(T item) => NotificationEvent?.Invoke(item);

    public Task WaitToListener() => _taskCompletionSource.Task;

    private sealed class DisposableToken<TItem> : IDisposable {
        private readonly DefaultNotificationManager<TItem> _manager;
        private readonly Action<TItem> _listener;

        public DisposableToken(DefaultNotificationManager<TItem> manager, Action<TItem> listener) =>
            (_manager, _listener) = (manager, listener);

        public void OnNotify(TItem item) => _listener.Invoke(item);

        public void Dispose() => _manager.NotificationEvent -= OnNotify;
    }
}

public static class ServiceCollectionHelper {
    public static IServiceCollection
    AddNotificationManager<T>(this IServiceCollection services) {
        var manager = new DefaultNotificationManager<T>();
        return services
            .AddSingleton<INotificationPublisher<T>>(manager)
            .AddSingleton<INotificationManager<T>>(manager)
            .AddSingleton<IBackgroundQueue<T>, DefaultBackgroundQueue<T>>()
            .AddHostedService<NotificationService<T>>();
    }
}