/*
  104: Per-finding thumbs feedback (operator instrumentation).

  RLS: triple scope (TenantId, WorkspaceId, ProjectId), same pattern as ProductFeedback.
*/

IF OBJECT_ID(N'dbo.FindingFeedback', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FindingFeedback
    (
        FeedbackId   UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_FindingFeedback PRIMARY KEY,
        TenantId     UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId  UNIQUEIDENTIFIER NOT NULL,
        ProjectId    UNIQUEIDENTIFIER NOT NULL,
        RunId        UNIQUEIDENTIFIER NOT NULL,
        FindingId    NVARCHAR(32)     NOT NULL,
        Score        SMALLINT         NOT NULL,
        CreatedUtc   DATETIME2(7)     NOT NULL CONSTRAINT DF_FindingFeedback_CreatedUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT CK_FindingFeedback_Score CHECK (Score IN (-1, 1)),
        CONSTRAINT FK_FindingFeedback_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (Id)
    );

    CREATE NONCLUSTERED INDEX IX_FindingFeedback_Tenant_CreatedUtc
        ON dbo.FindingFeedback (TenantId, CreatedUtc DESC);

    CREATE NONCLUSTERED INDEX IX_FindingFeedback_Tenant_Run_Finding
        ON dbo.FindingFeedback (TenantId, RunId, FindingId);
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchiforgeTenantScope')
   AND OBJECT_ID(N'dbo.FindingFeedback', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.objects AS t ON t.object_id = p.target_object_id
        WHERE SCHEMA_NAME(t.schema_id) = N'dbo'
          AND t.name = N'FindingFeedback')
BEGIN
    EXEC (N'
ALTER SECURITY POLICY rls.ArchiforgeTenantScope
        ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingFeedback,
        ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingFeedback AFTER INSERT,
        ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingFeedback AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.FindingFeedback BEFORE DELETE;
');
END;
GO
