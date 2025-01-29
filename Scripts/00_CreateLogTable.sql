/*
Author: Krishna Bogati
Date: JAN 29 2025

Description:
This script checks if the `LOG_ScriptExecutions` table exists in the database. 
If the table does not exist, it creates a new table to log the execution of scripts.
The table includes the following columns:
- ScriptExecutionId: A unique identifier for each script execution (Primary Key, Auto-incremented).
- ExecutionDate: The date and time when the script was executed.
- ExecutedTill: A marker indicating up to which point the script was successfully executed.
- ScriptVersion: The version of the script being executed.
- Status: The status of execution (e.g., Success, Failed).
- ErrorMessage: Stores error details if the script execution fails.

This ensures that script executions are logged systematically for tracking and debugging purposes.
*/
GO
IF  NOT EXISTS (SELECT * FROM sys.objects 
WHERE object_id = OBJECT_ID(N'LOG_ScriptExecutions') AND type in (N'U'))
BEGIN
	CREATE TABLE LOG_ScriptExecutions(
		ScriptExecutionId INT CONSTRAINT PK_LOG_ScriptExecutions PRIMARY KEY IDENTITY(1,1) NOT NULL,
		ExecutionDate DATETIMEOFFSET(7) NOT NULL,
		ExecutedTill VARCHAR(100) NULL,
		ScriptVersion VARCHAR(5) NULL,
		Status VARCHAR(10) NOT NULL,
		ErrorMessage VARCHAR(max) NULL
	)
END
GO