using System.Collections.Concurrent;
using Serilog.Core;
using Serilog.Events;

namespace aspnet.Logging;

public sealed class LogBufferSink : ILogEventSink, IDisposable
{
    private readonly int _maxEntries;
    private readonly ConcurrentQueue<BufferedLogEntry> _entries = new();
    private int _count;

    public LogBufferSink(int maxEntries = 2000)
    {
        _maxEntries = maxEntries;
    }

    public void Emit(LogEvent logEvent)
    {
        var entry = new BufferedLogEntry
        {
            Timestamp = logEvent.Timestamp,
            Level = logEvent.Level.ToString(),
            Message = logEvent.RenderMessage(),
            Exception = logEvent.Exception?.ToString(),
            Properties = logEvent.Properties
                .Where(p => p.Key != "SourceContext" && p.Key != "RequestId" && p.Key != "RequestPath")
                .ToDictionary(p => p.Key, p => p.Value.ToString().Trim('"'))
        };

        if (logEvent.Properties.TryGetValue("SourceContext", out var sourceCtx))
            entry.SourceContext = sourceCtx.ToString().Trim('"');

        _entries.Enqueue(entry);

        if (Interlocked.Increment(ref _count) > _maxEntries)
        {
            _entries.TryDequeue(out _);
            Interlocked.Decrement(ref _count);
        }
    }

    public IReadOnlyList<BufferedLogEntry> GetEntries() => _entries.ToList();

    public void Dispose()
    {
        _entries.Clear();
    }
}

public class BufferedLogEntry
{
    public DateTimeOffset Timestamp { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
    public string? SourceContext { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new();
}
