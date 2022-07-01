using Fluxor;
using Microsoft.AspNetCore.SignalR;
using ServerMonitorApp.Hubs;
using ServerMonitorCore;

namespace ServerMonitorApp.Reports;

public record UpdateIntervalAction(int UpdateIntervalSeconds);

[FeatureState]
public record MetricsReportSettings {
    public int UpdateIntervalSeconds { get; init; }

    public MetricsReportSettings(int updateIntervalSeconds) =>
        UpdateIntervalSeconds = updateIntervalSeconds;

    private MetricsReportSettings() : this(5) { }

    public static MetricsReportSettings
    Default => new();
}

public sealed class MetricsReportSettingsEffect {
    private readonly IHubContext<MonitorHub, IMonitorHubClient> _hubContext;
    private readonly ILogger _logger;

    public MetricsReportSettingsEffect(
        IHubContext<MonitorHub, IMonitorHubClient> hubContext,
        ILogger<MetricsReportSettingsEffect> logger
    ) {
        _hubContext = hubContext;
        _logger = logger;
    }

    [EffectMethod]
    public async Task OnUpdateInterval(UpdateIntervalAction action, IDispatcher dispatcher) {
        MonitorHub.CurrentConfiguration = new ConfigurationMessage(action.UpdateIntervalSeconds);
        _logger.LogWarning($"Sending {nameof(ConfigurationMessage)} to servers: {MonitorHub.CurrentConfiguration}");
        await _hubContext.Clients.All.ReceiveConfiguration(MonitorHub.CurrentConfiguration);
        dispatcher.Dispatch<MetricsReportSettings>(x => x with { UpdateIntervalSeconds = action.UpdateIntervalSeconds });
    }
}

