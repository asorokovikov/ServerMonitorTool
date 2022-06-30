namespace ServerMonitorApp.Notifications;

public interface INotificationManager<out T> {
    IDisposable Subscribe(Action<T> listener);
}

public interface INotificationPublisher<in T> {
    Task PublishAsync(T item);
}

public sealed class 
DefaultNotificationManager<T> : INotificationManager<T>, INotificationPublisher<T> {
    private readonly TaskCompletionSource _taskCompletionSource = new();
    private event Action<T>? NotificationEvent;

    public IDisposable Subscribe(Action<T> listener) {
        var disposable = new DisposableToken<T>(this, listener);
        NotificationEvent += disposable.OnNotify;
        _taskCompletionSource.TrySetResult();
        return disposable;
    }

    public async Task PublishAsync(T item) {
        await WaitToFirstListener();
        NotificationEvent?.Invoke(item);
    }

    private Task WaitToFirstListener() => _taskCompletionSource.Task;

    private sealed class DisposableToken<TItem> : IDisposable {
        private readonly Action<TItem> _listener;
        private readonly DefaultNotificationManager<TItem> _manager;

        public DisposableToken(
            DefaultNotificationManager<TItem> manager,
            Action<TItem> listener
        ) {
            _manager = manager;
            _listener = listener;
        }

        public void OnNotify(TItem item) => _listener.Invoke(item);

        public void Dispose() => _manager.NotificationEvent -= OnNotify;
    }
}

public static class ServiceCollectionHelper {

    public static IServiceCollection
    AddNotificationManager<T>(this IServiceCollection services) {
        var manager = new DefaultNotificationManager<T>();
        services.AddSingleton<INotificationPublisher<T>>(manager);
        services.AddSingleton<INotificationManager<T>>(manager);
        services.AddSingleton<IBackgroundQueue<T>, DefaultBackgroundQueue<T>>();
        services.AddHostedService<NotificationService<T>>();
        return services;
    }
}