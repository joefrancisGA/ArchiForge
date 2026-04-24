/*
  114: Pilot `archlucid try --real` provenance — persist simulator fallback after Azure OpenAI failure.
*/
IF OBJECT_ID(N'dbo.Runs', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.Runs', N'RealModeFellBackToSimulator') IS NULL
BEGIN
    ALTER TABLE dbo.Runs ADD
        RealModeFellBackToSimulator BIT NOT NULL CONSTRAINT DF_Runs_RealModeFellBackToSimulator114 DEFAULT (0),
        PilotAoaiDeploymentSnapshot NVARCHAR(256) NULL;
END;
GO
