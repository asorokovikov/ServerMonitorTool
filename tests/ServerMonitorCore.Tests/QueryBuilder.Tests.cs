using FluentAssertions;
using ServerMonitorCore.Database;
using static ServerMonitorCore.Database.QueryBuilders.QueryBuilderColumnType;
using static ServerMonitorCore.Database.QueryBuilders.QueryBuilderColumnAttributes;

namespace ServerMonitorCore.Tests;

public sealed class QueryBuilderTests {

    [Fact]
    public void CreateDatabaseTest() {
        const string databaseName = "database";
        var query = QueryBuilder.CreateDatabaseQuery(databaseName);
        query.Should().Be($"CREATE DATABASE {databaseName};");
    }

    [Fact]
    public void DropDatabaseIfExistsTest() {
        const string databaseName = "database";
        var query = QueryBuilder.DropDatabaseIfExistsQuery(databaseName);
        query.Should().Be($"DROP DATABASE IF EXISTS {databaseName};");
    }

    [Fact]
    public void DatabasesListTest() {
        var query = QueryBuilder.DatabasesListQuery;
        query.Should().Be($"SELECT datname FROM pg_database;");
    }

    [Fact]
    public void CreateTable_OneColumn_Test() {
        var query = QueryBuilder.CreateTable("table").WithColumn("column", Integer).Build();
        query.Should().Be("CREATE TABLE table (column int);");
    }

    [Fact]
    public void Select_Columns_Test() {
        QueryBuilder.Select("table").Build()
            .Should().Be("SELECT * FROM table;");

        QueryBuilder.Select("table").WithColumn("first").Build()
            .Should().Be("SELECT first FROM table;");

        QueryBuilder.Select("table").WithColumn("first", "second", "third").Build()
            .Should().Be("SELECT first, second, third FROM table;");
    }

    [Fact]
    public void Select_EmptyColumnException_Test() {
        QueryBuilder.Select("table")
            .Invoking(x => x.WithColumn("first", "", "third"))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateTable_WithColumns_Test() {
        QueryBuilder.CreateTable("table").WithColumn("column", Integer).Build()
            .Should().Be("CREATE TABLE table (column int);");

        QueryBuilder.CreateTable("table")
            .WithColumn("column1", Integer)
            .WithColumn("column2", Real)
            .WithColumn("column3", Timestamp)
            .Build()
            .Should().Be("CREATE TABLE table (column1 int,column2 real,column3 timestamp);");
    }

    [Fact]
    public void CreateTable_WithColumn_Attributes_Test() {
        QueryBuilder.CreateTable("table").WithColumn("id", Serial, PrimaryKey).Build()
            .Should().Be("CREATE TABLE table (id serial PRIMARY KEY);");

        QueryBuilder.CreateTable("table")
            .WithColumn("column1", Integer, PrimaryKey)
            .WithColumn("column2", Real, NotNull)
            .WithColumn("column3", Timestamp, NotNull, Unique)
            .Build()
            .Should().Be("CREATE TABLE table (column1 int PRIMARY KEY,column2 real NOT NULL,column3 timestamp NOT NULL UNIQUE);");
    }
}