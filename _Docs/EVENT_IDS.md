# Event IDs Documentation

This document provides a structured reference for the various Event IDs used in the SQL Script Runner application.

| Event ID | Category                | Description |
|----------|-------------------------|-------------|
| 100      | Regular Workflow        | Application is working fine, running smoothly. |
| 1000     | Application Start/Stop  | Application started. |
| 1001     | Application Start/Stop  | Application started successfully. |
| 4002     | Application Start/Stop  | Application startup failed (unexpected error). |
| 4003     | Application Start/Stop  | Application failed to load configuration. |
| 4004     | Application Start/Stop  | Application failed due to missing dependencies. |
| 4005     | Application Start/Stop  | Application failed due to insufficient resources. |
| 1002     | SQL Execution           | SQL script execution started. |
| 1003     | SQL Execution           | SQL script executed successfully. |
| 1004     | SQL Execution           | SQL script execution skipped. |
| 1005     | SQL Execution           | No SQL script found in the artifact source directory. |
| 1006     | SQL Execution           | Non-SQL file found in the artifact source directory. |
| 1007     | SQL Execution           | Could not read last execution point. |
| 1008     | SQL Execution           | Empty SQL script file encountered. |
| 1009     | SQL Execution           | Rollback exception occurred. |
| 1010     | SQL Execution           | Rollback successful. |
| 1011     | SQL Execution           | SQL script execution batch messages. |
| 2000     | SQL Warnings            | SQL execution took longer than expected. |
| 2001     | SQL Warnings            | SQL warning (e.g., deprecated feature used). |
| 2002     | SQL Warnings            | SQL execution completed with warnings. |
| 3000     | SQL Errors              | SQL script execution failed due to a general error. |
| 3002     | SQL Errors              | SQL script execution failed due to timeout. |
| 3003     | SQL Errors              | SQL script execution failed due to permission issues. |
| 3004     | SQL Errors              | SQL script execution failed due to constraint violation. |
| 3005     | SQL Errors              | SQL script execution failed due to a deadlock. |
| 4000     | Critical Errors         | Database corruption detected. |
| 4001     | Critical Errors         | SQL Server instance is down/unreachable. |
| 4002     | Critical Errors         | Exception caught during processing of scripts. |

For further details, refer to the main documentation file.

