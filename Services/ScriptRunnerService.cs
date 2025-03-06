using System.Data;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SQLScriptRunner.Models;
using SQLScriptRunner.Common.Enums;

namespace SQLScriptRunner.Services;

internal class ScriptRunnerService
{
    private readonly ILogger<ScriptRunnerService> _logger;
    private readonly AppSettings _settings;
    private readonly ScriptExecManagerService _scriptExecManager;
    private string _script = string.Empty;

    public ScriptRunnerService(
        ILogger<ScriptRunnerService> logger,
        IOptions<AppSettings> options,
        ScriptExecManagerService scriptExecManager)
    {
        _logger = logger;
        _settings = options.Value;
        _scriptExecManager = scriptExecManager;
    }

    public void Run()
    {
        try
        {
            string[] artifactSourceFolder = Directory.GetFiles(_settings.ScriptConfig.Folders.ArtifactSourceFolder);

            if (artifactSourceFolder is not null && artifactSourceFolder.Length == 0)
            {
                _logger.LogInformation("There are no scripts found inside '{ArtifactSourceFolder}' directory.", _settings.ScriptConfig.Folders.ArtifactSourceFolder);
                return;
            }

            foreach (var item in artifactSourceFolder)
            {
                FileInfo fileInfo = new FileInfo(item);

                string extension = fileInfo.Extension;

                if (extension.Equals(".sql", StringComparison.OrdinalIgnoreCase))
                {
                    _script = File.ReadAllText(item);
                }
                else
                {
                    _logger.LogError("{Extension} is not a supported file format, .sql is only supported!", extension);
                }

                //execute script to find what is the latest execution point and from where the script should be executed. It is stored in a table.
                var scriptParameters = new Dictionary<string, object>
                {
                    {"Status", "success" }
                };

                _logger.LogInformation((int)EventIds.RegularWorkflow, "Started Reading Last Execution Point from {LogTable} database table.", _settings.ScriptExecutionConfig.LogTable);

                string scriptToReadExecutionLog = $"SELECT TOP(1) * FROM {_settings.ScriptExecutionConfig.LogTable} WHERE Status = @Status ORDER BY ScriptExecutionId DESC";
                var parameterizedQuery = _scriptExecManager.CreateParameterizedQuery(scriptToReadExecutionLog, scriptParameters);

                QueryResult<DataTable> result = _scriptExecManager.GetDataFromDbTable(parameterizedQuery);

                if (!result.IsSuccess)
                {
                    _logger.LogInformation((int)EventIds.CouldNotReadLastExecution, "Could not read last Execution Point from {LogTable} database table, Error Details: {ErrorMessage}.", _settings.ScriptExecutionConfig.LogTable, result.ErrorMessage);
                    return;
                }

                if (result.Data.Rows.Count == 0)
                {
                    _logger.LogInformation((int)EventIds.RegularWorkflow, "There is no record of last Execution Point in {LogTable} database table.", _settings.ScriptExecutionConfig.LogTable);

                    string scriptToExecute = _script;

                    ProcessScriptExecution(scriptToExecute);
                }
                else
                {
                    foreach (DataRow row in result.Data.Rows)
                    {
                        string lastExecutionPoint = row["ExecutedTill"].ToString();

                        _logger.LogInformation((int)EventIds.RegularWorkflow, "Last Execution Point from {LogTable} database table is {LastExecutionPoint}", _settings.ScriptExecutionConfig.LogTable, lastExecutionPoint);

                        int scriptStartIndex = _script.LastIndexOf(lastExecutionPoint);

                        string? scriptToExecute = string.Empty;
                        //string? scriptToExecute = _script?.TrimEnd().Substring(scriptStartIndex + lastExecutionPoint.Length);

                        if (scriptStartIndex == -1) 
                        {
                            scriptToExecute = _script;
                        }
                        else
                        {
                            scriptToExecute = _script?.TrimEnd().Substring(scriptStartIndex + lastExecutionPoint.Length);
                        }

                        if (String.IsNullOrWhiteSpace(scriptToExecute))
                        {
                            _logger.LogInformation((int)EventIds.RegularWorkflow, "No Script Found to Execute after {LastExecutionPoint} in file {fileName}", lastExecutionPoint, fileInfo.Name);
                        }
                        else
                        {
                            ProcessScriptExecution(scriptToExecute, lastExecutionPoint);
                        }
                    }
                }
            }
        }
        catch (SqlException ex)
        {
            _logger.LogError((int)EventIds.SqlError, "Sql Exception Caught while processing scripts, Exception Details: {ExMessage}", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError((int)EventIds.ExceptionCaught, "Exception Caught while processing scripts, Exception Details: {ExMessage}", ex.Message);
            throw;
        }
    }

    private void ProcessScriptExecution(string scriptToExecute, string lastExecutionPoint = "")
    {
        string scriptSourceFolder = _settings.ScriptConfig.Folders.ScriptSourceFolder;

        if (!Directory.Exists(scriptSourceFolder))
        {
            Directory.CreateDirectory(scriptSourceFolder);
        }

        string scriptStartPattern = _settings.ScriptConfig.ScriptStartPattern;
        string scriptEndPattern = _settings.ScriptConfig.ScriptEndPattern;

        Console.WriteLine(scriptStartPattern);
        Console.WriteLine(scriptEndPattern);
        MatchCollection startMatches = Regex.Matches(scriptToExecute, scriptStartPattern, RegexOptions.Multiline);
        MatchCollection endMatches = Regex.Matches(scriptToExecute, scriptEndPattern, RegexOptions.Multiline);


        if (endMatches is not null && endMatches.Count > 0)
        {
            /*
             * Below logic is to identify whether the script version is already executed. It checks the lastExecutionScriptVersion and the script version it matched in that file.
             * This happens when there are multiple files in a single directory.
             */
            if (!String.IsNullOrWhiteSpace(lastExecutionPoint))
            {
                var endMatchedScriptVersion = endMatches.Select(s => s.Value).Last();

                //It assumes the string is in (--[END-V-3]--) format as it looks in endMatches only
                int extractedEndMatchedVersion = int.Parse(endMatchedScriptVersion.Split('-')[4].TrimEnd(']'));

                int lastExecutedScriptVersion = int.Parse(lastExecutionPoint.Split('-')[4].TrimEnd(']'));
                if (lastExecutedScriptVersion > extractedEndMatchedVersion)
                {
                    _logger.LogInformation((int)EventIds.RegularWorkflow, "This script version {ScriptVersion} is already executed!", endMatches.Select(s => s.Value).LastOrDefault());
                    return;
                }
            }

            string fileName = $"script_till_V_{endMatches?.LastOrDefault()?.Groups[1].Value}";
            string fileLocation = Path.Combine(scriptSourceFolder, $"{fileName}{Path.GetExtension(".sql")}");

            File.WriteAllText(fileLocation, scriptToExecute);

            Match lastMatch = endMatches[endMatches.Count - 1];

            string scriptVersion = lastMatch.Groups[1].Value;

            QueryExecutionResponse response = _scriptExecManager.ExecuteQuery(scriptToExecute);

            var scriptExecutionLog = new ScriptExecutionLog
            {
                ExecutionDate = DateTimeOffset.UtcNow,
                ExecutedTill = lastMatch.Value,
                ScriptVersion = scriptVersion
            };

            _scriptExecManager.LogScriptExecution(response, scriptExecutionLog, startMatches[0]?.Value, lastMatch.Value);

            _logger.LogInformation((int)EventIds.SqlScriptSuccess, "Script executed Successfully.");
        }
        else
        {
            _logger.LogInformation((int)EventIds.RegularWorkflow, "There is nothing to execute, everything is up to date.");
        }
    }
}
