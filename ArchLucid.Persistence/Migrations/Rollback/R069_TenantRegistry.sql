/*
  Rollback 069: remove tenant registry tables.
*/
IF OBJECT_ID(N'dbo.TenantWorkspaces', N'U') IS NOT NULL
    DROP TABLE dbo.TenantWorkspaces;
GO

IF OBJECT_ID(N'dbo.Tenants', N'U') IS NOT NULL
    DROP TABLE dbo.Tenants;
GO
