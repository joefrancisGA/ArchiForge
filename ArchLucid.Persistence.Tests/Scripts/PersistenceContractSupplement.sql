/* Tables required by Dapper contract tests that are not created by DbUp migrations.
   Intentionally excludes FK hardening from ArchiForge.sql (e.g. ContextSnapshots -> Runs,
   GraphSnapshots -> ContextSnapshots, ComparisonRecords -> ArchitectureRuns) so integration
   tests can seed rows without full authority-chain parents. Production deployments apply
   the full ArchiForge.sql via API schema bootstrap. */

/* Append-only audit stream (no UPDATE/DELETE from application code). */
IF OBJECT_ID('dbo.AuditEvents', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditEvents
    (
        EventId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        OccurredUtc DATETIME2 NOT NULL,
        EventType NVARCHAR(100) NOT NULL,
        ActorUserId NVARCHAR(200) NOT NULL,
        ActorUserName NVARCHAR(200) NOT NULL,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        RunId UNIQUEIDENTIFIER NULL,
        ManifestId UNIQUEIDENTIFIER NULL,
        ArtifactId UNIQUEIDENTIFIER NULL,
        DataJson NVARCHAR(MAX) NOT NULL,
        CorrelationId NVARCHAR(200) NULL,
        INDEX IX_AuditEvents_Scope_OccurredUtc NONCLUSTERED (TenantId, WorkspaceId, ProjectId, OccurredUtc DESC)
    );
END;
GO

IF OBJECT_ID('dbo.ProvenanceSnapshots', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProvenanceSnapshots
    (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        RunId UNIQUEIDENTIFIER NOT NULL,
        GraphJson NVARCHAR(MAX) NOT NULL,
        CreatedUtc DATETIME2 NOT NULL,
        INDEX IX_ProvenanceSnapshots_Scope_Run NONCLUSTERED (TenantId, WorkspaceId, ProjectId, RunId, CreatedUtc DESC)
    );
END;
GO

IF OBJECT_ID('dbo.ConversationThreads', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConversationThreads
    (
        ThreadId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        RunId UNIQUEIDENTIFIER NULL,
        BaseRunId UNIQUEIDENTIFIER NULL,
        TargetRunId UNIQUEIDENTIFIER NULL,
        Title NVARCHAR(300) NOT NULL,
        CreatedUtc DATETIME2 NOT NULL,
        LastUpdatedUtc DATETIME2 NOT NULL,
        ArchivedUtc DATETIME2 NULL,
        INDEX IX_ConversationThreads_Scope NONCLUSTERED (TenantId, WorkspaceId, ProjectId, LastUpdatedUtc DESC)
    );
END;
GO

IF OBJECT_ID('dbo.ConversationMessages', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConversationMessages
    (
        MessageId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        ThreadId UNIQUEIDENTIFIER NOT NULL,
        Role NVARCHAR(50) NOT NULL,
        Content NVARCHAR(MAX) NOT NULL,
        CreatedUtc DATETIME2 NOT NULL,
        MetadataJson NVARCHAR(MAX) NOT NULL,
        INDEX IX_ConversationMessages_ThreadId_CreatedUtc NONCLUSTERED (ThreadId, CreatedUtc ASC)
    );
END;
GO

/* Integration tests only: allow NULL legacy JSON columns to exercise repository IsNullOrWhiteSpace guards.
   Production ArchiForge.sql keeps NOT NULL for brownfield inserts; test catalog applies this supplement after DbUp. */
IF OBJECT_ID(N'dbo.FindingsSnapshots', N'U') IS NOT NULL
BEGIN
    ALTER TABLE dbo.FindingsSnapshots ALTER COLUMN FindingsJson NVARCHAR(MAX) NULL;
END;
GO

IF OBJECT_ID(N'dbo.ContextSnapshots', N'U') IS NOT NULL
BEGIN
    ALTER TABLE dbo.ContextSnapshots ALTER COLUMN CanonicalObjectsJson NVARCHAR(MAX) NULL;
    ALTER TABLE dbo.ContextSnapshots ALTER COLUMN WarningsJson NVARCHAR(MAX) NULL;
    ALTER TABLE dbo.ContextSnapshots ALTER COLUMN ErrorsJson NVARCHAR(MAX) NULL;
    ALTER TABLE dbo.ContextSnapshots ALTER COLUMN SourceHashesJson NVARCHAR(MAX) NULL;
END;
GO

IF OBJECT_ID(N'dbo.ArtifactBundles', N'U') IS NOT NULL
BEGIN
    ALTER TABLE dbo.ArtifactBundles ALTER COLUMN ArtifactsJson NVARCHAR(MAX) NULL;
    ALTER TABLE dbo.ArtifactBundles ALTER COLUMN TraceJson NVARCHAR(MAX) NULL;
END;
GO

/* dbo.Tenants parent row for FK from governance tables (118) used by GovernanceRepositoryContractScope in Dapper contracts. */
IF OBJECT_ID(N'dbo.Tenants', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.Tenants WHERE Id = 'AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE')
BEGIN
    INSERT INTO dbo.Tenants (Id, Name, Slug, Tier, EntraTenantId)
    VALUES (
        N'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee',
        N'ArchLucid persistence contract governance',
        N'archlucid-persistence-contract-governance',
        N'Standard',
        NULL);
END;
GO
