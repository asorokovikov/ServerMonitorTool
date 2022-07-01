using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using ServerMonitorApp.Notifications;
using ServerMonitorCore;

namespace ServerMonitorApp.Hubs;

public sealed class 
MonitorHub : Hub<IMonitorHubClient> {
    public static ConfigurationMessage CurrentConfiguration = ConfigurationMessage.Default;
    private readonly IBackgroundQueue<ServerMetrics> _queue;
    private readonly ILogger _logger;

    public MonitorHub(IBackgroundQueue<ServerMetrics> queue, ILogger<MonitorHub> logger) {
        _queue = queue;
        _logger = logger;
    }

    public override async Task OnConnectedAsync() {  
        _logger.LogInformation($"Server connected (Id={Context.ConnectionId})");
        _logger.LogWarning($"Sending {nameof(ConfigurationMessage)}={CurrentConfiguration} to server with Id={Context.ConnectionId}");
        await Clients.Caller.SendConfiguration(CurrentConfiguration);
        await base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception) {
        var message = $"Server disconnected (Id={Context.ConnectionId})";
        if (exception != null)
            _logger.LogError(exception, message);
        else
            _logger.LogWarning(message);
        return base.OnDisconnectedAsync(exception);
    }

    public async Task
    SendMetrics(MetricsSnapshot snapshot) {
        var feature = Context.Features.Get<IHttpConnectionFeature>();
        var serverIp = feature?.RemoteIpAddress?.MapToIPv4().ToString() ?? "none";
        _logger.LogInformation($"Received metrics: {snapshot.MachineName} {snapshot}");
        var message = snapshot.ToServerMetrics(connectionId: Context.ConnectionId, ipAddress: serverIp, timestamp: DateTimeOffset.Now);
        await _queue.EnqueueAsync(message);
    }
}