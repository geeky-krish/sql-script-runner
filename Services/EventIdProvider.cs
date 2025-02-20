using Serilog.Events;
using Serilog.Sinks.EventLog;

namespace SQLScriptRunner.Services;

/// <summary>
/// Provides an implementation of <see cref="IEventIdProvider"/> for mapping 
/// Serilog log events to specific event IDs when writing to the Windows Event Log.
/// </summary>
public class EventIdProvider : IEventIdProvider
{

    /// <summary>
    /// A singleton instance of <see cref="EventIdProvider"/>.
    /// </summary>
    public static EventIdProvider Instance { get; } = new EventIdProvider();

    /// <summary>
    /// Computes an event ID for the given <see cref="LogEvent"/>.
    /// </summary>
    /// <param name="logEvent">The log event to extract the event ID from.</param>
    /// <returns>
    /// The extracted event ID if available, otherwise a fallback event ID 
    /// based on the log level.
    /// </returns>
    ushort IEventIdProvider.ComputeEventId(LogEvent logEvent)
    {
        if (logEvent.Properties.TryGetValue("EventId", out var eventIdProperty) &&
            eventIdProperty is StructureValue structure &&
            structure.Properties.FirstOrDefault(p => p.Name == "Id")?.Value.ToString() is string eventIdStr &&
            ushort.TryParse(eventIdStr, out ushort eventId))
        {
            return eventId;
        }

        // Fallback IDs based on log level
        return logEvent.Level switch
        {
            LogEventLevel.Error => 10000,
            LogEventLevel.Warning => 20000,
            LogEventLevel.Information => 30000,
            LogEventLevel.Debug => 40000,
            LogEventLevel.Verbose => 50000,
            _ => 0
        };
    }
}