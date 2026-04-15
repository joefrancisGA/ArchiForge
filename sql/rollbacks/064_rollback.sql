-- Rollback for migration 064: remove InlineFallbackFailed column from AgentExecutionTraces.
-- WARNING: Loses per-row fallback failure flags.

IF COL_LENGTH(N'dbo.AgentExecutionTraces', N'InlineFallbackFailed') IS NOT NULL
BEGIN
    ALTER TABLE dbo.AgentExecutionTraces DROP COLUMN InlineFallbackFailed;
END
