SET NOCOUNT ON;
GO

/*
  142: Optional pilot closeout rows (proof-of-ROI questionnaire) at tenant/workspace/project scope.
  RLS: triple scope; application INSERT under session context (Core Pilot checklist pattern).
*/

SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.PilotCloseouts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PilotCloseouts
    (
        CloseoutId            UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_PilotCloseouts PRIMARY KEY,
        TenantId              UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId           UNIQUEIDENTIFIER NOT NULL,
        ProjectId             UNIQUEIDENTIFIER NOT NULL,
        RunId                 UNIQUEIDENTIFIER     NULL,
        BaselineHours         DECIMAL(12, 2)       NULL,
        SpeedScore            TINYINT          NOT NULL,
        ManifestPackageScore  TINYINT          NOT NULL,
        TraceabilityScore     TINYINT          NOT NULL,
        Notes                 NVARCHAR(2000)       NULL,
        CreatedUtc            DATETIME2(7)     NOT NULL CONSTRAINT DF_PilotCloseouts_CreatedUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT CK_PilotCloseouts_Scores CHECK (
            SpeedScore BETWEEN 1 AND 5
            AND ManifestPackageScore BETWEEN 1 AND 5
            AND TraceabilityScore BETWEEN 1 AND 5),
        CONSTRAINT FK_PilotCloseouts_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (Id)
    );

    CREATE NONCLUSTERED INDEX IX_PilotCloseouts_Scope_CreatedUtc
        ON dbo.PilotCloseouts (TenantId, WorkspaceId, ProjectId, CreatedUtc DESC);
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchLucidTenantScope')
   AND OBJECT_ID(N'dbo.PilotCloseouts', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.objects AS t ON t.object_id = p.target_object_id
        WHERE SCHEMA_NAME(t.schema_id) = N'dbo'
          AND t.name = N'PilotCloseouts')
BEGIN
    ALTER SECURITY POLICY rls.ArchLucidTenantScope
        ADD FILTER PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.PilotCloseouts,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.PilotCloseouts AFTER INSERT,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.PilotCloseouts AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archlucid_scope_predicate(TenantId, WorkspaceId, ProjectId) ON dbo.PilotCloseouts BEFORE DELETE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'ArchLucidApp')
   AND OBJECT_ID(N'dbo.PilotCloseouts', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.database_permissions AS dp
        WHERE dp.major_id = OBJECT_ID(N'dbo.PilotCloseouts')
          AND dp.grantee_principal_id = DATABASE_PRINCIPAL_ID(N'ArchLucidApp')
          AND dp.permission_name = N'SELECT')
BEGIN
    GRANT SELECT, INSERT ON dbo.PilotCloseouts TO [ArchLucidApp];
END;
GO
