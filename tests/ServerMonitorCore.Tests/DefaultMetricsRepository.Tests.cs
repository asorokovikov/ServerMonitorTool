using FluentAssertions;
using ServerMonitorCore.Database;

namespace ServerMonitorCore.Tests;

public sealed class DefaultMetricsRepositoryTests {
    private readonly DatabaseConfiguration _configuration = new () {
        DatabaseName = "servermonitor_test",
        Host = "localhost",
        Port = 5432,
        Username = "postgres",
        Password = "postgres",
        MetricsTableName = "metrics"
    };

    [Fact]
    public async Task DatabasesListTest() {
        var databaseName = UniqueString.GetString(prefix: "db_");

        await using var connection = _configuration.GetConnectionToPostgresql();
        await connection.OpenAsync();
        await connection.CreateDatabaseAsync(databaseName);
        var databasesList = await connection.GetDatabasesListAsync();

        databasesList.Should().Contain(databaseName);

        await connection.DeleteDatabaseIfExistsAsync(databaseName);
    }

    [Fact]
    public async Task CreateMetricsTableTest() {
        var database = _configuration.DatabaseName;
        var tableName = _configuration.MetricsTableName;

        await CreateDatabaseIfNotExists(database);

        await using var connection = _configuration.GetConnectionToDatabase();
        await connection.OpenAsync();

        //var builder = QueryBuilder.Create(database);
        //builder.NewTable(tableName)
        //    .WithColumn("metrics_id", "serial PRIMARY KEY")
        //    .WithColumn("ip_address", "cidr NOT NULL")
        //    .WithColumn("processor_usage_percent", "real NOT NULL")
        //    .WithColumn("available_memory_mbytes", "int NOT NULL")
        //    .WithColumn("total_memory_mbytes", "int NOT NULL")
        //    .WithColumn("timestamp", "timestamp NOT NULL");

    }

    public async Task CreateDatabaseIfNotExists(string databaseName) {
        await using var connection = _configuration.GetConnectionToPostgresql();
        await connection.OpenAsync();
        var databases = await connection.GetDatabasesListAsync();
        if (databases.NotContains(databaseName))
            await connection.CreateDatabaseAsync(databaseName);
    }
}