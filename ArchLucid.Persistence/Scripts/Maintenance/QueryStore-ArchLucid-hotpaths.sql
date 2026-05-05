/*
    Read-only Query Store diagnostics for Azure SQL Database / SQL Server (ArchLucid workload).

    Purpose (step 1 — where time goes):
      - Rank stable query shapes by weighted avg duration and logical reads over a recent window.
      - Surface observed max_duration per shape (tail hint; not a true percentile).

    Prerequisites:
      - Query Store enabled and READ_WRITE on this database (default on Azure SQL).
      - Reader permission on catalog views below.

    Usage (examples):
      sqlcmd -S <server>.database.windows.net -d <database> -G -i QueryStore-ArchLucid-hotpaths.sql
      -- Or SSMS / Azure Data Studio: open this file, set database context, execute.

    Tunables:
*/

DECLARE @Hours INT = 168; /* rolling window; default 7 days */

IF EXISTS (
    SELECT 1
    FROM sys.database_query_store_options AS qso
    WHERE qso.actual_state = 0 /* OFF — no new snapshots until enabled */)
BEGIN
    PRINT N'WARNING: Query Store actual_state is OFF; enable READ_WRITE or expect empty/stale results.';
END;

/* ---- Weighted averages by logical query text (trim duplicate plans sharing one text id). ---- */

SELECT TOP 45
    SUM(rs.count_executions) AS total_executions,
    SUM(rs.avg_duration * rs.count_executions)
        / CONVERT(DECIMAL(38, 6), NULLIF(SUM(rs.count_executions), 0))
        / 1000.0 AS weighted_avg_duration_ms,
    MAX(rs.max_duration) / 1000.0 AS observed_max_duration_ms_any_interval,
    SUM(rs.avg_logical_io_reads * rs.count_executions)
        / CONVERT(DECIMAL(38, 6), NULLIF(SUM(rs.count_executions), 0)) AS weighted_avg_logical_io_reads,
    COUNT(DISTINCT rs.plan_id) AS distinct_plans_touched,
    MIN(rs.first_execution_time) AS first_seen_in_window_utc,
    MAX(rs.last_execution_time) AS last_seen_in_window_utc,
    qt.query_sql_text
FROM sys.query_store_runtime_stats AS rs
INNER JOIN sys.query_store_plan AS p ON rs.plan_id = p.plan_id
INNER JOIN sys.query_store_query AS q ON p.query_id = q.query_id
INNER JOIN sys.query_store_query_text AS qt ON q.query_text_id = qt.query_text_id
WHERE rs.last_execution_time >= DATEADD(HOUR, -@Hours, SYSUTCDATETIME())
GROUP BY qt.query_sql_text
ORDER BY weighted_avg_duration_ms DESC;

/* ---- Same ranking but ordered by weighted logical reads (IO-heavy shapes). ---- */

SELECT TOP 45
    SUM(rs.count_executions) AS total_executions,
    SUM(rs.avg_logical_io_reads * rs.count_executions)
        / CONVERT(DECIMAL(38, 6), NULLIF(SUM(rs.count_executions), 0)) AS weighted_avg_logical_io_reads,
    SUM(rs.avg_duration * rs.count_executions)
        / CONVERT(DECIMAL(38, 6), NULLIF(SUM(rs.count_executions), 0))
        / 1000.0 AS weighted_avg_duration_ms,
    MAX(rs.max_duration) / 1000.0 AS observed_max_duration_ms_any_interval,
    qt.query_sql_text
FROM sys.query_store_runtime_stats AS rs
INNER JOIN sys.query_store_plan AS p ON rs.plan_id = p.plan_id
INNER JOIN sys.query_store_query AS q ON p.query_id = q.query_id
INNER JOIN sys.query_store_query_text AS qt ON q.query_text_id = qt.query_text_id
WHERE rs.last_execution_time >= DATEADD(HOUR, -@Hours, SYSUTCDATETIME())
GROUP BY qt.query_sql_text
ORDER BY weighted_avg_logical_io_reads DESC;

/* ---- Narrow slice: statements referencing core ArchLucid tables (edit list as needed). ---- */

SELECT TOP 60
    SUM(rs.count_executions) AS total_executions,
    SUM(rs.avg_duration * rs.count_executions)
        / CONVERT(DECIMAL(38, 6), NULLIF(SUM(rs.count_executions), 0))
        / 1000.0 AS weighted_avg_duration_ms,
    SUM(rs.avg_logical_io_reads * rs.count_executions)
        / CONVERT(DECIMAL(38, 6), NULLIF(SUM(rs.count_executions), 0)) AS weighted_avg_logical_io_reads,
    qt.query_sql_text
FROM sys.query_store_runtime_stats AS rs
INNER JOIN sys.query_store_plan AS p ON rs.plan_id = p.plan_id
INNER JOIN sys.query_store_query AS q ON p.query_id = q.query_id
INNER JOIN sys.query_store_query_text AS qt ON q.query_text_id = qt.query_text_id
WHERE rs.last_execution_time >= DATEADD(HOUR, -@Hours, SYSUTCDATETIME())
  AND (
      qt.query_sql_text LIKE N'%dbo.Runs%'
      OR qt.query_sql_text LIKE N'%dbo.AuditEvents%'
      OR qt.query_sql_text LIKE N'%dbo.GoldenManifests%'
      OR qt.query_sql_text LIKE N'%dbo.FindingsSnapshots%'
      OR qt.query_sql_text LIKE N'%dbo.ProductLearningPilotSignals%');
GROUP BY qt.query_sql_text
ORDER BY weighted_avg_duration_ms DESC;
