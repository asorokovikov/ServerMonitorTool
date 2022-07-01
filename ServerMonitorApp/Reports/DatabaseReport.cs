using Fluxor;

namespace ServerMonitorApp.Reports;

[FeatureState]
public record DatabaseReport {
    public DatabaseStatistics Statistics { get; init; }
    public DatabaseConnection Connection { get; init; }

    public DatabaseReport(DatabaseStatistics statistics, DatabaseConnection connection) {
        Statistics = statistics;
        Connection = connection;
    }

    private DatabaseReport() : this(
        statistics: DatabaseStatistics.Default, 
        connection: DatabaseConnection.Default
        ) { }

    public static DatabaseReport
    Default => new();
}

public record DatabaseStatistics(
    int MetricsCount, 
    long DatabaseSizeMB, 
    DateTimeOffset Timestamp
) {
    public static DatabaseStatistics Default => new(
        MetricsCount: 0,
        DatabaseSizeMB: 0,
        Timestamp: DateTimeOffset.MinValue
    );
}

public record DatabaseConnection(string Host, int Port, string DatabaseName, DatabaseState State) {
    public static DatabaseConnection Default => new(string.Empty, 0, string.Empty, DatabaseState.Offline);
}

public enum DatabaseState {
    Online = 0,
    Offline = 1,
    Connecting = 2,
}