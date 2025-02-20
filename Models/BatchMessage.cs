namespace SQLScriptRunner.Models;

internal sealed class BatchMessage
{
    public int BatchNumber { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public int LineNumber { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
