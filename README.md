# SQL Script Runner

## Overview
SQL Script Runner is a utility designed to execute SQL scripts in batches while logging execution details, warnings, and errors. It ensures transactional execution, maintaining database integrity by rolling back in case of failures.

## Features
- Executes SQL scripts in batches using `GO` separators.
- Logs SQL Server messages (info, warnings, errors) using Windows Event Log and SeriLog.
- Supports transactional execution with rollback on failure.
- Provides structured event logging with categorized event IDs.
- Allows configurable script start and end patterns for better script control.
- Notification through Email (yet to be implemented)

## Tech Stack
- **Backend:** C# (.NET)
- **Database:** SQL Server
- **Logging:** Windows Event Logging & SeriLog

## Prerequisites
Before starting the application, you must execute the required database setup script.  

 **Run the following SQL script in your database:**  
 [Database Setup Script](Scripts/00_CreateLogTable.sql)  

This script initializes necessary database tables required for logging script executions.


## Configuration
### `appsettings.json`
```json
{
    "ApplicationName": "SqlScriptRunner",
    "ConnectionStrings": {
        "TargetDB": "your-database-connection-string"
    },
    "ScriptConfig": {
        "ScriptStartPattern": "--\\[START-V-(\\d+)\\]--",
        "ScriptEndPattern": "--\\[END-V-(\\d+)\\]--",
        "Folders": {
            "ArtifactSourceFolder": "D:\\ArtifactSourceFolder",
            "ScriptSourceFolder": "D:\\ScriptSourceFolder"
        }
    },
    "ScriptExecutionConfig": {
        "LogTable": "LOG_ScriptExecutions",
        "ExecutionTimeOutInSeconds": 300
    }
}

```

## Usage
1. Ensure that your SQL Server database is accessible.
2. Execute the required [Database Setup Script](Scripts/00_CreateLogTable.sql).  
3. Configure `appsettings.json` with the correct connection string.
4. Place SQL scripts in the expected directory format with start and end markers.
5. Run the application to execute scripts in a transactional manner.

## Example SQL Script Format
```sql
--[START-V-1]
CREATE TABLE Test (
    ID INT PRIMARY KEY,
    Name NVARCHAR(50)
);
--[END-V-1]
```

## Logging & Event Monitoring
The application logs execution details to Windows Event Log.
For a complete list of Event IDs and their descriptions, refer to the [Event IDs Documentation](_Docs/EVENT_IDS.md).

## Contributions
Contributions are welcome! Feel free to submit issues or pull requests to improve functionality.

