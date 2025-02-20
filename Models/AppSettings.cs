namespace SQLScriptRunner.Models;

internal sealed class AppSettings
{
    public required string ApplicationName { get; set; }
    public required ConnectionStrings ConnectionStrings { get; set; }
    public required ScriptConfig ScriptConfig { get; set; }
    public required ScriptExecutionConfig ScriptExecutionConfig { get; set; }
}

internal sealed class ConnectionStrings
{
    public required string TargetDB { get; set; }
}

internal sealed class ScriptConfig
{
    public required string ScriptStartPattern { get; set; }
    public required string ScriptEndPattern { get; set; }
    public required Folders Folders { get; set; }
}

internal sealed class Folders
{
    public required string ArtifactSourceFolder { get; set; }
    public required string ScriptSourceFolder { get; set; }
}

internal sealed class ScriptExecutionConfig
{
    public required string LogTable { get; set; }
    public required int ExecutionTimeOutInSeconds { get; set; }
}