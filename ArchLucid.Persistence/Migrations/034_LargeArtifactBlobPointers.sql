-- Large artifact pointers: optional blob URIs alongside inline NVARCHAR(MAX) (dual-write read prefers blob when present).

IF COL_LENGTH('dbo.GoldenManifests', 'ManifestPayloadBlobUri') IS NULL
    ALTER TABLE dbo.GoldenManifests ADD ManifestPayloadBlobUri NVARCHAR(2000) NULL;
GO

IF COL_LENGTH('dbo.ArtifactBundles', 'BundlePayloadBlobUri') IS NULL
    ALTER TABLE dbo.ArtifactBundles ADD BundlePayloadBlobUri NVARCHAR(2000) NULL;
GO

IF COL_LENGTH('dbo.ArtifactBundleArtifacts', 'ContentBlobUri') IS NULL
    ALTER TABLE dbo.ArtifactBundleArtifacts ADD ContentBlobUri NVARCHAR(2000) NULL;
GO
