namespace SQLScriptRunner.Models;

internal sealed class QueryExecutionResponse
{
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}
