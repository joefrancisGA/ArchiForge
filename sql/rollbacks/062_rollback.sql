-- Rollback for migration 062: remove inline full-text prompt/response columns.
-- WARNING: Loses SQL-stored forensic text that was not in blob storage.

IF COL_LENGTH(N'dbo.AgentExecutionTraces', N'FullSystemPromptInline') IS NOT NULL
    ALTER TABLE dbo.AgentExecutionTraces DROP COLUMN FullSystemPromptInline;

IF COL_LENGTH(N'dbo.AgentExecutionTraces', N'FullUserPromptInline') IS NOT NULL
    ALTER TABLE dbo.AgentExecutionTraces DROP COLUMN FullUserPromptInline;

IF COL_LENGTH(N'dbo.AgentExecutionTraces', N'FullResponseInline') IS NOT NULL
    ALTER TABLE dbo.AgentExecutionTraces DROP COLUMN FullResponseInline;
