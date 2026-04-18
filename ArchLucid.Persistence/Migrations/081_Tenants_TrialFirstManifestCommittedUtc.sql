SET NOCOUNT ON;
GO

IF COL_LENGTH(N'dbo.Tenants', N'TrialFirstManifestCommittedUtc') IS NULL
BEGIN
    ALTER TABLE dbo.Tenants ADD TrialFirstManifestCommittedUtc DATETIMEOFFSET NULL;
END;
GO
