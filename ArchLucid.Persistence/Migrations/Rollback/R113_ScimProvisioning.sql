IF OBJECT_ID(N'dbo.ScimGroupMembers', N'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.ScimGroupMembers;
END;
GO

IF OBJECT_ID(N'dbo.ScimGroups', N'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.ScimGroups;
END;
GO

IF OBJECT_ID(N'dbo.ScimUsers', N'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.ScimUsers;
END;
GO

IF OBJECT_ID(N'dbo.ScimTenantTokens', N'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.ScimTenantTokens;
END;
GO

IF COL_LENGTH(N'dbo.Tenants', N'EnterpriseSeatsLimit') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Tenants DROP CONSTRAINT DF_Tenants_EnterpriseSeatsUsed113;
    ALTER TABLE dbo.Tenants DROP COLUMN EnterpriseSeatsUsed;
    ALTER TABLE dbo.Tenants DROP COLUMN EnterpriseSeatsLimit;
END;
GO
