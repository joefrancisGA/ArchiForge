/* ADR 0030 PR A4 — hard-drop legacy coordinator manifest row store (owner decision 35d, pre-release). */
IF OBJECT_ID(N'dbo.GoldenManifestVersions', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_GoldenManifestVersions_Parent')
        ALTER TABLE dbo.GoldenManifestVersions DROP CONSTRAINT FK_GoldenManifestVersions_Parent;

    DROP TABLE dbo.GoldenManifestVersions;
END
GO
