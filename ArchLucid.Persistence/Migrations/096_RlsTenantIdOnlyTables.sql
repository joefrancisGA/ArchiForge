/*
  096: RLS expansion — tenant-keyed tables that carry TenantId only (no workspace/project on the row).

  Predicate: rls.archiforge_tenant_predicate(@TenantId) — session af_tenant_id match or af_rls_bypass = 1.

  Targets: dbo.SentEmails, dbo.TenantLifecycleTransitions, dbo.TenantTrialSeatOccupants.

  Ships with existing policy STATE unchanged (typically OFF until operators enable RLS globally).
*/

IF OBJECT_ID(N'rls.archiforge_tenant_predicate', N'IF') IS NOT NULL
    DROP FUNCTION rls.archiforge_tenant_predicate;
GO

CREATE FUNCTION rls.archiforge_tenant_predicate(@TenantId uniqueidentifier)
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN
(
    SELECT 1 AS access_granted
    WHERE ISNULL(TRY_CONVERT(int, SESSION_CONTEXT(N'af_rls_bypass')), 0) = 1
       OR @TenantId = TRY_CONVERT(uniqueidentifier, SESSION_CONTEXT(N'af_tenant_id'))
);
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchiforgeTenantScope')
   AND OBJECT_ID(N'dbo.SentEmails', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.objects AS t ON t.object_id = p.target_object_id
        WHERE SCHEMA_NAME(t.schema_id) = N'dbo'
          AND t.name = N'SentEmails')
BEGIN
    EXEC (N'
ALTER SECURITY POLICY rls.ArchiforgeTenantScope
        ADD FILTER PREDICATE rls.archiforge_tenant_predicate(TenantId) ON dbo.SentEmails,
        ADD BLOCK PREDICATE rls.archiforge_tenant_predicate(TenantId) ON dbo.SentEmails AFTER INSERT,
        ADD BLOCK PREDICATE rls.archiforge_tenant_predicate(TenantId) ON dbo.SentEmails AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archiforge_tenant_predicate(TenantId) ON dbo.SentEmails BEFORE DELETE;
');
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchiforgeTenantScope')
   AND OBJECT_ID(N'dbo.TenantLifecycleTransitions', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.objects AS t ON t.object_id = p.target_object_id
        WHERE SCHEMA_NAME(t.schema_id) = N'dbo'
          AND t.name = N'TenantLifecycleTransitions')
BEGIN
    EXEC (N'
ALTER SECURITY POLICY rls.ArchiforgeTenantScope
        ADD FILTER PREDICATE rls.archiforge_tenant_predicate(TenantId) ON dbo.TenantLifecycleTransitions,
        ADD BLOCK PREDICATE rls.archiforge_tenant_predicate(TenantId) ON dbo.TenantLifecycleTransitions AFTER INSERT,
        ADD BLOCK PREDICATE rls.archiforge_tenant_predicate(TenantId) ON dbo.TenantLifecycleTransitions AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archiforge_tenant_predicate(TenantId) ON dbo.TenantLifecycleTransitions BEFORE DELETE;
');
END;
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'ArchiforgeTenantScope')
   AND OBJECT_ID(N'dbo.TenantTrialSeatOccupants', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.security_predicates AS p
        INNER JOIN sys.objects AS t ON t.object_id = p.target_object_id
        WHERE SCHEMA_NAME(t.schema_id) = N'dbo'
          AND t.name = N'TenantTrialSeatOccupants')
BEGIN
    EXEC (N'
ALTER SECURITY POLICY rls.ArchiforgeTenantScope
        ADD FILTER PREDICATE rls.archiforge_tenant_predicate(TenantId) ON dbo.TenantTrialSeatOccupants,
        ADD BLOCK PREDICATE rls.archiforge_tenant_predicate(TenantId) ON dbo.TenantTrialSeatOccupants AFTER INSERT,
        ADD BLOCK PREDICATE rls.archiforge_tenant_predicate(TenantId) ON dbo.TenantTrialSeatOccupants AFTER UPDATE,
        ADD BLOCK PREDICATE rls.archiforge_tenant_predicate(TenantId) ON dbo.TenantTrialSeatOccupants BEFORE DELETE;
');
END;
GO
