-- Adds ROWVERSION columns for optimistic concurrency on high-churn tables (Runs wired in app code; others reserved for future updates).

IF COL_LENGTH('dbo.Runs', 'RowVersionStamp') IS NULL
    ALTER TABLE dbo.Runs ADD RowVersionStamp ROWVERSION;

IF COL_LENGTH('dbo.GoldenManifests', 'RowVersionStamp') IS NULL
    ALTER TABLE dbo.GoldenManifests ADD RowVersionStamp ROWVERSION;

IF COL_LENGTH('dbo.PolicyPackAssignments', 'RowVersionStamp') IS NULL
    ALTER TABLE dbo.PolicyPackAssignments ADD RowVersionStamp ROWVERSION;
