namespace ConsoleClient;

public sealed class ServerConfiguration {
    public int UpdateIntervalMs { get; init; }
    public int ReconnectDelayMs { get; init; }
    public string HubUrl { get; init; } = default!;
}

public static class ServerConfigurationHelper {
    public static ServerConfiguration
    ReplaceUpdateInterval(this ServerConfiguration config, int updateIntervalMs) => new() {
        HubUrl = config.HubUrl,
        ReconnectDelayMs = config.ReconnectDelayMs,
        UpdateIntervalMs = updateIntervalMs
    };
}