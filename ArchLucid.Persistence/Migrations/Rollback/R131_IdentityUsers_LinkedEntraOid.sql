IF OBJECT_ID(N'dbo.IdentityUsers', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.IdentityUsers', N'LinkedEntraOid') IS NOT NULL
        ALTER TABLE dbo.IdentityUsers DROP COLUMN LinkedEntraOid;

    IF COL_LENGTH(N'dbo.IdentityUsers', N'LinkedUtc') IS NOT NULL
        ALTER TABLE dbo.IdentityUsers DROP COLUMN LinkedUtc;
END;
GO
