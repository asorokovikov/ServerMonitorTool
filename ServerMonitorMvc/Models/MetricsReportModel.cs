using System.Collections.Immutable;
using ServerMonitorCore;

namespace ServerMonitorMvc.Models;

public sealed class MetricsReportModel {
    public IReadOnlyCollection<ServerMetrics> Metrics { get; init; }
    public int UpdateIntervalSeconds { get; init; }
    public MetricsReportModel(IEnumerable<ServerMetrics> items, int updateIntervalSeconds) {
        Metrics = items.ToImmutableList();
        UpdateIntervalSeconds = updateIntervalSeconds;
    }

}

public static class MetricsHelper {
    public static TimeSpan
    GetElapsedTime(this ServerMetrics metrics) =>
        DateTimeOffset.Now.Subtract(metrics.Timestamp);
}