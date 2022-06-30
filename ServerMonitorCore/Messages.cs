using ServerMonitorCore.Common;

namespace ServerMonitorCore;

public interface IMonitorHubClient {
    Task SendConfiguration(ConfigurationMessage message);
}

public sealed class 
ConfigurationMessage {
    public int UpdateIntervalSeconds { get; }

    public ConfigurationMessage(int updateIntervalSeconds) => 
        UpdateIntervalSeconds = updateIntervalSeconds;

    public static ConfigurationMessage Default => new(5);

    public override string ToString() => this.ToJson();
}
