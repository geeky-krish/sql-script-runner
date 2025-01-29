using System.Data;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SQLScripRunner.Models;

namespace SQLScripRunner._helpers;

internal sealed class DatabaseManager
{
    private readonly ILogger<DatabaseManager> _logger;
    private readonly AppSettings _settings;
    private readonly EventLogger _eventLogger;

    public DatabaseManager(ILogger<DatabaseManager> logger, IOptions<AppSettings> options)
    {
        _logger = logger;
        _settings = options.Value;
        _eventLogger = new EventLogger(_settings.ApplicationName);
    }


    public DatabaseQuery CreateParameterizedQuery(string baseScript, Dictionary<string, object> parameters)
    {
        var query = new DatabaseQuery
        {
            SqlScript = baseScript
        };

        if (parameters != null)
        {
            foreach (var param in parameters)
            {
                query.Parameters.Add(new SqlParameter($"@{param.Key}", param.Value ?? DBNull.Value));
            }
        }

        return query;
    }


    public QueryResult<DataTable> GetDataFromDbTable(DatabaseQuery query)
    {
        var result = new QueryResult<DataTable>
        {
            Data = new DataTable()
        };

        if (string.IsNullOrEmpty(query?.SqlScript))
        {
            result.IsSuccess = false;
            result.ErrorMessage = "SQL script cannot be null or empty";
            _logger.LogError(result.ErrorMessage);
            _eventLogger.LogError(result.ErrorMessage, (int)EventIds.SqlScriptCannotBeEmpty);
            return result;
        }

        using (var connection = new SqlConnection(_settings.ConnectionStrings.TargetDB))
        {
            try
            {
                connection.Open();

                using (var command = new SqlCommand(query.SqlScript, connection))
                {
                    if (query.Parameters != null && query.Parameters.Any())
                    {
                        command.Parameters.AddRange(query.Parameters.ToArray());
                    }

                    using (var adapter = new SqlDataAdapter(command))
                    {
                        adapter.Fill(result.Data);
                    }
                }

                result.IsSuccess = true;
                return result;
            }
            catch (SqlException ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"Database error occurred: {ex.Message}";
                _logger.LogError(ex, "SQL Error occurred while executing query. Error Code: {ErrorCode}", ex.Number);
                _eventLogger.LogError($"SQL Error occurred while executing query. Error Code: {ex.Number}", (int)EventIds.SqlError);

                // Log additional SQL-specific information
                if (ex.Errors != null)
                {
                    foreach (SqlError error in ex.Errors)
                    {
                        _logger.LogError("SQL Error Details - Server: {Server}, Procedure: {Procedure}, Line: {Line}, Message: {Message}",
                            error.Server, error.Procedure, error.LineNumber, error.Message);

                        _eventLogger.LogError($"SQL Error Details - Server: {error.Server}, Procedure: {error.Procedure}, Line: {error.LineNumber}, Message: {error.Message}", (int)EventIds.SqlError);
                    }
                }
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"An unexpected error occurred: {ex.Message}";
                _logger.LogError(ex, "Unexpected error occurred while executing query");
                _eventLogger.LogError($"Unexpected error occurred while executing query, Exception Details: {ex.Message}", (int)EventIds.SqlError);
            }
        }

        return result;
    }

    public QueryExecutionResponse ExecuteQuery(string queryString)
    {
        using (SqlConnection connection = new SqlConnection(_settings.ConnectionStrings.TargetDB))
        {
            SqlTransaction? transaction = null;
            var response = new QueryExecutionResponse();
            int currentBatchNumber = 0;
            List<BatchMessage> batchMessages = new List<BatchMessage>();

            // Hook into the InfoMessage event on the connection to capture SQL Server messages
            connection.InfoMessage += (sender, args) =>
            {
                foreach (SqlError error in args.Errors)
                {
                    string severity = error.Class <= 10 ? "Information" : "Warning";
                    var message = new BatchMessage
                    {
                        BatchNumber = currentBatchNumber,
                        Message = error.Message,
                        Severity = severity,
                        LineNumber = error.LineNumber,
                        Timestamp = DateTimeOffset.UtcNow
                    };

                    batchMessages.Add(message);

                    _eventLogger.LogInformation($"Batch {currentBatchNumber}: {severity}: {error.Message} (Line: {error.LineNumber})", (int)EventIds.ExecutionBatchMessages);
                }
            };

            try
            {
                connection.Open();
                transaction = connection.BeginTransaction();

                if (string.IsNullOrWhiteSpace(queryString))
                {
                    throw new ArgumentException("SQL query string cannot be empty.", nameof(queryString));
                }

                // Split the script by "GO" (case insensitive) and trim each batch
                string[] batches = Regex.Split(queryString, @"^\s*GO\s*$",
                    RegexOptions.Multiline | RegexOptions.IgnoreCase)
                    .Where(b => !string.IsNullOrWhiteSpace(b))
                    .Select(b => b.Trim())
                    .ToArray();

                if (!batches.Any())
                {
                    throw new ArgumentException("No valid SQL commands found after parsing.", nameof(queryString));
                }

                foreach (var batch in batches)
                {
                    currentBatchNumber++;
                    try
                    {
                        using (SqlCommand command = new SqlCommand(batch, connection, transaction))
                        {
                            command.CommandTimeout = _settings.ScriptExecutionConfig.ExecutionTimeOutInSeconds;
                            _eventLogger.LogInformation($"Executing batch {currentBatchNumber} of {batches.Length}", (int)EventIds.ExecutionBatchMessages);

                            command.ExecuteNonQuery();

                        }
                    }
                    catch (SqlException sqlEx)
                    {
                        // Log error message
                        _eventLogger.LogError($"Error in batch {currentBatchNumber}: Line {sqlEx.LineNumber}, " + $"Message: {sqlEx.Message}, " + $"Batch Script: {batch}", (int)EventIds.SqlError);

                        throw new Exception($"Error in batch {currentBatchNumber}: Line {sqlEx.LineNumber}, " + $"Message: {sqlEx.Message}", sqlEx);
                    }
                }

                transaction.Commit();

                // Log final success message
                _eventLogger.LogInformation($"SQL script executed successfully. Completed {batches.Length} batches.", (int)EventIds.SqlScriptSuccess);

                response.IsSuccess = true;
                if (batchMessages is not null && batchMessages.Count > 0)
                {
                    response.ErrorMessage = JsonSerializer.Serialize(batchMessages);
                }
                return response;
            }
            catch (Exception ex)
            {
                if (transaction != null)
                {
                    try
                    {
                        transaction.Rollback();

                        // Log rollback message
                        _eventLogger.LogInformation("Transaction rolled back successfully.", (int)EventIds.RollBackSuccessful);
                    }
                    catch (Exception rollbackEx)
                    {
                        // Log rollback error message
                        _eventLogger.LogError($"Transaction rollback failed: {rollbackEx.Message}", (int)EventIds.RollBackException);
                        throw new AggregateException("Transaction rollback failed after execution error.", new[] { ex, rollbackEx });
                    }
                }

                _eventLogger.LogError($"SQL execution failed: {ex.Message}", (int)EventIds.SqlError);
                response.IsSuccess = false;
                response.ErrorMessage = ex.Message;
                return response;
            }
        }
    }

    public void LogScriptExecution(
        QueryExecutionResponse response,
        ScriptExecutionLog scriptExecutionLog,
        string? startMatch,
        string? lastMatch)
    {
        var parameters = new List<SqlParameter>
        {
            new SqlParameter("@ExecutionDate", SqlDbType.DateTimeOffset) { Value = scriptExecutionLog.ExecutionDate },
            new SqlParameter("@ExecutedTill", SqlDbType.VarChar) { Value = scriptExecutionLog.ExecutedTill },
            new SqlParameter("@ScriptVersion", SqlDbType.VarChar) { Value = scriptExecutionLog.ScriptVersion },
            new SqlParameter("@Status", SqlDbType.VarChar) { Value = response.IsSuccess ? "success" : "error" }
        };

        string insertQuery;
        if (response.IsSuccess)
        {
            insertQuery = @"
                INSERT INTO @LogTable (ExecutionDate, ExecutedTill, ScriptVersion, Status) 
                VALUES (@ExecutionDate, @ExecutedTill, @ScriptVersion, @Status)";
        }
        else
        {
            string errorLog = $"Error while executing from {startMatch} to {lastMatch}";
            string errorMessage = $"{errorLog}\n {response.ErrorMessage}";
            parameters.Add(new SqlParameter("@ErrorMessage", SqlDbType.VarChar) { Value = errorMessage });

            insertQuery = @"
                INSERT INTO @LogTable (ExecutionDate, ExecutedTill, ScriptVersion, Status, ErrorMessage) 
                VALUES (@ExecutionDate, @ExecutedTill, @ScriptVersion, @Status, @ErrorMessage)";
        }

        // Replace table name safely
        insertQuery = insertQuery.Replace("@LogTable", _settings.ScriptExecutionConfig.LogTable);
        ExecuteParameterizedQuery(insertQuery, parameters);
    }

    private void ExecuteParameterizedQuery(string query, List<SqlParameter> parameters)
    {
        using (SqlConnection connection = new SqlConnection(_settings.ConnectionStrings.TargetDB))
        {
            connection.Open();
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddRange(parameters.ToArray());
                command.ExecuteNonQuery();
            }
        }
    }
}
