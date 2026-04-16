/*
  069: SaaS tenant registry (dbo.Tenants, dbo.TenantWorkspaces).

  RLS: not applied here — rows are global admin metadata; API must restrict to Admin policy.
*/
IF OBJECT_ID(N'dbo.Tenants', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Tenants
    (
        Id               UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Tenants PRIMARY KEY,
        Name             NVARCHAR(200)    NOT NULL,
        Slug             NVARCHAR(100)    NOT NULL,
        Tier             NVARCHAR(32)     NOT NULL CONSTRAINT DF_Tenants_Tier DEFAULT N'Standard',
        CreatedUtc       DATETIMEOFFSET   NOT NULL CONSTRAINT DF_Tenants_CreatedUtc DEFAULT SYSUTCDATETIME(),
        SuspendedUtc     DATETIMEOFFSET   NULL,
        CONSTRAINT UQ_Tenants_Slug UNIQUE (Slug)
    );
END;
GO

IF OBJECT_ID(N'dbo.TenantWorkspaces', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TenantWorkspaces
    (
        Id                UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_TenantWorkspaces PRIMARY KEY,
        TenantId          UNIQUEIDENTIFIER NOT NULL,
        Name              NVARCHAR(200)    NOT NULL,
        DefaultProjectId  UNIQUEIDENTIFIER NOT NULL,
        CreatedUtc        DATETIMEOFFSET   NOT NULL CONSTRAINT DF_TenantWorkspaces_CreatedUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_TenantWorkspaces_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (Id)
    );

    CREATE NONCLUSTERED INDEX IX_TenantWorkspaces_TenantId ON dbo.TenantWorkspaces (TenantId);
END;
GO
