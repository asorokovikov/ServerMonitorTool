using Fluxor;
using ServerMonitorCore;
using System.Collections.Immutable;
using System.Diagnostics;
using ServerMonitorApp.Common;
using ServerMonitorCore.Notifications;

namespace ServerMonitorApp.Reports;

public record StartReceivingLogsAction();
public record StopReceivingLogsAction();

[FeatureState]
public class MetricsReportLogs {
    public ImmutableList<LogMessage> Messages { get; init; }

    public MetricsReportLogs(ImmutableList<LogMessage> messages) => Messages = messages;

    private MetricsReportLogs() : this(ImmutableList<LogMessage>.Empty) { }
}

public static class MetricsReportLogsHelper {

    public static MetricsReportLogs
    AddLogMessage(this MetricsReportLogs report, LogMessage message) => new(
        messages: report.Messages.Add(message)
    );
}

public sealed class MetricsReportLogsEffect : IDisposable {
    private readonly INotificationManager<LogMessage> _notificationManager;
    private IDispatcher? _dispatcher;
    private IDisposable? _disposable;

    public MetricsReportLogsEffect(INotificationManager<LogMessage> notificationManager) =>
        _notificationManager = notificationManager;

    [EffectMethod(typeof(StartReceivingLogsAction))]
    public Task OnStartReceivingLogs(IDispatcher dispatcher) {
        UnsubscribeIfPossible();
        _dispatcher = dispatcher;
        _disposable = _notificationManager.Subscribe(OnDataReceived);
        return Task.CompletedTask;
    }

    [EffectMethod(typeof(StopReceivingLogsAction))]
    public Task OnStopReceivingLogs(IDispatcher dispatcher) {
        UnsubscribeIfPossible();
        return Task.CompletedTask;
    }

    public void UnsubscribeIfPossible() {
        if (_disposable == null)
            return;
        _disposable.Dispose();
        _disposable = null;
    }

    public void OnDataReceived(LogMessage message) {
        Debug.Assert(_dispatcher != null);
        _dispatcher.Dispatch<MetricsReportLogs>(x => x.AddLogMessage(message));
    }

    public void Dispose() {
        UnsubscribeIfPossible();
    }
}
