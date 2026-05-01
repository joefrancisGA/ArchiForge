/* R133: Roll back 133 — drop dbo.AdminNotifications; remove ScimUsers.ResolvedRoleOrigin (+ default/check). */

IF OBJECT_ID(N'dbo.AdminNotifications', N'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.AdminNotifications;
END;
GO

IF OBJECT_ID(N'dbo.ScimUsers', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.ScimUsers', N'ResolvedRoleOrigin') IS NOT NULL
BEGIN
    ALTER TABLE dbo.ScimUsers DROP CONSTRAINT IF EXISTS CK_ScimUsers_ResolvedRoleOrigin_Valid;
    ALTER TABLE dbo.ScimUsers DROP CONSTRAINT IF EXISTS DF_ScimUsers_ResolvedRoleOrigin;
    ALTER TABLE dbo.ScimUsers DROP COLUMN ResolvedRoleOrigin;
END;
GO
