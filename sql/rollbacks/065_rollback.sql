-- Rollback for migration 065: drop filtered index on AgentExecutionTraces.InlineFallbackFailed.
-- WARNING: Data loss risk is low (index only); verify no dependent plans before running.

IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_AgentExecutionTraces_InlineFallbackFailed'
      AND object_id = OBJECT_ID(N'dbo.AgentExecutionTraces'))
BEGIN
    DROP INDEX IX_AgentExecutionTraces_InlineFallbackFailed ON dbo.AgentExecutionTraces;
END
