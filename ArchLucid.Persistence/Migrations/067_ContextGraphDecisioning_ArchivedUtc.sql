-- Soft-archive alignment: cascade ArchivedUtc from dbo.Runs bulk-archive to context/graph/decisioning trace headers (RunId-aligned).
IF OBJECT_ID(N'dbo.ContextSnapshots', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.ContextSnapshots', N'ArchivedUtc') IS NULL
    ALTER TABLE dbo.ContextSnapshots ADD ArchivedUtc DATETIME2 NULL;

IF OBJECT_ID(N'dbo.GraphSnapshots', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.GraphSnapshots', N'ArchivedUtc') IS NULL
    ALTER TABLE dbo.GraphSnapshots ADD ArchivedUtc DATETIME2 NULL;

IF OBJECT_ID(N'dbo.DecisioningTraces', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.DecisioningTraces', N'ArchivedUtc') IS NULL
    ALTER TABLE dbo.DecisioningTraces ADD ArchivedUtc DATETIME2 NULL;
