using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using ServerMonitorCore.Common;
using System.Text;

namespace ServerMonitorCore.Database;

public interface IQueryBuilder {
    ICreateTableQueryBuilder NewTable(string tableName);
    IBuilder CreateDatabase();
    IBuilder DropDatabaseIfExists();
    ISelectQueryBuilder Select(string tableName);
    IBuilder Get();
}

public interface IBuilder {
    string Build();
}

public interface IFilterQueryBuilder {
    IBuilder Where(string column, string value);
}


public abstract class QueryBuilder { 

    private readonly StringBuilder _builder = new ();

    private QueryBuilder() { }

    public static string CreateDatabaseQuery(string databaseName) => 
        $"CREATE DATABASE {databaseName.VerifyNotEmpty(nameof(databaseName))};";

    public static string DropDatabaseIfExistsQuery(string databaseName) => 
        $"DROP DATABASE IF EXISTS {databaseName.VerifyNotEmpty(nameof(databaseName))};";

    public static string DatabasesListQuery => "SELECT datname FROM pg_database;";

    public static ISelectQueryBuilder Select(string tableName) => 
        new SelectQueryBuilder(tableName);

    public static ICreateTableQueryBuilder CreateTable(string tableName) => 
        new CreateTableQueryBuilder(tableName);

    public string Build() {
        if (_builder[^1] == ',') {
            _builder.Remove(_builder.Length - 1, 1);
            _builder.Append(");");
        }
        return _builder.ToString();
    }

}


public static class QueryBuilderHelper {


}