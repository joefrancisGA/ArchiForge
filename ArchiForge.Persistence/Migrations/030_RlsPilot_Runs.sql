-- Pilot row-level security on dbo.Runs (defense-in-depth). Policy ships with STATE = OFF; enable after app sets SESSION_CONTEXT (SqlServer:RowLevelSecurity:ApplySessionContext).
-- Trusted jobs (archival, schema bootstrap) set SESSION_CONTEXT key af_rls_bypass = 1 via the API layer.

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'rls')
    EXEC(N'CREATE SCHEMA rls');
GO

IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'RunsScopeFilter')
    DROP SECURITY POLICY rls.RunsScopeFilter;
GO

IF OBJECT_ID(N'rls.runs_scope_predicate', N'IF') IS NOT NULL
    DROP FUNCTION rls.runs_scope_predicate;
GO

CREATE FUNCTION rls.runs_scope_predicate(
    @TenantId uniqueidentifier,
    @WorkspaceId uniqueidentifier,
    @ScopeProjectId uniqueidentifier)
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN
(
    SELECT 1 AS access_granted
    WHERE ISNULL(TRY_CONVERT(int, SESSION_CONTEXT(N'af_rls_bypass')), 0) = 1
       OR (
            @TenantId = TRY_CONVERT(uniqueidentifier, SESSION_CONTEXT(N'af_tenant_id'))
        AND @WorkspaceId = TRY_CONVERT(uniqueidentifier, SESSION_CONTEXT(N'af_workspace_id'))
        AND @ScopeProjectId = TRY_CONVERT(uniqueidentifier, SESSION_CONTEXT(N'af_project_id'))
       )
);
GO

CREATE SECURITY POLICY rls.RunsScopeFilter
    ADD FILTER PREDICATE rls.runs_scope_predicate(TenantId, WorkspaceId, ScopeProjectId) ON dbo.Runs
    WITH (STATE = OFF);
GO
