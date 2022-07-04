namespace ConsoleClient;

public sealed class ServerConfiguration {
    public int UpdateIntervalMs { get; init; }
    public int ReconnectDelayMs { get; init; }
    public string HubName { get; init; } = default!;
    public string Host { get; init; } = default!;
    public string Ports { get; init; } = default!;

    public IReadOnlyCollection<string> GetConnectionStrings() {
        //"HubUrl": "https://localhost:7143/monitorhub"
        var results = new List<string>();
        foreach (var portString in Ports.Split(',')) {
            if (!int.TryParse(portString, out var port))
                throw new InvalidOperationException("Failed to parse ports from the configuration file");
            results.Add($"https://{Host}:{port}/{HubName}");
        }

        return results;
    }
}

public static class ServerConfigurationHelper {
    public static ServerConfiguration
    ReplaceUpdateInterval(this ServerConfiguration config, int updateIntervalMs) => new() {
        UpdateIntervalMs = updateIntervalMs,
        ReconnectDelayMs = config.ReconnectDelayMs,
        HubName = config.HubName,
        Host = config.Host,
        Ports = config.Ports
    };
}