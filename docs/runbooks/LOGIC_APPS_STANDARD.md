> **Scope:** Runbook — Azure Logic Apps (Standard) for ArchLucid - full detail, tables, and links in the sections below.

# Runbook — Azure Logic Apps (Standard) for ArchLucid

**Priority:** P3 — Reference  
**Last reviewed:** 2026-04-19 (trial / ChatOps / promotion optional dedicated Logic App hosts in `terraform-logicapps`)

## Objective

Operate optional **Logic App (Standard)** hosts that consume ArchLucid **Service Bus integration events** (see `docs/INTEGRATION_EVENTS_AND_WEBHOOKS.md`) without moving trust boundaries out of .NET (ADR **0016** for Marketplace JWTs, ADR **0019** for orchestration posture).

## When this applies

- You enabled any combination of **`enable_logic_apps`**, **`enable_governance_approval_logic_app`**, **`enable_marketplace_fulfillment_logic_app`**, **`enable_trial_lifecycle_logic_app`**, **`enable_incident_chatops_logic_app`**, and **`enable_promotion_customer_notify_logic_app`** in `infra/terraform-logicapps/`, and deployed workflows for governance, trial email, Marketplace fulfillment, incident ChatOps, or prod promotion customer notices (see `docs/CURSOR_PROMPTS_LOGIC_APPS.md`).
- **Governance approval routing:** enable **`enable_logic_app_governance_approval_subscription`** in `infra/terraform-servicebus/` so a dedicated topic subscription receives only `com.archlucid.governance.approval.submitted` (user property **`event_type`**, matching `AzureServiceBusIntegrationEventPublisher`). After deploying the governance Logic App, set **`governance_logic_app_managed_identity_principal_id`** in Service Bus Terraform to the output **`governance_logic_app_principal_id`** from `terraform-logicapps`, then re-apply Service Bus for **Data Receiver** on the namespace.
- **Marketplace fulfillment hand-off:** enable **`enable_logic_app_marketplace_fulfillment_subscription`** in `infra/terraform-servicebus/` and set **`marketplace_fulfillment_logic_app_managed_identity_principal_id`** to **`marketplace_fulfillment_logic_app_principal_id`** from `terraform-logicapps` after deploying **`enable_marketplace_fulfillment_logic_app`** (same two-step apply as governance). Trigger on output **`logic_app_marketplace_fulfillment_subscription_name`**.
- **Trial lifecycle email:** **`enable_logic_app_trial_lifecycle_email_subscription`** + **`trial_lifecycle_logic_app_managed_identity_principal_id`** ← **`trial_lifecycle_logic_app_principal_id`** after **`enable_trial_lifecycle_logic_app`**. Trigger on **`logic_app_trial_lifecycle_email_subscription_name`**.
- **Incident ChatOps:** **`enable_logic_app_incident_chatops_subscription`** + **`incident_chatops_logic_app_managed_identity_principal_id`** ← **`incident_chatops_logic_app_principal_id`** after **`enable_incident_chatops_logic_app`**. Trigger on **`logic_app_incident_chatops_subscription_name`**.
- **Promotion customer notify (prod):** **`enable_logic_app_promotion_prod_customer_subscription`** + **`promotion_customer_notify_logic_app_managed_identity_principal_id`** ← **`promotion_customer_notify_logic_app_principal_id`** after **`enable_promotion_customer_notify_logic_app`**. Trigger on **`logic_app_promotion_prod_customer_subscription_name`**.

## Assumptions

- Integration topic and subscriptions exist (`infra/terraform-servicebus/`).
- Managed identities are granted **Azure Service Bus Data Receiver** on the subscription used by each workflow.
- APIM or private ingress allows the Logic App to call back into `ArchLucid.Api` with Entra-protected routes where human actions apply.

## Procedure

1. **Diagnostics (recommended):** In `infra/terraform-logicapps/`, set **`enable_logic_app_diagnostic_settings = true`** and **`logic_app_diagnostic_log_analytics_workspace_id`** to your Log Analytics workspace resource ID, then `terraform apply`. That attaches **Diagnostic settings** (all log category groups + **AllMetrics**) to each Logic App Standard site this root deploys. Query tables such as **`AppServicePlatformLogs`** and **`WorkflowRuntime`** in Log Analytics; tune workspace retention for **cost** vs **forensics**.
2. **Health:** In Azure Portal, open the Logic App → **Workflows** → confirm last run status. Failed runs should correlate with Service Bus dead-letter depth on the same subscription.
3. **Replay:** For a poison message, fix the workflow or payload contract, then use Service Bus explorer to **dead-letter requeue** after verifying schema under `schemas/integration-events/`.
4. **Disable fan-out:** Set workflow **Enabled** to false (or remove the Service Bus trigger subscription) before disabling the API feature flag that publishes the upstream event — avoids one-sided failures.
5. **Secrets:** Prefer **managed identity** connectors; if a connector requires a secret, store it in Key Vault and reference via Logic App settings — never commit secrets to workflow JSON in git.

## Constraints

- Do **not** place Logic Apps **in front of** anonymous billing webhooks; they consume **`com.archlucid.billing.marketplace.webhook.received.v1`** only after API + provider success.
- Align file share / storage networking with private-endpoint policy; do not expose **SMB (port 445)** publicly.

## Related

- `infra/terraform-logicapps/README.md`
- `docs/adr/0019-logic-apps-standard-edge-orchestration.md`
- `docs/CURSOR_PROMPTS_LOGIC_APPS.md`
- `docs/runbooks/LOGIC_APPS_INCIDENT_CHATOPS.md` — fired/resolved payloads, Service Bus user properties, callback routes
