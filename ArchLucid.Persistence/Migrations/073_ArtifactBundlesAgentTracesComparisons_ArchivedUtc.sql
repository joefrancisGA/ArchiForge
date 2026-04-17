-- Soft-archive alignment: cascade ArchivedUtc from dbo.Runs bulk / by-id archival to
-- artifact bundles, agent execution traces, and comparison records (RunId-aligned).
IF OBJECT_ID(N'dbo.ArtifactBundles', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.ArtifactBundles', N'ArchivedUtc') IS NULL
    ALTER TABLE dbo.ArtifactBundles ADD ArchivedUtc DATETIME2 NULL;
GO

IF OBJECT_ID(N'dbo.AgentExecutionTraces', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AgentExecutionTraces', N'ArchivedUtc') IS NULL
    ALTER TABLE dbo.AgentExecutionTraces ADD ArchivedUtc DATETIME2 NULL;
GO

IF OBJECT_ID(N'dbo.ComparisonRecords', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.ComparisonRecords', N'ArchivedUtc') IS NULL
    ALTER TABLE dbo.ComparisonRecords ADD ArchivedUtc DATETIME2 NULL;
GO
