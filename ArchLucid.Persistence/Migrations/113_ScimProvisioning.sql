/*
  113: SCIM 2.0 inbound service provider (Enterprise IdP provisioning).

  Tables: per-tenant SCIM bearer tokens (Argon2id-hashed secret material), SCIM Users / Groups / GroupMembers,
  plus enterprise seat counters on dbo.Tenants (Active=true SCIM users consume seats).
*/
IF COL_LENGTH(N'dbo.Tenants', N'EnterpriseSeatsLimit') IS NULL
BEGIN
    ALTER TABLE dbo.Tenants ADD
        EnterpriseSeatsLimit INT NULL,
        EnterpriseSeatsUsed INT NOT NULL CONSTRAINT DF_Tenants_EnterpriseSeatsUsed113 DEFAULT (0);
END;
GO

IF OBJECT_ID(N'dbo.ScimTenantTokens', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ScimTenantTokens
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ScimTenantTokens PRIMARY KEY
            CONSTRAINT DF_ScimTenantTokens_Id DEFAULT NEWSEQUENTIALID(),
        TenantId UNIQUEIDENTIFIER NOT NULL,
        PublicLookupKey NVARCHAR(128) NOT NULL,
        SecretHash VARBINARY(128) NOT NULL,
        CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_ScimTenantTokens_CreatedUtc DEFAULT SYSUTCDATETIME(),
        RevokedUtc DATETIME2(7) NULL,
        CONSTRAINT FK_ScimTenantTokens_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (Id),
        CONSTRAINT UQ_ScimTenantTokens_PublicLookupKey UNIQUE (PublicLookupKey)
    );

    CREATE NONCLUSTERED INDEX IX_ScimTenantTokens_TenantId_Active
        ON dbo.ScimTenantTokens (TenantId)
        INCLUDE (SecretHash, CreatedUtc, Id)
        WHERE RevokedUtc IS NULL;
END;
GO

IF OBJECT_ID(N'dbo.ScimUsers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ScimUsers
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ScimUsers PRIMARY KEY
            CONSTRAINT DF_ScimUsers_Id DEFAULT NEWSEQUENTIALID(),
        TenantId UNIQUEIDENTIFIER NOT NULL,
        ExternalId NVARCHAR(256) NOT NULL,
        UserName NVARCHAR(256) NOT NULL,
        DisplayName NVARCHAR(256) NULL,
        Active BIT NOT NULL CONSTRAINT DF_ScimUsers_Active DEFAULT (1),
        ResolvedRole NVARCHAR(64) NULL,
        CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_ScimUsers_CreatedUtc DEFAULT SYSUTCDATETIME(),
        UpdatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_ScimUsers_UpdatedUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_ScimUsers_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (Id),
        CONSTRAINT UQ_ScimUsers_TenantId_ExternalId UNIQUE (TenantId, ExternalId)
    );

    CREATE NONCLUSTERED INDEX IX_ScimUsers_TenantId_UserName ON dbo.ScimUsers (TenantId, UserName);
END;
GO

IF OBJECT_ID(N'dbo.ScimGroups', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ScimGroups
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ScimGroups PRIMARY KEY
            CONSTRAINT DF_ScimGroups_Id DEFAULT NEWSEQUENTIALID(),
        TenantId UNIQUEIDENTIFIER NOT NULL,
        ExternalId NVARCHAR(256) NOT NULL,
        DisplayName NVARCHAR(256) NOT NULL,
        CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_ScimGroups_CreatedUtc DEFAULT SYSUTCDATETIME(),
        UpdatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_ScimGroups_UpdatedUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_ScimGroups_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (Id),
        CONSTRAINT UQ_ScimGroups_TenantId_ExternalId UNIQUE (TenantId, ExternalId)
    );
END;
GO

IF OBJECT_ID(N'dbo.ScimGroupMembers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ScimGroupMembers
    (
        TenantId UNIQUEIDENTIFIER NOT NULL,
        GroupId UNIQUEIDENTIFIER NOT NULL,
        UserId UNIQUEIDENTIFIER NOT NULL,
        CreatedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_ScimGroupMembers_CreatedUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_ScimGroupMembers PRIMARY KEY (GroupId, UserId),
        CONSTRAINT FK_ScimGroupMembers_Groups FOREIGN KEY (GroupId) REFERENCES dbo.ScimGroups (Id),
        CONSTRAINT FK_ScimGroupMembers_Users FOREIGN KEY (UserId) REFERENCES dbo.ScimUsers (Id),
        CONSTRAINT FK_ScimGroupMembers_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (Id)
    );

    CREATE NONCLUSTERED INDEX IX_ScimGroupMembers_UserId ON dbo.ScimGroupMembers (UserId, TenantId);
END;
GO
