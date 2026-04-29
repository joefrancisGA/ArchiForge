-- 131: Trial local identity handoff — link dbo.IdentityUsers rows to Entra object id after paid conversion.

IF OBJECT_ID(N'dbo.IdentityUsers', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.IdentityUsers', N'LinkedEntraOid') IS NULL
        ALTER TABLE dbo.IdentityUsers ADD LinkedEntraOid NVARCHAR(128) NULL;

    IF COL_LENGTH(N'dbo.IdentityUsers', N'LinkedUtc') IS NULL
        ALTER TABLE dbo.IdentityUsers ADD LinkedUtc DATETIMEOFFSET NULL;
END;
GO
