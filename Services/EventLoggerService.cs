using System.Diagnostics;
using System.Runtime.Versioning;

namespace SQLScripRunner.Services;

internal sealed class EventLoggerService
{
    private readonly string _source; // Source for Event Log in Event Viewer
    private readonly string _logName; // Name of the Log to categorize it as Application Events

    public EventLoggerService(string source, string logName = "Application")
    {
        _source = source;
        _logName = logName;

        // Ensure Event Source Exists
        if (!EventLog.SourceExists(_source))
        {
            try
            {
                EventLog.CreateEventSource(_source, _logName);
                Console.WriteLine($"Event source '{_source}' created successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating event source: {ex.Message}");
            }
        }
    }

    public void LogInformation(string message, int eventId)
    {
        // Print to console and log to Event Viewer
        Console.WriteLine($"{message} - EventId: {eventId}");
        EventLog.WriteEntry(_source, $"{message}", EventLogEntryType.Information, eventId);
    }

    public void LogWarning(string message, int eventId)
    {
        // Print to console and log to Event Viewer
        Console.WriteLine($"{message} - EventId: {eventId}");
        EventLog.WriteEntry(_source, $"{message}", EventLogEntryType.Warning, eventId);
    }

    public void LogError(string message, int eventId)
    {
        // Print to console and log to Event Viewer
        Console.WriteLine($"{message} - EventId: {eventId}");
        EventLog.WriteEntry(_source, $"{message}", EventLogEntryType.Error, eventId);
    }

    public void LogCritical(string message, int eventId)
    {
        // Print to console and log to Event Viewer
        Console.WriteLine($"{message} - EventId: {eventId}");
        EventLog.WriteEntry(_source, $"{message}", EventLogEntryType.FailureAudit, eventId);
    }
}
