/*
  082: Tenant-level customer notification channel toggles for governance promotion Logic Apps fan-out.

  RLS: not applied — tenant id is the sole scope; API enforces caller tenant via IScopeContextProvider.
*/
IF OBJECT_ID(N'dbo.TenantNotificationChannelPreferences', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TenantNotificationChannelPreferences
    (
        TenantId                                UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_TenantNotificationChannelPreferences PRIMARY KEY,
        SchemaVersion                           INT              NOT NULL
            CONSTRAINT DF_TenantNotificationChannelPreferences_SchemaVersion DEFAULT 1,
        EmailCustomerNotificationsEnabled       BIT              NOT NULL
            CONSTRAINT DF_TenantNotificationChannelPreferences_Email DEFAULT 1,
        TeamsCustomerNotificationsEnabled       BIT              NOT NULL
            CONSTRAINT DF_TenantNotificationChannelPreferences_Teams DEFAULT 0,
        OutboundWebhookCustomerNotificationsEnabled BIT          NOT NULL
            CONSTRAINT DF_TenantNotificationChannelPreferences_Webhook DEFAULT 0,
        UpdatedUtc                              DATETIME2(7)     NOT NULL
            CONSTRAINT DF_TenantNotificationChannelPreferences_UpdatedUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_TenantNotificationChannelPreferences_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (Id)
    );
END;
GO
