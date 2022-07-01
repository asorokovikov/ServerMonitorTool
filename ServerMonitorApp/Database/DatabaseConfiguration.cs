using Npgsql;

namespace ServerMonitorApp.Database;

public sealed class DatabaseConfiguration {
    public string Host { get; init; } = default!;
    public int Port { get; init; }
    public string Username { get; init; } = default!;
    public string Password { get; init; } = default!;
    public string DatabaseName { get; init; } = default!;

    public string ConnectionString => $"Host={Host};Port={Port};Username={Username};Password={Password};";
    public string ConnectionDatabaseString => $"{ConnectionString};Database={DatabaseName};";
}

public static class DatabaseConfigurationHelper {
    public static NpgsqlConnection GetConnection(this DatabaseConfiguration configuration) => 
        new(configuration.ConnectionDatabaseString);
}