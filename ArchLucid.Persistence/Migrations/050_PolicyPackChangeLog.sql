/*
  Migration 050: Append-only policy pack change log.

  Records every mutation to policy packs, versions, and assignments.
  The application identity should have INSERT-only permissions on this
  table; UPDATE and DELETE are prohibited by design.

  Idempotent: skips creation when the table exists.
*/

IF OBJECT_ID(N'dbo.PolicyPackChangeLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PolicyPackChangeLog
    (
        ChangeLogId UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT DF_PolicyPackChangeLog_ChangeLogId DEFAULT NEWSEQUENTIALID(),
        PolicyPackId UNIQUEIDENTIFIER NOT NULL,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        ChangeType NVARCHAR(64) NOT NULL,
        ChangedBy NVARCHAR(256) NOT NULL,
        ChangedUtc DATETIME2(7) NOT NULL
            CONSTRAINT DF_PolicyPackChangeLog_ChangedUtc DEFAULT SYSUTCDATETIME(),
        PreviousValue NVARCHAR(MAX) NULL,
        NewValue NVARCHAR(MAX) NULL,
        SummaryText NVARCHAR(512) NULL,
        CONSTRAINT PK_PolicyPackChangeLog
            PRIMARY KEY CLUSTERED (ChangeLogId)
    );

    CREATE NONCLUSTERED INDEX IX_PolicyPackChangeLog_PackId_ChangedUtc
        ON dbo.PolicyPackChangeLog (PolicyPackId, ChangedUtc DESC);

    CREATE NONCLUSTERED INDEX IX_PolicyPackChangeLog_TenantId_ChangedUtc
        ON dbo.PolicyPackChangeLog (TenantId, ChangedUtc DESC);
END;
GO

-- RLS: add predicate when tenant scope policy exists (idempotent for re-runs).
IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchiforgeTenantScope')
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.objects AS t ON t.object_id = p.target_object_id
        WHERE SCHEMA_NAME(t.schema_id) = N'dbo'
          AND t.name = N'PolicyPackChangeLog')
BEGIN
    EXEC (N'
ALTER SECURITY POLICY rls.ArchiforgeTenantScope
        ADD FILTER PREDICATE rls.archiforge_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.PolicyPackChangeLog;
');
END;
GO
