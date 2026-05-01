/*
 133: SCIM resolved-role provenance (manual vs IdP groups) + operator dbo.AdminNotifications (SCIM token rotation reminders).

 See docs/PENDING_QUESTIONS Resolved 2026-04-24 (SCIM Improvement 1b).
*/
IF COL_LENGTH(N'dbo.ScimUsers', N'ResolvedRoleOrigin') IS NULL
BEGIN
    ALTER TABLE dbo.ScimUsers ADD
        ResolvedRoleOrigin TINYINT NOT NULL
            CONSTRAINT DF_ScimUsers_ResolvedRoleOrigin DEFAULT (0),
        CONSTRAINT CK_ScimUsers_ResolvedRoleOrigin_Valid CHECK (ResolvedRoleOrigin IN (0, 1, 2));
END;
GO

-- Best-effort: historical rows that already had a persisted role likely came from group mapping.
UPDATE dbo.ScimUsers SET ResolvedRoleOrigin = 2 WHERE ResolvedRoleOrigin = 0 AND ResolvedRole IS NOT NULL;
GO

IF OBJECT_ID(N'dbo.AdminNotifications', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AdminNotifications
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_AdminNotifications PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
        RaisedUtc DATETIME2(7) NOT NULL CONSTRAINT DF_AdminNotifications_RaisedUtc DEFAULT SYSUTCDATETIME(),
        Kind NVARCHAR(96) NOT NULL,
        Summary NVARCHAR(512) NOT NULL,
        DataJson NVARCHAR(MAX) NULL
    );
END;
GO
