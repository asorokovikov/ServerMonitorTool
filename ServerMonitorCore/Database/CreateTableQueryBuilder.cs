using ServerMonitorCore.Common;
using System.Text;

namespace ServerMonitorCore.Database;

public interface ICreateTableQueryBuilder : IBuilder, ICreateTableQuery { }

public interface ICreateTableQuery {
    ICreateTableQueryBuilder WithColumn(string columnName, QueryBuilderColumnType type, params QueryBuilderColumnAttributes[] attributes);
}

public enum QueryBuilderColumnType {

    [Name("serial")] 
    Serial,

    [Name("int")] 
    Integer,

    [Name("real")]
    Real,

    [Name("timestamp")]
    Timestamp,

    [Name("cidr")]
    IpAddress
}

public enum QueryBuilderColumnAttributes {
    [Name("PRIMARY KEY")]
    PrimaryKey,

    [Name("NOT NULL")]
    NotNull,

    [Name("UNIQUE")]
    Unique,
}

public sealed class CreateTableQueryBuilder : ICreateTableQueryBuilder {
    private readonly StringBuilder _builder = new();

    public CreateTableQueryBuilder(string tableName) {
        tableName.VerifyNotEmpty(nameof(tableName));
        _builder.Remove(0, _builder.Length);
        _builder.Append($"CREATE TABLE {tableName} (");
    } 

    public ICreateTableQueryBuilder
    WithColumn(string columnName, QueryBuilderColumnType type, params QueryBuilderColumnAttributes[] attributes) {
        _builder.Append($"{columnName.VerifyNotEmpty(nameof(columnName))}");
        _builder.Append($" {type.GetName()}");
        attributes.ForEach(x => _builder.Append($" {x.GetName()}"));
        _builder.Append(",");
        return this;
    }

    public string Build() {
        if (_builder[^1] == ',') {
            _builder.Remove(_builder.Length - 1, 1);
            _builder.Append(");");
        }
        return _builder.ToString();
    }
}