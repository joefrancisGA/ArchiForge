/*
  103: Weekly executive digest email preferences (per-tenant schedule + recipients).

  RLS: not applied — tenant id is the sole scope; API enforces caller tenant via IScopeContextProvider.
*/
IF OBJECT_ID(N'dbo.TenantExecDigestPreferences', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TenantExecDigestPreferences
    (
        TenantId                    UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_TenantExecDigestPreferences PRIMARY KEY,
        SchemaVersion               INT              NOT NULL
            CONSTRAINT DF_TenantExecDigestPreferences_SchemaVersion DEFAULT 1,
        EmailEnabled                BIT              NOT NULL
            CONSTRAINT DF_TenantExecDigestPreferences_EmailEnabled DEFAULT 0,
        RecipientEmails             NVARCHAR(2000) NULL,
        IanaTimeZoneId              NVARCHAR(128)  NOT NULL
            CONSTRAINT DF_TenantExecDigestPreferences_Tz DEFAULT N'UTC',
        DayOfWeek                   TINYINT          NOT NULL
            CONSTRAINT DF_TenantExecDigestPreferences_Dow DEFAULT 1,
        HourOfDay                   TINYINT          NOT NULL
            CONSTRAINT DF_TenantExecDigestPreferences_Hour DEFAULT 8,
        UpdatedUtc                  DATETIME2(7)     NOT NULL
            CONSTRAINT DF_TenantExecDigestPreferences_UpdatedUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT CK_TenantExecDigestPreferences_Dow CHECK (DayOfWeek BETWEEN 0 AND 6),
        CONSTRAINT CK_TenantExecDigestPreferences_Hour CHECK (HourOfDay BETWEEN 0 AND 23),
        CONSTRAINT FK_TenantExecDigestPreferences_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (Id)
    );
END;
GO
