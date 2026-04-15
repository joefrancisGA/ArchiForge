-- Rollback for migration 063: drop AgentOutputEvaluationResults table.
-- WARNING: Deletes all persisted reference-case evaluation scores.

IF OBJECT_ID(N'dbo.AgentOutputEvaluationResults', N'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.AgentOutputEvaluationResults;
END
