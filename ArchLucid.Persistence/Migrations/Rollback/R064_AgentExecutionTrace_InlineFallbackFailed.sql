-- Rollback for migration 064: InlineFallbackFailed column (operator-only; not run by DbUp).
-- Idempotent.

IF COL_LENGTH(N'dbo.AgentExecutionTraces', N'InlineFallbackFailed') IS NOT NULL
    ALTER TABLE dbo.AgentExecutionTraces DROP COLUMN InlineFallbackFailed;
GO
