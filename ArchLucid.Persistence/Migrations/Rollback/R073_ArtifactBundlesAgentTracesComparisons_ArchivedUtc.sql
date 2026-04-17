IF COL_LENGTH(N'dbo.ComparisonRecords', N'ArchivedUtc') IS NOT NULL
    ALTER TABLE dbo.ComparisonRecords DROP COLUMN ArchivedUtc;
GO

IF COL_LENGTH(N'dbo.AgentExecutionTraces', N'ArchivedUtc') IS NOT NULL
    ALTER TABLE dbo.AgentExecutionTraces DROP COLUMN ArchivedUtc;
GO

IF COL_LENGTH(N'dbo.ArtifactBundles', N'ArchivedUtc') IS NOT NULL
    ALTER TABLE dbo.ArtifactBundles DROP COLUMN ArchivedUtc;
GO
