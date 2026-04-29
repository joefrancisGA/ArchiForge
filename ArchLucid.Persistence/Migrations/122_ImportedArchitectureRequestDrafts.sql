/* 122 — Draft imported architecture requests (TOML/JSON file upload). */

IF OBJECT_ID(N'dbo.ImportedArchitectureRequests', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ImportedArchitectureRequests
    (
        ImportId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ImportedArchitectureRequests PRIMARY KEY,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkspaceId UNIQUEIDENTIFIER NOT NULL,
        ProjectId UNIQUEIDENTIFIER NOT NULL,
        CreatedUtc DATETIME2 NOT NULL,
        SourceFileName NVARCHAR(400) NOT NULL,
        Format NVARCHAR(16) NOT NULL,
        Status NVARCHAR(32) NOT NULL CONSTRAINT DF_ImportedArchitectureRequests_Status DEFAULT (N'Draft'),
        RequestJson NVARCHAR(MAX) NULL,
        CONSTRAINT CH_ImportedArchitectureRequests_Format CHECK (Format IN (N'toml', N'json'))
    );

    CREATE NONCLUSTERED INDEX IX_ImportedArchitectureRequests_Scope_Created
        ON dbo.ImportedArchitectureRequests (TenantId, WorkspaceId, ProjectId, CreatedUtc DESC);
END;
GO
