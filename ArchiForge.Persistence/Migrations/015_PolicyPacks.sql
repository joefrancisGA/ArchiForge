IF OBJECT_ID('dbo.PolicyPacks', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PolicyPacks
    (
        PolicyPackId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_PolicyPacks PRIMARY KEY,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        Name NVARCHAR(300) NOT NULL,
        Description NVARCHAR(MAX) NOT NULL,
        PackType NVARCHAR(50) NOT NULL,
        Status NVARCHAR(50) NOT NULL,
        CreatedUtc DATETIME2 NOT NULL,
        ActivatedUtc DATETIME2 NULL,
        CurrentVersion NVARCHAR(50) NOT NULL
    );

    CREATE NONCLUSTERED INDEX IX_PolicyPacks_Scope_Status
        ON dbo.PolicyPacks (TenantId, WorkspaceId, ProjectId, Status, CreatedUtc DESC);
END;
GO

IF OBJECT_ID('dbo.PolicyPackVersions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PolicyPackVersions
    (
        PolicyPackVersionId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_PolicyPackVersions PRIMARY KEY,
        PolicyPackId UNIQUEIDENTIFIER NOT NULL,
        [Version] NVARCHAR(50) NOT NULL,
        ContentJson NVARCHAR(MAX) NOT NULL,
        CreatedUtc DATETIME2 NOT NULL,
        IsPublished BIT NOT NULL
    );

    CREATE NONCLUSTERED INDEX IX_PolicyPackVersions_PolicyPackId_Version
        ON dbo.PolicyPackVersions (PolicyPackId, [Version]);
END;
GO

IF OBJECT_ID('dbo.PolicyPackAssignments', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PolicyPackAssignments
    (
        AssignmentId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_PolicyPackAssignments PRIMARY KEY,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        PolicyPackId UNIQUEIDENTIFIER NOT NULL,
        PolicyPackVersion NVARCHAR(50) NOT NULL,
        IsEnabled BIT NOT NULL,
        AssignedUtc DATETIME2 NOT NULL
    );

    CREATE NONCLUSTERED INDEX IX_PolicyPackAssignments_Scope_Enabled
        ON dbo.PolicyPackAssignments (TenantId, WorkspaceId, ProjectId, IsEnabled, AssignedUtc DESC);
END;
GO
