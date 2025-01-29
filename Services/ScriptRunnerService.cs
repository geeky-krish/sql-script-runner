using System.Data;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SQLScripRunner.Common.Enums;
using SQLScripRunner.Models;

namespace SQLScripRunner.Services;

internal class ScriptRunnerService
{
    private readonly ILogger<ScriptRunnerService> _logger;
    private readonly AppSettings _settings;
    private readonly ScriptExecManagerService _databaseManager;
    private readonly EventLoggerService _eventLogger;
    private string _script = string.Empty;

    public ScriptRunnerService(
        ILogger<ScriptRunnerService> logger,
        IOptions<AppSettings> options,
        ScriptExecManagerService databaseManager)
    {
        _logger = logger;
        _settings = options.Value;
        _databaseManager = databaseManager;

        _eventLogger = new EventLoggerService(_settings.ApplicationName);
    }

    public void Run()
    {
        try
        {
            string[] artifactSourceFolder = Directory.GetFiles(_settings.ScriptConfig.Folders.ArtifactSourceFolder);

            if (artifactSourceFolder is not null && artifactSourceFolder.Length == 0)
            {
                _logger.LogInformation($"There are no scripts found inside '{_settings.ScriptConfig.Folders.ArtifactSourceFolder}' directory.");
                _eventLogger.LogInformation($"There are no scripts found inside '{_settings.ScriptConfig.Folders.ArtifactSourceFolder}' directory.", (int)EventIds.NoScriptsFound);
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
                    _eventLogger.LogError($"{extension} is not a supported file format, '.sql' is only supported!", (int)EventIds.NonSQLFileFound);
                    _logger.LogError($"{extension} is not a supported file format, .sql is only supported!");
                }

                //execute script to find what is the latest execution point and from where the script should be executed. It is stored in a table.
                var scriptParameters = new Dictionary<string, object>
                {
                    {"Status", "success" }
                };

                _logger.LogInformation($"Started Reading Last Execution Point from {_settings.ScriptExecutionConfig.LogTable} database table -EventId: {(int)EventIds.RegularWorkflow}");
                _eventLogger.LogInformation($"Started Reading Last Execution Point from {_settings.ScriptExecutionConfig.LogTable} database table", (int)EventIds.RegularWorkflow);

                string scriptToReadExecutionLog = $"SELECT TOP(1) * FROM {_settings.ScriptExecutionConfig.LogTable} WHERE Status = @Status ORDER BY ScriptExecutionId DESC";
                var parameterizedQuery = _databaseManager.CreateParameterizedQuery(scriptToReadExecutionLog, scriptParameters);

                QueryResult<DataTable> result = _databaseManager.GetDataFromDbTable(parameterizedQuery);

                if (!result.IsSuccess)
                {
                    _logger.LogInformation($"Could not read last Execution Point from {_settings.ScriptExecutionConfig.LogTable} database table, Error Details: {result.ErrorMessage}, -EventId: {(int)EventIds.CouldNotReadLastExecution}");
                    _eventLogger.LogInformation($"Could not read last Execution Point from {_settings.ScriptExecutionConfig.LogTable} database table, Error Details: {result.ErrorMessage}", (int)EventIds.CouldNotReadLastExecution);
                    return;
                }

                if (result.Data.Rows.Count == 0)
                {
                    _logger.LogInformation($"There is no record of last Execution Point in {_settings.ScriptExecutionConfig.LogTable} database table -EventId: {(int)EventIds.RegularWorkflow}");
                    _eventLogger.LogInformation($"There is no record of last Execution Point in {_settings.ScriptExecutionConfig.LogTable} database table", (int)EventIds.RegularWorkflow);

                    string scriptToExecute = _script;

                    ProcessScriptExecution(scriptToExecute);
                }
                else
                {
                    foreach (DataRow row in result.Data.Rows)
                    {
                        string lastExecutionPoint = row["ExecutedTill"].ToString();

                        _logger.LogInformation($"Last Execution Point from {_settings.ScriptExecutionConfig.LogTable} database table is '{lastExecutionPoint}' -EventId: {(int)EventIds.RegularWorkflow}");
                        _eventLogger.LogInformation($"Could not read last Execution Point from {_settings.ScriptExecutionConfig.LogTable} database table is '{lastExecutionPoint}'", (int)EventIds.RegularWorkflow);

                        int scriptStartIndex = _script.LastIndexOf(lastExecutionPoint);
                        string scriptToExecute = _script.Substring(scriptStartIndex + lastExecutionPoint.Length);

                        ProcessScriptExecution(scriptToExecute);
                    }
                }
            }
        }
        catch (SqlException ex)
        {
            _logger.LogError($"Sql Exception Caught while processing scripts -EventId: {(int)EventIds.SqlError}, Exception Details: {ex.Message}");
            _eventLogger.LogError($"Sql Exception Caught while processing scripts, Exception Details: {ex.Message}", (int)EventIds.SqlError);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Exception Caught while processing scripts -EventId: {(int)EventIds.ExceptionCaught}, Exception Details: {ex.Message}");
            _eventLogger.LogError($"Exception Caught while processing scripts, Exception Details: {ex.Message}", (int)EventIds.ExceptionCaught);
            throw;
        }
    }

    private void ProcessScriptExecution(string scriptToExecute)
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
            string fileName = $"script_till_V_{endMatches?.LastOrDefault()?.Groups[1].Value}";
            string fileLocation = Path.Combine(scriptSourceFolder, $"{fileName}{Path.GetExtension(".sql")}");

            File.WriteAllTextAsync(fileLocation, scriptToExecute);

            Match lastMatch = endMatches[endMatches.Count - 1];

            string scriptVersion = lastMatch.Groups[1].Value;

            QueryExecutionResponse response = _databaseManager.ExecuteQuery(scriptToExecute);

            var scriptExecutionLog = new ScriptExecutionLog
            {
                ExecutionDate = DateTimeOffset.UtcNow,
                ExecutedTill = lastMatch.Value,
                ScriptVersion = scriptVersion
            };

            _databaseManager.LogScriptExecution(response, scriptExecutionLog, startMatches[0]?.Value, lastMatch.Value);

            _logger.LogInformation($"Script executed Successfully -EventId: {(int)EventIds.SqlScriptSuccess}");
            _eventLogger.LogInformation($"Script executed Successfully", (int)EventIds.SqlScriptSuccess);
        }
        else
        {
            _logger.LogInformation("There is nothing to execute, everything is up to date.");
            _eventLogger.LogInformation($"There is nothing to execute, everything is up to date.", (int)EventIds.ExceptionCaught);
        }
    }
}
