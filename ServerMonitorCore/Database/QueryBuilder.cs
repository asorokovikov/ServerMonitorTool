using ServerMonitorCore.Common;
using ServerMonitorCore.Database.QueryBuilders;

namespace ServerMonitorCore.Database;

public interface IBuilder {
    string Build();
}

public static class QueryBuilder { 

    public static string CreateDatabaseQuery(string databaseName) => 
        $"CREATE DATABASE {databaseName.VerifyNotEmpty(nameof(databaseName))};";

    public static string DropDatabaseIfExistsQuery(string databaseName) => 
        $"DROP DATABASE IF EXISTS {databaseName.VerifyNotEmpty(nameof(databaseName))};";

    public static string DatabasesListQuery => "SELECT datname FROM pg_database;";

    public static ISelectQueryBuilder Select(string tableName) => 
        new SelectQueryBuilder(tableName);

    public static ICreateTableQueryBuilder CreateTable(string tableName) => 
        new CreateTableQueryBuilder(tableName);

}
