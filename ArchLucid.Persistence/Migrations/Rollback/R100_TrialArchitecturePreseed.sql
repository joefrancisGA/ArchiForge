SET NOCOUNT ON;
GO

/* R100: Rollback 100_TrialArchitecturePreseed.sql — remove trial pre-seed queue columns from dbo.Tenants. */

IF COL_LENGTH(N'dbo.Tenants', N'TrialWelcomeRunId') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Tenants DROP COLUMN TrialWelcomeRunId;
END;
GO

IF COL_LENGTH(N'dbo.Tenants', N'TrialArchitecturePreseedEnqueuedUtc') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Tenants DROP COLUMN TrialArchitecturePreseedEnqueuedUtc;
END;
GO
