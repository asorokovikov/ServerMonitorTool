using Microsoft.EntityFrameworkCore;

namespace ServerMonitorCore.Database;

public sealed class DatabaseContext : DbContext {
    public DbSet<ServerMetrics> Metrics => Set<ServerMetrics>();

    public DatabaseContext() {
        Database.EnsureCreated();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder builder) {
        builder.UseSqlite("Data Source=servermonitor.db");
    }
}