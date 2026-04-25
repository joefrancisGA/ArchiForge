/* Rollback 114_TryPilotRealModeRunColumns.sql */
IF OBJECT_ID(N'dbo.Runs', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.Runs', N'RealModeFellBackToSimulator') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Runs DROP CONSTRAINT IF EXISTS DF_Runs_RealModeFellBackToSimulator114;
    ALTER TABLE dbo.Runs DROP COLUMN RealModeFellBackToSimulator;
END;
GO

IF OBJECT_ID(N'dbo.Runs', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.Runs', N'PilotAoaiDeploymentSnapshot') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Runs DROP COLUMN PilotAoaiDeploymentSnapshot;
END;
GO
