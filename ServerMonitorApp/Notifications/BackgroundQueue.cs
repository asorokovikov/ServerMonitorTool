using System.Threading.Channels;

namespace ServerMonitorApp.Notifications;

public interface IBackgroundQueue<T> {
    ValueTask EnqueueAsync(T value);
    ValueTask<T> DequeueAsync(CancellationToken cancellationToken);
}

public sealed class
DefaultBackgroundQueue<T> : IBackgroundQueue<T> {
    private readonly Channel<T> _queue;

    public DefaultBackgroundQueue() =>
        _queue = Channel.CreateUnbounded<T>(new() { SingleReader = true, SingleWriter = true });

    public async ValueTask EnqueueAsync(T value) =>
        await _queue.Writer.WriteAsync(value);

    public async ValueTask<T> DequeueAsync(CancellationToken cancellationToken) =>
        await _queue.Reader.ReadAsync(cancellationToken);
}