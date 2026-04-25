/* Rollback 111_DropGoldenManifestVersions_Legacy.sql — recreate empty legacy table shell (no row restore). */
IF OBJECT_ID(N'dbo.GoldenManifestVersions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GoldenManifestVersions
    (
        ManifestVersion NVARCHAR(50) NOT NULL PRIMARY KEY,
        RunId NVARCHAR(64) NOT NULL,
        SystemName NVARCHAR(200) NOT NULL,
        ManifestJson NVARCHAR(MAX) NOT NULL,
        ParentManifestVersion NVARCHAR(50) NULL,
        CreatedUtc DATETIME2 NOT NULL
    );

    ALTER TABLE dbo.GoldenManifestVersions
        ADD CONSTRAINT FK_GoldenManifestVersions_Parent FOREIGN KEY (ParentManifestVersion)
            REFERENCES dbo.GoldenManifestVersions (ManifestVersion);
END;
GO
