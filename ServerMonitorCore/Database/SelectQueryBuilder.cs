using ServerMonitorCore.Common;
using System.Diagnostics;
using System.Text;

namespace ServerMonitorCore.Database;

public interface ISelectQueryBuilder : IBuilder {
    IBuilder WithColumn(params string[] columns);
}

public sealed class SelectQueryBuilder : ISelectQueryBuilder, IBuilder {

    private readonly StringBuilder _builder = new();
    private readonly string _tableName;

    public SelectQueryBuilder(string tableName) => 
        _tableName = tableName.VerifyNotEmpty(nameof(tableName));

    public IBuilder WithColumn(params string[] columns) {
        _builder.Append("SELECT ");
        foreach (var column in columns) {
            _builder.AppendFormat("{0}, ", column.VerifyNotEmpty(nameof(column)));
        }
        return this;
    }

    public string Build() {
        if (_builder.Length == 0)
            return $"SELECT * FROM {_tableName};";

        Debug.Assert(_builder.Length > 2);
        _builder.Remove(_builder.Length - 2, 2);
        _builder.Append($" FROM {_tableName};");
        return _builder.ToString();
    }
}