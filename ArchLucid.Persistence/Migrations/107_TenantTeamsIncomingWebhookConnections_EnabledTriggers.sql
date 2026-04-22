/*
  107: Per-tenant Microsoft Teams notification per-trigger opt-in matrix.

  Adds EnabledTriggersJson NVARCHAR(MAX) NOT NULL on dbo.TenantTeamsIncomingWebhookConnections.
  Stores a JSON array of canonical integration event type strings the tenant has opted in to
  (catalog mirrored in code: TeamsNotificationTriggerCatalog / IntegrationEventTypes). Logic Apps
  workflow filters server-side before fan-out so a disabled trigger can never reach Teams even if
  the upstream router misbehaves.

  Default = JSON array of all v1 triggers (resolves PENDING_QUESTIONS.md item 23 sub-bullet
  "Per-trigger Teams opt-in"): existing rows therefore keep current behaviour (all-on) without an
  explicit backfill statement. The CHECK constraint enforces ISJSON = 1 so a future bug cannot
  store malformed payloads.

  RLS: not applied (same posture as 105 — API enforces caller tenant via IScopeContextProvider).
*/
IF OBJECT_ID(N'dbo.TenantTeamsIncomingWebhookConnections', N'U') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1
       FROM sys.columns
       WHERE object_id = OBJECT_ID(N'dbo.TenantTeamsIncomingWebhookConnections')
         AND name = N'EnabledTriggersJson'
   )
BEGIN
    ALTER TABLE dbo.TenantTeamsIncomingWebhookConnections
        ADD EnabledTriggersJson NVARCHAR(MAX) NOT NULL
            CONSTRAINT DF_TenantTeamsIncomingWebhookConnections_EnabledTriggersJson
                DEFAULT (N'["com.archlucid.authority.run.completed","com.archlucid.governance.approval.submitted","com.archlucid.alert.fired","com.archlucid.compliance.drift.escalated","com.archlucid.advisory.scan.completed","com.archlucid.seat.reservation.released"]');
END;
GO

IF OBJECT_ID(N'dbo.TenantTeamsIncomingWebhookConnections', N'U') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1
       FROM sys.check_constraints
       WHERE parent_object_id = OBJECT_ID(N'dbo.TenantTeamsIncomingWebhookConnections')
         AND name = N'CK_TenantTeamsIncomingWebhookConnections_EnabledTriggersJson_IsJson'
   )
BEGIN
    ALTER TABLE dbo.TenantTeamsIncomingWebhookConnections
        ADD CONSTRAINT CK_TenantTeamsIncomingWebhookConnections_EnabledTriggersJson_IsJson
            CHECK (ISJSON(EnabledTriggersJson) = 1);
END;
GO
