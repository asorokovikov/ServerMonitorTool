using System.Collections.Immutable;
using ServerMonitorCore;

namespace ServerMonitorMvc.Models;

public sealed class MetricsReportModel {
    public IReadOnlyCollection<ServerMetrics> Metrics { get; init; }
    public MetricsReportModel(IEnumerable<ServerMetrics> items) {
        Metrics = items.ToImmutableList();
    }
}
