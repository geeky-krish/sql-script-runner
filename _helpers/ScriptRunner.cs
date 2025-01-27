using System.Data;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SQLScripRunner.Models;

namespace SQLScripRunner._helpers;

internal class ScriptRunner
{
    private readonly ILogger<ScriptRunner> _logger;
    private readonly AppSettings _settings;
    private string _script = string.Empty;

    public ScriptRunner(ILogger<ScriptRunner> logger, IOptions<AppSettings> options)
    {
        _logger = logger;
        _settings = options.Value;
    }

    public void Run()
    {
        try
        {
            string[] artifactSourceFolder = Directory.GetFiles(_settings.ScriptConfig.Folders.ArtifactSourceFolder);

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
                    _logger.LogError($"{extension} is not a supported file format, .sql is only supported!");
                }

                string scriptToReadExecutionLog = $"SELECT TOP(1) * FROM {_settings.DatabaseConfig.LogTable} WHERE Status = 'success' ORDER BY ExecutionId DESC";

                DataTable dataTable = GetDataFromDbTable(scriptToReadExecutionLog);

                foreach (DataRow row in dataTable.Rows)
                {
                    string? lastExecutionPoint = row["ExecutedTill"].ToString();
                    if (string.IsNullOrWhiteSpace(lastExecutionPoint))
                    {
                        //If this is the case consider it is being executed for the first time.
                    }
                    else
                    {
                        int scriptStartIndex = _script.LastIndexOf(lastExecutionPoint);
                        string scriptToExecute = _script.Substring(scriptStartIndex + lastExecutionPoint.Length);

                        string scriptSourceFolder = _settings.ScriptConfig.Folders.ScriptSourceFolder;

                        if (!Directory.Exists(scriptSourceFolder))
                        {
                            Directory.CreateDirectory(scriptSourceFolder);
                        }

                        string scriptStartPattern = _settings.ScriptConfig.ScriptStartPattern;
                        string scriptEndPattern = _settings.ScriptConfig.ScriptEndPattern;

                        MatchCollection startMatches = Regex.Matches(scriptToExecute, scriptStartPattern, RegexOptions.Multiline);
                        MatchCollection endMatches = Regex.Matches(scriptToExecute, scriptEndPattern, RegexOptions.Multiline);

                        if (endMatches is not null && endMatches.Count > 0)
                        {
                            string fileName = $"script_executed_till_{endMatches?.LastOrDefault()?.Groups[1].Value}";
                            string fileLocation = Path.Combine(scriptSourceFolder, ($"{fileName}{Path.GetExtension(".sql")}"));

                            File.WriteAllTextAsync(fileLocation, scriptToExecute);


                            Match lastMatch = endMatches[endMatches.Count - 1];

                            string versionNumber = lastMatch.Groups[1].Value;

                            QueryExecutionResponse response = ExecuteQuery(scriptToExecute);

                            if (response.IsSuccess)
                            {
                                //Create query to insert success result
                            }
                            else
                            {
                                //Create query to insert error result
                            }
                        }

                    }

                }

            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message.ToString());
            throw;
        }

    }

    private QueryExecutionResponse ExecuteQuery(string queryString)
    {
        using (SqlConnection connection = new SqlConnection(_settings.ConnectionStrings.TargetDB))
        {
            connection.Open();
            using (SqlTransaction transaction = connection.BeginTransaction())
            {
                try
                {
                    // Split the script by "GO" (case insensitive) and trim each batch
                    string[] batches = Regex.Split(queryString, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
                    foreach (var batch in batches)
                    {
                        if (!string.IsNullOrWhiteSpace(batch))
                        {
                            using (SqlCommand command = new SqlCommand(batch, connection, transaction))
                            {
                                command.ExecuteNonQuery();
                            }
                        }
                    }
                    transaction.Commit();
                    Console.WriteLine("Script executed successfully.");
                    return new QueryExecutionResponse { IsSuccess = true, ErrorMessage = string.Empty };
                }
                catch (Exception ex)
                {
                    try
                    {
                        transaction.Rollback();
                    }
                    catch (Exception rollbackEx)
                    {
                        Console.WriteLine("Rollback failed: " + rollbackEx.Message);
                    }
                    Console.WriteLine("Error executing script: " + ex.Message);
                    return new QueryExecutionResponse { IsSuccess = false, ErrorMessage = ex.Message };
                }
            }
        }
    }


    private DataTable GetDataFromDbTable(string script)
    {
        DataTable dataTable = new DataTable();
        using (SqlConnection connection = new SqlConnection(_settings.ConnectionStrings.TargetDB))
        {
            try
            {
                connection.Open();
                using (SqlDataAdapter adapter = new SqlDataAdapter(script, connection))
                {
                    adapter.Fill(dataTable);
                }
            }
            catch (SqlException e)
            {
                _logger.LogError($"SQL Error: {e.Message}");
                Console.WriteLine($"SQL Error: {e.Message}");
            }
            catch (Exception e)
            {
                _logger.LogError($"Error: {e.Message}");
                Console.WriteLine($"Error: {e.Message}");
            }
        }
        return dataTable;
    }
}
