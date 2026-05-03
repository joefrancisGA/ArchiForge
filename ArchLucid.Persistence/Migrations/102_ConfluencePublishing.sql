/*
  102: Confluence Cloud outbound publisher — targets + publish job queue (ADR 0023).
*/

SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.ConfluencePublishingTargets', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConfluencePublishingTargets
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ConfluencePublishingTargets PRIMARY KEY,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        BaseUrl NVARCHAR(512) NOT NULL,
        SpaceKey NVARCHAR(64) NOT NULL,
        ParentPageId NVARCHAR(64) NULL,
        AuthorEmail NVARCHAR(320) NOT NULL,
        SecretReference NVARCHAR(256) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_ConfluencePublishingTargets_IsActive DEFAULT (1),
        CreatedUtc DATETIMEOFFSET(7) NOT NULL,
        CreatedBy NVARCHAR(320) NOT NULL,
        UpdatedUtc DATETIMEOFFSET(7) NULL
    );

    CREATE UNIQUE INDEX UX_ConfluencePublishingTargets_TenantProject
        ON dbo.ConfluencePublishingTargets (TenantId, WorkspaceId, ProjectId)
        WHERE IsActive = 1;
END;
GO

IF OBJECT_ID(N'dbo.ConfluencePublishJobs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConfluencePublishJobs
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ConfluencePublishJobs PRIMARY KEY,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        TargetId UNIQUEIDENTIFIER NOT NULL,
        RunId UNIQUEIDENTIFIER NOT NULL,
        ManifestVersion NVARCHAR(64) NOT NULL,
        DiffBadgeState NVARCHAR(16) NOT NULL,
        PreviousBadgeState NVARCHAR(16) NULL,
        PayloadJson NVARCHAR(MAX) NOT NULL,
        IdempotencyKey VARBINARY(32) NOT NULL,
        Status NVARCHAR(16) NOT NULL,
        Attempts INT NOT NULL CONSTRAINT DF_ConfluencePublishJobs_Attempts DEFAULT (0),
        NextAttemptUtc DATETIMEOFFSET(7) NOT NULL,
        LastErrorReason NVARCHAR(64) NULL,
        LastErrorMessage NVARCHAR(2000) NULL,
        ConfluencePageId NVARCHAR(64) NULL,
        EnqueuedUtc DATETIMEOFFSET(7) NOT NULL,
        CompletedUtc DATETIMEOFFSET(7) NULL,
        CONSTRAINT FK_ConfluencePublishJobs_ConfluencePublishingTargets FOREIGN KEY (TargetId)
            REFERENCES dbo.ConfluencePublishingTargets (Id),
        CONSTRAINT UX_ConfluencePublishJobs_IdempotencyKey UNIQUE (TenantId, IdempotencyKey)
    );

    CREATE INDEX IX_ConfluencePublishJobs_NextAttempt
        ON dbo.ConfluencePublishJobs (Status, NextAttemptUtc)
        INCLUDE (TenantId, TargetId);
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchiforgeTenantScope')
   AND OBJECT_ID(N'dbo.ConfluencePublishingTargets', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.objects AS t ON t.object_id = p.target_object_id
        WHERE SCHEMA_NAME(t.schema_id) = N'dbo'
          AND t.name = N'ConfluencePublishingTargets')
BEGIN
    EXEC (N'
ALTER SECURITY POLICY rls.ArchiforgeTenantScope
        ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ConfluencePublishingTargets,
        ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ConfluencePublishingTargets AFTER INSERT,
        ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ConfluencePublishingTargets AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ConfluencePublishingTargets BEFORE DELETE;
');
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchiforgeTenantScope')
   AND OBJECT_ID(N'dbo.ConfluencePublishJobs', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.objects AS t ON t.object_id = p.target_object_id
        WHERE SCHEMA_NAME(t.schema_id) = N'dbo'
          AND t.name = N'ConfluencePublishJobs')
BEGIN
    EXEC (N'
ALTER SECURITY POLICY rls.ArchiforgeTenantScope
        ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ConfluencePublishJobs,
        ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ConfluencePublishJobs AFTER INSERT,
        ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ConfluencePublishJobs AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ConfluencePublishJobs BEFORE DELETE;
');
END;
GO
