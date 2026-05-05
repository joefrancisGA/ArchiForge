/*
  083: Tenant health score aggregates (scheduled worker) + in-product product feedback (PMF).

  RLS: triple scope (TenantId, WorkspaceId, ProjectId) on both tables.
  ArchLucidApp: DENY INSERT/UPDATE/DELETE on dbo.TenantHealthScores; writes via dbo.sp_TenantHealthScores_Upsert (EXECUTE AS OWNER).
  ProductFeedback: application INSERT under session context (RLS BLOCK/FILTER only).
*/

IF OBJECT_ID(N'dbo.TenantHealthScores', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TenantHealthScores
    (
        TenantId          UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_TenantHealthScores PRIMARY KEY,
        WorkspaceId       UNIQUEIDENTIFIER NOT NULL,
        ProjectId         UNIQUEIDENTIFIER NOT NULL,
        EngagementScore   DECIMAL(5, 2)    NOT NULL,
        BreadthScore      DECIMAL(5, 2)    NOT NULL,
        QualityScore      DECIMAL(5, 2)    NOT NULL,
        GovernanceScore   DECIMAL(5, 2)    NOT NULL,
        SupportScore      DECIMAL(5, 2)    NOT NULL,
        CompositeScore  DECIMAL(5, 2)    NOT NULL,
        UpdatedUtc        DATETIME2(7)     NOT NULL CONSTRAINT DF_TenantHealthScores_UpdatedUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_TenantHealthScores_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (Id)
    );
END;
GO

IF OBJECT_ID(N'dbo.TenantHealthScores', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.TenantHealthScores', N'WorkspaceId') IS NULL
    ALTER TABLE dbo.TenantHealthScores ADD WorkspaceId UNIQUEIDENTIFIER NULL;
GO

IF OBJECT_ID(N'dbo.TenantHealthScores', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.TenantHealthScores', N'ProjectId') IS NULL
    ALTER TABLE dbo.TenantHealthScores ADD ProjectId UNIQUEIDENTIFIER NULL;
GO

IF OBJECT_ID(N'dbo.ProductFeedback', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductFeedback
    (
        FeedbackId   UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ProductFeedback PRIMARY KEY,
        TenantId     UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId  UNIQUEIDENTIFIER NOT NULL,
        ProjectId    UNIQUEIDENTIFIER NOT NULL,
        FindingRef   NVARCHAR(512)    NULL,
        RunId        UNIQUEIDENTIFIER NULL,
        Score        SMALLINT         NOT NULL,
        CommentText  NVARCHAR(2000)   NULL,
        CreatedUtc   DATETIME2(7)     NOT NULL CONSTRAINT DF_ProductFeedback_CreatedUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT CK_ProductFeedback_Score CHECK (Score BETWEEN (-1) AND 1),
        CONSTRAINT FK_ProductFeedback_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (Id)
    );

    CREATE NONCLUSTERED INDEX IX_ProductFeedback_Tenant_CreatedUtc
        ON dbo.ProductFeedback (TenantId, CreatedUtc DESC);
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchiforgeTenantScope')
   AND OBJECT_ID(N'dbo.TenantHealthScores', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.objects AS t ON t.object_id = p.target_object_id
        WHERE SCHEMA_NAME(t.schema_id) = N'dbo'
          AND t.name = N'TenantHealthScores')
BEGIN
    EXEC (N'
ALTER SECURITY POLICY rls.ArchiforgeTenantScope
        ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.TenantHealthScores,
        ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.TenantHealthScores AFTER INSERT,
        ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.TenantHealthScores AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.TenantHealthScores BEFORE DELETE;
');
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchiforgeTenantScope')
   AND OBJECT_ID(N'dbo.ProductFeedback', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.objects AS t ON t.object_id = p.target_object_id
        WHERE SCHEMA_NAME(t.schema_id) = N'dbo'
          AND t.name = N'ProductFeedback')
BEGIN
    EXEC (N'
ALTER SECURITY POLICY rls.ArchiforgeTenantScope
        ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProductFeedback,
        ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProductFeedback AFTER INSERT,
        ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProductFeedback AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.ProductFeedback BEFORE DELETE;
');
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_TenantHealthScores_Upsert
    @TenantId uniqueidentifier,
    @WorkspaceId uniqueidentifier,
    @ProjectId uniqueidentifier,
    @EngagementScore decimal(5, 2),
    @BreadthScore decimal(5, 2),
    @QualityScore decimal(5, 2),
    @GovernanceScore decimal(5, 2),
    @SupportScore decimal(5, 2),
    @CompositeScore decimal(5, 2)
WITH EXECUTE AS OWNER
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dbo.TenantHealthScores AS t
    USING (SELECT @TenantId AS TenantId) AS s ON t.TenantId = s.TenantId
    WHEN MATCHED THEN
        UPDATE SET
            WorkspaceId = @WorkspaceId,
            ProjectId = @ProjectId,
            EngagementScore = @EngagementScore,
            BreadthScore = @BreadthScore,
            QualityScore = @QualityScore,
            GovernanceScore = @GovernanceScore,
            SupportScore = @SupportScore,
            CompositeScore = @CompositeScore,
            UpdatedUtc = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN
        INSERT (
            TenantId, WorkspaceId, ProjectId,
            EngagementScore, BreadthScore, QualityScore, GovernanceScore, SupportScore,
            CompositeScore, UpdatedUtc)
        VALUES (
            @TenantId, @WorkspaceId, @ProjectId,
            @EngagementScore, @BreadthScore, @QualityScore, @GovernanceScore, @SupportScore,
            @CompositeScore, SYSUTCDATETIME());
END;
GO

IF DATABASE_PRINCIPAL_ID(N'ArchLucidApp') IS NOT NULL
   AND OBJECT_ID(N'dbo.TenantHealthScores', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM sys.database_permissions AS dp
        INNER JOIN sys.database_principals AS gp ON dp.grantee_principal_id = gp.principal_id
        WHERE dp.class_desc = N'OBJECT_OR_COLUMN'
          AND dp.major_id = OBJECT_ID(N'dbo.TenantHealthScores')
          AND dp.permission_name = N'INSERT'
          AND dp.state_desc = N'DENY'
          AND gp.name = N'ArchLucidApp')
    BEGIN
        DENY INSERT ON dbo.TenantHealthScores TO [ArchLucidApp];
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.database_permissions AS dp
        INNER JOIN sys.database_principals AS gp ON dp.grantee_principal_id = gp.principal_id
        WHERE dp.class_desc = N'OBJECT_OR_COLUMN'
          AND dp.major_id = OBJECT_ID(N'dbo.TenantHealthScores')
          AND dp.permission_name = N'UPDATE'
          AND dp.state_desc = N'DENY'
          AND gp.name = N'ArchLucidApp')
    BEGIN
        DENY UPDATE ON dbo.TenantHealthScores TO [ArchLucidApp];
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.database_permissions AS dp
        INNER JOIN sys.database_principals AS gp ON dp.grantee_principal_id = gp.principal_id
        WHERE dp.class_desc = N'OBJECT_OR_COLUMN'
          AND dp.major_id = OBJECT_ID(N'dbo.TenantHealthScores')
          AND dp.permission_name = N'DELETE'
          AND dp.state_desc = N'DENY'
          AND gp.name = N'ArchLucidApp')
    BEGIN
        DENY DELETE ON dbo.TenantHealthScores TO [ArchLucidApp];
    END;

    GRANT EXECUTE ON OBJECT::dbo.sp_TenantHealthScores_Upsert TO [ArchLucidApp];
END;
GO
