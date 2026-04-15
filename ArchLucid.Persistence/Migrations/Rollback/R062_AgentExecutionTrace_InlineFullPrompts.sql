-- Rollback for migration 062: optional inline full prompt/response columns (operator-only; not run by DbUp).
-- Idempotent.

IF COL_LENGTH(N'dbo.AgentExecutionTraces', N'FullResponseInline') IS NOT NULL
    ALTER TABLE dbo.AgentExecutionTraces DROP COLUMN FullResponseInline;

IF COL_LENGTH(N'dbo.AgentExecutionTraces', N'FullUserPromptInline') IS NOT NULL
    ALTER TABLE dbo.AgentExecutionTraces DROP COLUMN FullUserPromptInline;

IF COL_LENGTH(N'dbo.AgentExecutionTraces', N'FullSystemPromptInline') IS NOT NULL
    ALTER TABLE dbo.AgentExecutionTraces DROP COLUMN FullSystemPromptInline;
GO
