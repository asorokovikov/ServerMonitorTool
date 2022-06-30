using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ServerMonitorCore.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ServerMonitorCore;

public interface IMetricsProvider {
    MetricsSnapshot GetShapshot();
}

public static class ServiceCollectionExtension {
    public static IServiceCollection
    AddMetrics(this IServiceCollection collection) {
        if (OperatingSystem.IsWindows())
            collection.AddSingleton<IMetricsProvider, WindowsMetricsProvider>();
        else if (OperatingSystem.IsLinux())
            collection.AddSingleton<IMetricsProvider, LinuxMetricsProvider>();
        else
            throw new NotImplementedException("Unsupported OS. Currently we only support Windows and Linux");
        return collection;
    }
}

[SupportedOSPlatform("Linux")]
public sealed class LinuxMetricsProvider : IMetricsProvider {
    public MetricsSnapshot GetShapshot() =>
        throw new NotImplementedException();
}

[SupportedOSPlatform("Windows")]
public sealed class WindowsMetricsProvider : IMetricsProvider {
    private readonly ILogger _logger;
    private PerformanceCounter? _processorUsageCounter;
    private PerformanceCounter? _availableMemoryCounter;
    private int _totalMemoryMBytes;

    public WindowsMetricsProvider(ILogger<WindowsMetricsProvider> logger) {
        _logger = logger;
        Initialize();
    }

    public MetricsSnapshot GetShapshot() {
        TryGetProcessorUsage(out var processorUsagePercent);
        TryGetAvailableMemoryMBytes(out var availableMemoryMBytes);
        return new MetricsSnapshot(
            machineName: Environment.MachineName,
            processorUsagePercent: processorUsagePercent,
            availableMemoryMBytes: availableMemoryMBytes,
            totalMemoryMBytes: _totalMemoryMBytes,
            drives: GetDriveMetrics()
        );
    }

    public bool TryGetAvailableMemoryMBytes(out int result) {
        if (_availableMemoryCounter != null) {
            result = (int) _availableMemoryCounter.NextValue();
            return true;
        }
        result = 0;
        return false;
    }

    public bool TryGetProcessorUsage(out float result) {
        if (_processorUsageCounter != null) {
            result = _processorUsageCounter.NextValue();
            return true;
        }
        result = 0;
        return false;
    }

    public static ImmutableList<DriveMetrics> GetDriveMetrics() =>
        DriveInfo.GetDrives().MapToImmutableList(x => new DriveMetrics(
            name: x.Name,
            availableFreeSpaceMBytes: x.AvailableFreeSpace.ToMBytes(),
            totalSizeMBytes: x.TotalSize.ToMBytes()
        ));

    private void Initialize() {
        TryCreatePerformanceCounter(
            category: "Processor",
            name: "% Processor Time",
            result: out _processorUsageCounter,
            instance: "_Total");
        TryCreatePerformanceCounter(
            category: "Memory",
            name: "Available MBytes",
            result: out _availableMemoryCounter);
        _totalMemoryMBytes = Native.GetPhysicalMemoryMBytes();
    }

    private bool TryCreatePerformanceCounter(
        string category,
        string name,
        [NotNullWhen(true)] out PerformanceCounter? result,
        string? instance = null
    ) {
        _logger?.LogInformation($"Creating performance counter: {category} {name}");
        try {
            result = new PerformanceCounter(category, name, instance);
            result.NextValue();
            _logger?.LogInformation($"{category} {name} counter has been created");
            return true;
        }
        catch (Exception exception) {
            _logger?.LogError(exception, $"Failed to create {category} {name} counter");
            result = null;
            return false;
        }
    }
}

internal static class Native {

    public static int GetPhysicalMemoryMBytes() {
        var memoryStatus = new MEMORYSTATUSEX();
        GlobalMemoryStatusEx(memoryStatus);
        return (int) (memoryStatus.ullTotalPhys / (1024 * 1024));
    }

    [StructLayout(LayoutKind.Sequential)]
    private class MEMORYSTATUSEX {
        #pragma warning disable 169 //disable unused field warning
        public uint dwLength = 64;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys; //The amount of actual physical memory, in bytes.
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
        #pragma warning restore 169
    }

    [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(MEMORYSTATUSEX lpBuffer);
}