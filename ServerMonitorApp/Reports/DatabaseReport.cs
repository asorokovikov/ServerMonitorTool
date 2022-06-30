using Fluxor;

namespace ServerMonitorApp.Reports;

[FeatureState]
public record DatabaseReport {
    public int UpdateIntervalSeconds {get;init;}

    public DatabaseReport(int updateIntervalSeconds) =>
    UpdateIntervalSeconds = updateIntervalSeconds;

    private DatabaseReport() : this(5) { }

    public static DatabaseReport
    Default => new();
}
