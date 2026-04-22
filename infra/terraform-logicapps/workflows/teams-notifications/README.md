# Microsoft Teams notifications (Logic Apps Standard)

**Purpose:** fan out ArchLucid integration events from **Azure Service Bus** to a Microsoft Teams channel using an **Incoming Webhook** URL resolved at runtime from **Azure Key Vault** (never stored in ArchLucid SQL — see `POST /v1/integrations/teams/connections`).

## Terraform host

When `enable_teams_notifications_logic_app = true`, this module provisions a dedicated **Logic App (Standard)** site (`teams_notifications_logic_app_name`), backing storage, and WS1 plan — same shape as `trial-lifecycle-email` and `incident-chatops`.

## Workflow design (author in Designer)

1. **Service Bus trigger** — subscribe to the integration topic (see [`docs/INTEGRATION_EVENTS_AND_WEBHOOKS.md`](../../../docs/INTEGRATION_EVENTS_AND_WEBHOOKS.md)).
2. **Filter / switch** on `eventType` (catalog: [`schemas/integration-events/catalog.json`](../../../schemas/integration-events/catalog.json)). The v1 default trigger set was extended on **2026-04-21** (PENDING_QUESTIONS.md item 32):
   - `com.archlucid.authority.run.completed` — run committed path.
   - `com.archlucid.governance.approval.submitted` — governance approval requested.
   - `com.archlucid.alert.fired` — alert raised.
   - `com.archlucid.compliance.drift.escalated` — compliance drift breached its threshold and escalated. **(added 2026-04-21)**
   - `com.archlucid.advisory.scan.completed` — advisory finding scan committed a fresh result. **(added 2026-04-21)**
   - `com.archlucid.seat.reservation.released` — a trial seat reservation expired or was released, freeing capacity. **(added 2026-04-21)**

   Card layout convention: **headline (bold)**, **tenant + workspace** as a sub-line, **action link** to the operator UI route most relevant to the event (run page / governance approval page / alert page / compliance dashboard / advisory finding / trial seat dashboard). Re-use the existing Adaptive Card schema; no new card variants per trigger.
3. **Per-tenant trigger opt-in filter (added 2026-04-21 — PENDING_QUESTIONS.md item 23 sub-bullet "Per-trigger Teams opt-in").** Before the HTTP POST, the workflow **must** look up the tenant's row from `dbo.TenantTeamsIncomingWebhookConnections` and abort if the current event's `eventType` is **not** in the row's `EnabledTriggersJson` array. Two implementation paths:
   - **API path (preferred):** call `GET /v1/integrations/teams/connections` with managed identity or an operator API key (the response carries `enabledTriggers: string[]`); short-circuit when `enabledTriggers.includes(eventType)` is false.
   - **Direct SQL path (fallback when API is unreachable):** Logic App Standard SQL connector running `SELECT 1 FROM dbo.TenantTeamsIncomingWebhookConnections WHERE TenantId = @tenantId AND ISJSON(EnabledTriggersJson) = 1 AND EXISTS (SELECT 1 FROM OPENJSON(EnabledTriggersJson) WHERE [value] = @eventType)` — abort on zero rows.

   Either path must run server-side **before** the Key Vault secret fetch so a disabled trigger never resolves a webhook URL. The catalog is the source of truth in `ArchLucid.Core.Notifications.Teams.TeamsNotificationTriggerCatalog`; the SQL column defaults to **all-on** so existing rows keep current behaviour without an explicit backfill.
4. **HTTP GET** (optional) — `GET /v1/notifications/customer-channel-preferences` with managed identity or API key to respect **channel-level** tenant Teams toggles (different concern from per-trigger opt-in: a tenant can disable Teams entirely OR opt-out specific triggers).
5. **HTTP POST Incoming Webhook** — build an **Adaptive Card** JSON body; webhook URL from **Key Vault Get Secret** action using the secret **name** stored per tenant via the ArchLucid API.

## RBAC

Grant the Logic App’s **system-assigned managed identity** `Azure Service Bus Data Receiver` on the topic/subscription used for integration events, and **Get** on the Key Vault secrets referenced by operators.

## References

- [`docs/integrations/MICROSOFT_TEAMS_NOTIFICATIONS.md`](../../../docs/integrations/MICROSOFT_TEAMS_NOTIFICATIONS.md)
- [`docs/adr/0019-logic-apps-standard-edge-orchestration.md`](../../../docs/adr/0019-logic-apps-standard-edge-orchestration.md)
