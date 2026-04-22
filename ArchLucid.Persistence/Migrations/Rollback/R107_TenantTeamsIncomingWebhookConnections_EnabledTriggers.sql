IF OBJECT_ID(N'dbo.TenantTeamsIncomingWebhookConnections', N'U') IS NOT NULL
   AND EXISTS (
       SELECT 1
       FROM sys.check_constraints
       WHERE parent_object_id = OBJECT_ID(N'dbo.TenantTeamsIncomingWebhookConnections')
         AND name = N'CK_TenantTeamsIncomingWebhookConnections_EnabledTriggersJson_IsJson'
   )
BEGIN
    ALTER TABLE dbo.TenantTeamsIncomingWebhookConnections
        DROP CONSTRAINT CK_TenantTeamsIncomingWebhookConnections_EnabledTriggersJson_IsJson;
END;
GO

IF OBJECT_ID(N'dbo.TenantTeamsIncomingWebhookConnections', N'U') IS NOT NULL
   AND EXISTS (
       SELECT 1
       FROM sys.default_constraints
       WHERE parent_object_id = OBJECT_ID(N'dbo.TenantTeamsIncomingWebhookConnections')
         AND name = N'DF_TenantTeamsIncomingWebhookConnections_EnabledTriggersJson'
   )
BEGIN
    ALTER TABLE dbo.TenantTeamsIncomingWebhookConnections
        DROP CONSTRAINT DF_TenantTeamsIncomingWebhookConnections_EnabledTriggersJson;
END;
GO

IF OBJECT_ID(N'dbo.TenantTeamsIncomingWebhookConnections', N'U') IS NOT NULL
   AND EXISTS (
       SELECT 1
       FROM sys.columns
       WHERE object_id = OBJECT_ID(N'dbo.TenantTeamsIncomingWebhookConnections')
         AND name = N'EnabledTriggersJson'
   )
BEGIN
    ALTER TABLE dbo.TenantTeamsIncomingWebhookConnections
        DROP COLUMN EnabledTriggersJson;
END;
GO
