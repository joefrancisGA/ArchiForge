-- Parent tables for dbo.GraphSnapshotEdges (FK in 018). Authority/decisioning persistence; aligns with SQL/ArchiForge.sql.
IF OBJECT_ID(N'dbo.Runs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Runs
    (
        RunId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        ProjectId NVARCHAR(200) NOT NULL,
        Description NVARCHAR(4000) NULL,
        CreatedUtc DATETIME2 NOT NULL,
        ContextSnapshotId UNIQUEIDENTIFIER NULL,
        GraphSnapshotId UNIQUEIDENTIFIER NULL,
        FindingsSnapshotId UNIQUEIDENTIFIER NULL,
        GoldenManifestId UNIQUEIDENTIFIER NULL,
        DecisionTraceId UNIQUEIDENTIFIER NULL,
        ArtifactBundleId UNIQUEIDENTIFIER NULL,
        TenantId UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT DF_Runs_TenantId DEFAULT ('11111111-1111-1111-1111-111111111111'),
        WorkspaceId UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT DF_Runs_WorkspaceId DEFAULT ('22222222-2222-2222-2222-222222222222'),
        ScopeProjectId UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT DF_Runs_ScopeProjectId DEFAULT ('33333333-3333-3333-3333-333333333333')
    );
    CREATE NONCLUSTERED INDEX IX_Runs_ProjectId_CreatedUtc ON dbo.Runs (ProjectId, CreatedUtc DESC);
END;

IF OBJECT_ID(N'dbo.ContextSnapshots', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ContextSnapshots
    (
        SnapshotId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        RunId UNIQUEIDENTIFIER NOT NULL,
        ProjectId NVARCHAR(200) NOT NULL,
        CreatedUtc DATETIME2 NOT NULL,
        CanonicalObjectsJson NVARCHAR(MAX) NOT NULL,
        DeltaSummary NVARCHAR(MAX) NULL,
        WarningsJson NVARCHAR(MAX) NOT NULL,
        ErrorsJson NVARCHAR(MAX) NOT NULL,
        SourceHashesJson NVARCHAR(MAX) NOT NULL
    );
    CREATE NONCLUSTERED INDEX IX_ContextSnapshots_ProjectId_CreatedUtc ON dbo.ContextSnapshots (ProjectId, CreatedUtc DESC);
    CREATE NONCLUSTERED INDEX IX_ContextSnapshots_RunId ON dbo.ContextSnapshots (RunId);
END;

IF OBJECT_ID(N'dbo.GraphSnapshots', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GraphSnapshots
    (
        GraphSnapshotId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        ContextSnapshotId UNIQUEIDENTIFIER NOT NULL,
        RunId UNIQUEIDENTIFIER NOT NULL,
        CreatedUtc DATETIME2 NOT NULL,
        NodesJson NVARCHAR(MAX) NOT NULL,
        EdgesJson NVARCHAR(MAX) NOT NULL,
        WarningsJson NVARCHAR(MAX) NOT NULL
    );
    CREATE NONCLUSTERED INDEX IX_GraphSnapshots_RunId ON dbo.GraphSnapshots (RunId);
    CREATE NONCLUSTERED INDEX IX_GraphSnapshots_ContextSnapshotId ON dbo.GraphSnapshots (ContextSnapshotId);
END;
