namespace SQLScriptRunner.Common.Enums;

public enum EventIds
{
    // Regular Workflow logs
    RegularWorkflow = 100,              // Application is working fine, Running smoothly

    // Application Start/Stop Events
    AppStart = 1000,                    // Application started.
    AppStartedSuccessfully = 1001,      // Application started successfully.
    AppStartFailed = 4002,              // Application startup failed (unexpected error).
    AppStartConfigFailed = 4003,        // Application failed to load configuration.
    AppStartDependencyFailed = 4004,    // Application failed due to missing dependencies.
    AppStartResourceFailed = 4005,      // Application failed due to insufficient resources.

    // SQL Execution Events
    SqlScriptStart = 1002,              // SQL script execution started.
    SqlScriptSuccess = 1003,            // SQL script executed successfully.
    SqlScriptSkipped = 1004,            // SQL script execution skipped.
    NoScriptsFound = 1005,              // No SQL script Found in the Artifact Source directory.
    NonSQLFileFound = 1006,             // Non SQL File Found in the Artifact Source directory.
    CouldNotReadLastExecution = 1007,   // Could not read last execution point.
    SqlScriptCannotBeEmpty = 1008,      // Empty SQL Script file encountered.
    RollBackException = 1009,           // Rollback exception occurred.
    RollBackSuccessful = 1010,          // Rollback successful.
    ExecutionBatchMessages = 1011,      // SQL Script Execution Batch Messages

    // SQL Warnings
    SqlExecutionWarning = 2000,         // SQL execution took longer than expected.
    SqlWarning = 2001,                  // SQL warning (e.g., deprecated feature used).
    SqlExecutionWithWarning = 2002,     // SQL execution completed with warnings.

    // SQL Errors
    SqlError = 3000,                    // SQL script execution failed due to a general error.
    SqlTimeoutError = 3002,             // SQL script execution failed due to timeout.
    SqlPermissionError = 3003,          // SQL script execution failed due to permission issues.
    SqlConstraintViolation = 3004,      // SQL script execution failed due to constraint violation.
    SqlDeadlockError = 3005,            // SQL script execution failed due to a deadlock.

    // Critical Errors
    DatabaseCorruption = 4000,          // Database corruption detected.
    SqlServerDown = 4001,               // SQL Server instance is down/unreachable.
    ExceptionCaught = 4002              // Exception Caught During processing of Scripts.
}

