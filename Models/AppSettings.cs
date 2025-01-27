namespace SQLScripRunner.Models;

internal sealed class AppSettings
{
    public required ConnectionStrings ConnectionStrings { get; set; }
    public required ScriptConfig ScriptConfig { get; set; }
    public required DatabaseConfig DatabaseConfig { get; set; }
}

internal sealed class ConnectionStrings
{
    public string TargetDB { get; set; } = string.Empty;
}

internal sealed class ScriptConfig
{
    public string ScriptStartPattern { get; set; } = string.Empty;
    public string ScriptEndPattern { get; set; } = string.Empty;
    public required Folders Folders { get; set; }
}

internal sealed class Folders
{
    public string ArtifactSourceFolder { get; set; } = string.Empty;
    public string ScriptSourceFolder { get; set; } = string.Empty;
}

internal sealed class DatabaseConfig
{
    public string LogTable { get; set; } = string.Empty;
}