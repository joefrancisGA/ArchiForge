/* Align DbUp-only databases with Dapper: ScopeLevel + IsPinned on PolicyPackAssignments. */

IF OBJECT_ID('dbo.PolicyPackAssignments', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.PolicyPackAssignments', 'ScopeLevel') IS NULL
    BEGIN
        ALTER TABLE dbo.PolicyPackAssignments ADD ScopeLevel NVARCHAR(50) NOT NULL
            CONSTRAINT DF_PolicyPackAssignments_ScopeLevel DEFAULT (N'Project');
    END;

    IF COL_LENGTH('dbo.PolicyPackAssignments', 'IsPinned') IS NULL
    BEGIN
        ALTER TABLE dbo.PolicyPackAssignments ADD IsPinned BIT NOT NULL
            CONSTRAINT DF_PolicyPackAssignments_IsPinned DEFAULT (0);
    END;
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_PolicyPackAssignments_ScopeLevel_AssignedUtc'
      AND object_id = OBJECT_ID(N'dbo.PolicyPackAssignments'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_PolicyPackAssignments_ScopeLevel_AssignedUtc
        ON dbo.PolicyPackAssignments (TenantId, WorkspaceId, ProjectId, ScopeLevel, AssignedUtc DESC);
END;
GO
