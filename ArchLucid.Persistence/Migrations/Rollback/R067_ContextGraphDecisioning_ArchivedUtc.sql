IF OBJECT_ID(N'dbo.DecisioningTraces', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.DecisioningTraces', N'ArchivedUtc') IS NOT NULL
    ALTER TABLE dbo.DecisioningTraces DROP COLUMN ArchivedUtc;

IF OBJECT_ID(N'dbo.GraphSnapshots', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.GraphSnapshots', N'ArchivedUtc') IS NOT NULL
    ALTER TABLE dbo.GraphSnapshots DROP COLUMN ArchivedUtc;

IF OBJECT_ID(N'dbo.ContextSnapshots', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.ContextSnapshots', N'ArchivedUtc') IS NOT NULL
    ALTER TABLE dbo.ContextSnapshots DROP COLUMN ArchivedUtc;
