using ServerMonitorCore.Common;
using System.Collections.Immutable;
using System.Net;

namespace ServerMonitorCore;

public sealed class
MetricsSnapshot {
    public string MachineName { get; }
    public float ProcessorUsagePercent { get; }
    public int AvailableMemoryMBytes { get; }
    public int TotalMemoryMBytes { get; }
    public ImmutableList<DriveMetrics> Drives { get; }

    public MetricsSnapshot(
        string machineName,
        float processorUsagePercent, 
        int availableMemoryMBytes, 
        int totalMemoryMBytes, 
        ImmutableList<DriveMetrics> drives
    ) {
        MachineName = machineName;
        ProcessorUsagePercent = processorUsagePercent.VerifyGreaterOrEqualZero();
        AvailableMemoryMBytes = availableMemoryMBytes.VerifyGreaterOrEqualZero();
        TotalMemoryMBytes = totalMemoryMBytes.VerifyGreaterZero();
        Drives = drives;
    }

    public float MemoryAvailablePercent =>
        TotalMemoryMBytes > 0 ? AvailableMemoryMBytes * 100f / TotalMemoryMBytes : 0;

    public float MemoryUsagePercent => 
        TotalMemoryMBytes > 0 ? 100 - MemoryAvailablePercent : -1f;

    public float DrivesUsagePercent {
        get {
            var totalSizeMBytes = Drives.Sum(x => x.TotalSizeMBytes);
            if (totalSizeMBytes <= 0)
                return -1f;
            var availablePercent = Drives.Sum(x => x.AvailableFreeSpaceMBytes) * 100f / totalSizeMBytes;
            return 100 - availablePercent;
        }
    }

    public int MemoryUsageMBytes => TotalMemoryMBytes - AvailableMemoryMBytes;

    public string MemoryUsage => $"{TotalMemoryMBytes - AvailableMemoryMBytes} / {TotalMemoryMBytes} MB";

    public string DrivesUsage => Drives.Aggregate(string.Empty, (s, drive) => s + drive + Environment.NewLine);

    public override string ToString() =>
        $"CPU: {ProcessorUsagePercent:F2}%, Memory: {MemoryUsageMBytes.FromMBytes().Gigabytes:F2} / {TotalMemoryMBytes.FromMBytes().Gigabytes:F2} GB, Drives: "  +
        Drives.Aggregate(string.Empty, (s, drive) => s + drive + Environment.NewLine);
}

public sealed class 
DriveMetrics {
    public string Name { get; }
    public int AvailableFreeSpaceMBytes { get; }
    public int TotalSizeMBytes { get; }

    public DriveMetrics(string name, int availableFreeSpaceMBytes, int totalSizeMBytes) {
        Name = name;
        AvailableFreeSpaceMBytes = availableFreeSpaceMBytes.VerifyGreaterOrEqualZero();
        TotalSizeMBytes = totalSizeMBytes.VerifyGreaterZero();
    }

    public override string ToString() {
        var unit = TotalSizeMBytes.FromMBytes().LargestUnit;
        return $"{Name} - {AvailableFreeSpaceMBytes.FromMBytes().Humanize(unit)}/{TotalSizeMBytes.FromMBytes().Humanize(unit)}";
    }
}

public sealed class
ServerMetrics {
    public MetricsSnapshot Snapshot { get; }
    public string ConnectionId { get; }
    public IPAddress IpAddress { get; }
    public DateTimeOffset Timestamp { get; }

    public ServerMetrics(MetricsSnapshot snapshot, string connectionId, IPAddress ipAddress, DateTimeOffset timestamp) {
        Snapshot = snapshot;
        ConnectionId = connectionId;
        IpAddress = ipAddress;
        Timestamp = timestamp;
    }
}

public static class MetricsHelper {
    public static ServerMetrics
    ToServerMetrics(this MetricsSnapshot snapshot, string connectionId, IPAddress ipAddress, DateTimeOffset timestamp) =>
        new(snapshot: snapshot, connectionId: connectionId, ipAddress: ipAddress, timestamp: timestamp);
}