-- §227: Support scoped list queries on authority Runs (ListByProjectAsync: TenantId, WorkspaceId, ScopeProjectId, ProjectId, ORDER BY CreatedUtc DESC).
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Runs_Scope_Project_CreatedUtc'
      AND object_id = OBJECT_ID(N'dbo.Runs'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Runs_Scope_Project_CreatedUtc
        ON dbo.Runs (TenantId, WorkspaceId, ScopeProjectId, ProjectId, CreatedUtc DESC);
END
GO
