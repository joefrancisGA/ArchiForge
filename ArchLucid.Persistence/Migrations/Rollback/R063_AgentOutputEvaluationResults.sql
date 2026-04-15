-- Rollback for migration 063: AgentOutputEvaluationResults table (operator-only; not run by DbUp).
-- Idempotent. WARNING: drops persisted evaluation rows.

IF OBJECT_ID(N'dbo.AgentOutputEvaluationResults', N'U') IS NOT NULL
    DROP TABLE dbo.AgentOutputEvaluationResults;
GO
