namespace SQLScriptRunner.Models;

internal sealed class ScriptExecutionLog
{
    public int ScriptExecutionId { get; set; }
    public DateTimeOffset ExecutionDate { get; set; }
    public string? ExecutedTill { get; set; } = string.Empty;
    public string? ScriptVersion { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; } = string.Empty;
}
