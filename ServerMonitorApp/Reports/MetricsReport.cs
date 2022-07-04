using Fluxor;
using ServerMonitorCore;
using System.Collections.Immutable;
using System.Diagnostics;
using ServerMonitorCore.Notifications;

namespace ServerMonitorApp.Reports;

public record StartReceivingMetricsAction();
public record StopReceivingMetricsAction();


[FeatureState]
public record MetricsReport {
    public ImmutableDictionary<string, ServerMetrics> ConnectionIdToMetrics { get; init; }
    
    public MetricsReport(ImmutableDictionary<string, ServerMetrics> connectionIdToMetrics) => 
        ConnectionIdToMetrics = connectionIdToMetrics;

    public static MetricsReport Default => new();

    private MetricsReport() : this(ImmutableDictionary<string, ServerMetrics>.Empty) { }

    public TimeSpan GetElapsedTime(string connectionId) =>
        DateTimeOffset.Now.Subtract(ConnectionIdToMetrics[connectionId].Timestamp);

    public bool IsConnectionLost(string connectionId, int updateIntervalSeconds) {
        var elapsedTime = GetElapsedTime(connectionId);
        return elapsedTime.TotalSeconds > updateIntervalSeconds * 1.5;
    }
}

public static class MetricsReportHelper {

    public static MetricsReport
    AddServerMetrics(this MetricsReport report, ServerMetrics metrics) => new(
        connectionIdToMetrics: report.ConnectionIdToMetrics.SetItem(metrics.ConnectionId, metrics)
    );

    public static MetricsReport
    RemoveServer(this MetricsReport report, string connectionId) => new(
        connectionIdToMetrics: report.ConnectionIdToMetrics.Remove(connectionId)
    );
}

public sealed class MetricsReportEffect : IDisposable {
    private readonly INotificationManager<ServerMetrics> _notificationManager;
    private IDispatcher? _dispatcher;
    private IDisposable? _disposable;

    public MetricsReportEffect(INotificationManager<ServerMetrics> notificationManager) => 
        _notificationManager = notificationManager;

    [EffectMethod(typeof(StartReceivingMetricsAction))]
    public Task OnStartReceivingMetrics(IDispatcher dispatcher) {
        UnsubscribeIfPossible();
        _dispatcher = dispatcher;
        _disposable = _notificationManager.Subscribe(OnDataReceived);
        return Task.CompletedTask;
    }

    [EffectMethod(typeof(StopReceivingMetricsAction))]
    public Task OnStopReceivingMetrics(IDispatcher _) {
        UnsubscribeIfPossible();
        return Task.CompletedTask;
    }

    public void UnsubscribeIfPossible() {
        if (_disposable == null)
            return;
        _disposable.Dispose();
        _disposable = null;
    }

    public void OnDataReceived(ServerMetrics metrics) {
        Debug.Assert(_dispatcher != null);
        _dispatcher.Dispatch<MetricsReport>(x => x.AddServerMetrics(metrics));
    }

    public void Dispose() {
        UnsubscribeIfPossible();
    }
}
