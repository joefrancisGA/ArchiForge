IF COL_LENGTH(N'dbo.Tenants', N'TrialSampleRunId') IS NOT NULL
    ALTER TABLE dbo.Tenants DROP COLUMN TrialSampleRunId;
GO

IF COL_LENGTH(N'dbo.Tenants', N'TrialStatus') IS NOT NULL
    ALTER TABLE dbo.Tenants DROP COLUMN TrialStatus;
GO

IF COL_LENGTH(N'dbo.Tenants', N'TrialSeatsUsed') IS NOT NULL
    ALTER TABLE dbo.Tenants DROP COLUMN TrialSeatsUsed;
GO

IF COL_LENGTH(N'dbo.Tenants', N'TrialSeatsLimit') IS NOT NULL
    ALTER TABLE dbo.Tenants DROP COLUMN TrialSeatsLimit;
GO

IF COL_LENGTH(N'dbo.Tenants', N'TrialRunsUsed') IS NOT NULL
    ALTER TABLE dbo.Tenants DROP COLUMN TrialRunsUsed;
GO

IF COL_LENGTH(N'dbo.Tenants', N'TrialRunsLimit') IS NOT NULL
    ALTER TABLE dbo.Tenants DROP COLUMN TrialRunsLimit;
GO

IF COL_LENGTH(N'dbo.Tenants', N'TrialExpiresUtc') IS NOT NULL
    ALTER TABLE dbo.Tenants DROP COLUMN TrialExpiresUtc;
GO

IF COL_LENGTH(N'dbo.Tenants', N'TrialStartUtc') IS NOT NULL
    ALTER TABLE dbo.Tenants DROP COLUMN TrialStartUtc;
GO
