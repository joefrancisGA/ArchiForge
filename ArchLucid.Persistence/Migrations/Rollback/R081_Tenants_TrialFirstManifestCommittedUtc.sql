IF COL_LENGTH(N'dbo.Tenants', N'TrialFirstManifestCommittedUtc') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Tenants DROP COLUMN TrialFirstManifestCommittedUtc;
END;
GO
