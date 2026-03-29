-- Relational artifact bundle slices (dual-write with ArtifactsJson / TraceJson). Mirrors ArchiForge.Data/SQL/ArchiForge.sql.
-- DbUp-only deployments may not run ISchemaBootstrapper; ensure dbo.ArtifactBundles exists before child tables.
IF OBJECT_ID(N'dbo.ArtifactBundles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ArtifactBundles
    (
        BundleId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        RunId UNIQUEIDENTIFIER NOT NULL,
        ManifestId UNIQUEIDENTIFIER NOT NULL,
        CreatedUtc DATETIME2 NOT NULL,
        ArtifactsJson NVARCHAR(MAX) NOT NULL,
        TraceJson NVARCHAR(MAX) NOT NULL,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        INDEX IX_ArtifactBundles_RunId NONCLUSTERED (RunId),
        INDEX IX_ArtifactBundles_ManifestId NONCLUSTERED (ManifestId)
    );
END;

IF OBJECT_ID(N'dbo.ArtifactBundles', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.Runs', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.GoldenManifests', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ArtifactBundles_Runs_RunId')
        ALTER TABLE dbo.ArtifactBundles ADD CONSTRAINT FK_ArtifactBundles_Runs_RunId
            FOREIGN KEY (RunId) REFERENCES dbo.Runs (RunId);

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ArtifactBundles_GoldenManifests_ManifestId')
        ALTER TABLE dbo.ArtifactBundles ADD CONSTRAINT FK_ArtifactBundles_GoldenManifests_ManifestId
            FOREIGN KEY (ManifestId) REFERENCES dbo.GoldenManifests (ManifestId);
END;

IF OBJECT_ID(N'dbo.ArtifactBundleArtifacts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ArtifactBundleArtifacts
    (
        BundleId UNIQUEIDENTIFIER NOT NULL,
        SortOrder INT NOT NULL,
        ArtifactId UNIQUEIDENTIFIER NOT NULL,
        RunId UNIQUEIDENTIFIER NOT NULL,
        ManifestId UNIQUEIDENTIFIER NOT NULL,
        CreatedUtc DATETIME2 NOT NULL,
        ArtifactType NVARCHAR(500) NOT NULL,
        Name NVARCHAR(2000) NOT NULL,
        Format NVARCHAR(200) NOT NULL,
        Content NVARCHAR(MAX) NOT NULL,
        ContentHash NVARCHAR(128) NOT NULL,
        CONSTRAINT PK_ArtifactBundleArtifacts PRIMARY KEY (BundleId, SortOrder),
        CONSTRAINT UQ_ArtifactBundleArtifacts_ArtifactId UNIQUE (BundleId, ArtifactId),
        CONSTRAINT FK_ArtifactBundleArtifacts_Bundles FOREIGN KEY (BundleId)
            REFERENCES dbo.ArtifactBundles (BundleId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_ArtifactBundleArtifacts_BundleId
        ON dbo.ArtifactBundleArtifacts (BundleId);
END;

IF OBJECT_ID(N'dbo.ArtifactBundleArtifactMetadata', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ArtifactBundleArtifactMetadata
    (
        BundleId UNIQUEIDENTIFIER NOT NULL,
        ArtifactSortOrder INT NOT NULL,
        MetaSortOrder INT NOT NULL,
        MetaKey NVARCHAR(500) NOT NULL,
        MetaValue NVARCHAR(MAX) NOT NULL,
        CONSTRAINT PK_ArtifactBundleArtifactMetadata PRIMARY KEY (BundleId, ArtifactSortOrder, MetaSortOrder),
        CONSTRAINT FK_ArtifactBundleArtifactMetadata_Artifacts FOREIGN KEY (BundleId, ArtifactSortOrder)
            REFERENCES dbo.ArtifactBundleArtifacts (BundleId, SortOrder) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_ArtifactBundleArtifactMetadata_Bundle
        ON dbo.ArtifactBundleArtifactMetadata (BundleId);
END;

IF OBJECT_ID(N'dbo.ArtifactBundleArtifactDecisionLinks', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ArtifactBundleArtifactDecisionLinks
    (
        BundleId UNIQUEIDENTIFIER NOT NULL,
        ArtifactSortOrder INT NOT NULL,
        LinkSortOrder INT NOT NULL,
        DecisionId NVARCHAR(200) NOT NULL,
        CONSTRAINT PK_ArtifactBundleArtifactDecisionLinks PRIMARY KEY (BundleId, ArtifactSortOrder, LinkSortOrder),
        CONSTRAINT FK_ArtifactBundleArtifactDecisionLinks_Artifacts FOREIGN KEY (BundleId, ArtifactSortOrder)
            REFERENCES dbo.ArtifactBundleArtifacts (BundleId, SortOrder) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_ArtifactBundleArtifactDecisionLinks_Bundle
        ON dbo.ArtifactBundleArtifactDecisionLinks (BundleId);
END;

IF OBJECT_ID(N'dbo.ArtifactBundleTraceGenerators', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ArtifactBundleTraceGenerators
    (
        BundleId UNIQUEIDENTIFIER NOT NULL,
        SortOrder INT NOT NULL,
        GeneratorName NVARCHAR(500) NOT NULL,
        CONSTRAINT PK_ArtifactBundleTraceGenerators PRIMARY KEY (BundleId, SortOrder),
        CONSTRAINT FK_ArtifactBundleTraceGenerators_Bundles FOREIGN KEY (BundleId)
            REFERENCES dbo.ArtifactBundles (BundleId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_ArtifactBundleTraceGenerators_BundleId
        ON dbo.ArtifactBundleTraceGenerators (BundleId);
END;

IF OBJECT_ID(N'dbo.ArtifactBundleTraceDecisionLinks', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ArtifactBundleTraceDecisionLinks
    (
        BundleId UNIQUEIDENTIFIER NOT NULL,
        SortOrder INT NOT NULL,
        DecisionId NVARCHAR(200) NOT NULL,
        CONSTRAINT PK_ArtifactBundleTraceDecisionLinks PRIMARY KEY (BundleId, SortOrder),
        CONSTRAINT FK_ArtifactBundleTraceDecisionLinks_Bundles FOREIGN KEY (BundleId)
            REFERENCES dbo.ArtifactBundles (BundleId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_ArtifactBundleTraceDecisionLinks_BundleId
        ON dbo.ArtifactBundleTraceDecisionLinks (BundleId);
END;

IF OBJECT_ID(N'dbo.ArtifactBundleTraceNotes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ArtifactBundleTraceNotes
    (
        BundleId UNIQUEIDENTIFIER NOT NULL,
        SortOrder INT NOT NULL,
        NoteText NVARCHAR(MAX) NOT NULL,
        CONSTRAINT PK_ArtifactBundleTraceNotes PRIMARY KEY (BundleId, SortOrder),
        CONSTRAINT FK_ArtifactBundleTraceNotes_Bundles FOREIGN KEY (BundleId)
            REFERENCES dbo.ArtifactBundles (BundleId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_ArtifactBundleTraceNotes_BundleId
        ON dbo.ArtifactBundleTraceNotes (BundleId);
END;
