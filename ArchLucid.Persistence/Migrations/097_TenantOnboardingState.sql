/*
  097: Per-tenant first-session completion marker (Core Pilot wizard funnel metric).

  One row per tenant; FirstSessionCompletedUtc set on first successful golden-manifest commit.
  RLS: tenant-only predicate (rls.archiforge_tenant_predicate from 096).
*/

IF OBJECT_ID(N'dbo.TenantOnboardingState', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TenantOnboardingState
    (
        TenantId                 UNIQUEIDENTIFIER NOT NULL,
        FirstSessionCompletedUtc DATETIME2(7)     NULL,
        CONSTRAINT PK_TenantOnboardingState PRIMARY KEY CLUSTERED (TenantId),
        CONSTRAINT FK_TenantOnboardingState_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (Id)
    );
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchiforgeTenantScope')
   AND OBJECT_ID(N'dbo.TenantOnboardingState', N'U') IS NOT NULL
   AND OBJECT_ID(N'rls.archiforge_tenant_predicate', N'IF') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.objects AS t ON t.object_id = p.target_object_id
        WHERE SCHEMA_NAME(t.schema_id) = N'dbo'
          AND t.name = N'TenantOnboardingState')
BEGIN
    EXEC (N'
ALTER SECURITY POLICY rls.ArchiforgeTenantScope
        ADD FILTER PREDICATE rls.archiforge_tenant_predicate(TenantId) ON dbo.TenantOnboardingState,
        ADD BLOCK PREDICATE rls.archiforge_tenant_predicate(TenantId) ON dbo.TenantOnboardingState AFTER INSERT,
        ADD BLOCK PREDICATE rls.archiforge_tenant_predicate(TenantId) ON dbo.TenantOnboardingState AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archiforge_tenant_predicate(TenantId) ON dbo.TenantOnboardingState BEFORE DELETE;
');
END;
GO
