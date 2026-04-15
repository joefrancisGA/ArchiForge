-- Rollback for migration 065: IX_AgentExecutionTraces_InlineFallbackFailed (operator-only; not run by DbUp).
-- Idempotent.

IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_AgentExecutionTraces_InlineFallbackFailed'
      AND object_id = OBJECT_ID(N'dbo.AgentExecutionTraces'))
    DROP INDEX IX_AgentExecutionTraces_InlineFallbackFailed ON dbo.AgentExecutionTraces;
GO
