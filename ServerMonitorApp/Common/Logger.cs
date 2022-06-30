using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Configuration;
using System.Collections.Concurrent;
using ServerMonitorApp.Notifications;
using ServerMonitorCore.Common;

namespace ServerMonitorApp.Common;

public class LogMessage {
    public string Source { get; }
    public string Body { get; }
    public LogLevel LogLevel { get; }
    public int ThreadId { get; }
    public DateTimeOffset Timestamp { get; }
    public Exception? Exception { get; }

    public LogMessage(string source, string body, LogLevel logLevel, int threadId, DateTimeOffset timestamp, Exception? exception) {
        Source = source;
        Body = body;
        LogLevel = logLevel;
        ThreadId = threadId;
        Timestamp = timestamp;
        Exception = exception;
    }

    public override string ToString() => this.ToJson();
}

public sealed class Logger : ILogger {
    private readonly string _loggerName;
    private readonly IBackgroundQueue<LogMessage> _queue;

    public Logger(string loggerName, IBackgroundQueue<LogMessage> queue) {
        _loggerName = loggerName;
        _queue = queue;
    }

    public bool IsEnabled(LogLevel logLevel) => 
        logLevel != LogLevel.None;

    public IDisposable 
    BeginScope<TState>(TState state) => default!;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter
    ) {
        if (!IsEnabled(logLevel))
            return;

        _queue.EnqueueAsync(new LogMessage(
            logLevel: logLevel,
            threadId: Environment.CurrentManagedThreadId,
            source: _loggerName,
            body: $"{formatter(state, exception)}",
            timestamp: DateTimeOffset.Now, 
            exception: exception
        ));
    }
}

[ProviderAlias("Logger")]
public sealed class LoggerProvider : ILoggerProvider {
    private readonly ConcurrentDictionary<string, Logger> _loggers = new(StringComparer.OrdinalIgnoreCase);
    private readonly IBackgroundQueue<LogMessage> _queue;

    public LoggerProvider(IBackgroundQueue<LogMessage> queue) => _queue = queue;

    public ILogger CreateLogger(string categoryName) =>
        _loggers.GetOrAdd(categoryName, name => new Logger(name, _queue));

    public void Dispose() {
        _loggers.Clear();
    }
}

public static class InMemoryLoggerExtensions {

    public static ILoggingBuilder
    AddLogger(this ILoggingBuilder builder) {
        builder.AddConfiguration();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, LoggerProvider>());
        return builder;
    }
}