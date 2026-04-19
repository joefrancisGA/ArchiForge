# Runbook — Azure Logic Apps (Standard) for ArchLucid

**Priority:** P3 — Reference  
**Last reviewed:** 2026-04-19 (governance Service Bus subscription + second host notes)

## Objective

Operate optional **Logic App (Standard)** hosts that consume ArchLucid **Service Bus integration events** (see `docs/INTEGRATION_EVENTS_AND_WEBHOOKS.md`) without moving trust boundaries out of .NET (ADR **0016** for Marketplace JWTs, ADR **0019** for orchestration posture).

## When this applies

- You enabled `enable_logic_apps` and/or **`enable_governance_approval_logic_app`** in `infra/terraform-logicapps/` and deployed workflows for governance approvals, trial email fan-out, marketplace fulfillment notifications, incident ChatOps, or customer promotion notices (see `docs/CURSOR_PROMPTS_LOGIC_APPS.md`).
- **Governance approval routing:** enable **`enable_logic_app_governance_approval_subscription`** in `infra/terraform-servicebus/` so a dedicated topic subscription receives only `com.archlucid.governance.approval.submitted` (user property **`event_type`**, matching `AzureServiceBusIntegrationEventPublisher`). After deploying the governance Logic App, set **`governance_logic_app_managed_identity_principal_id`** in Service Bus Terraform to the output **`governance_logic_app_principal_id`** from `terraform-logicapps`, then re-apply Service Bus for **Data Receiver** on the namespace.

## Assumptions

- Integration topic and subscriptions exist (`infra/terraform-servicebus/`).
- Managed identities are granted **Azure Service Bus Data Receiver** on the subscription used by each workflow.
- APIM or private ingress allows the Logic App to call back into `ArchLucid.Api` with Entra-protected routes where human actions apply.

## Procedure

1. **Health:** In Azure Portal, open the Logic App → **Workflows** → confirm last run status. Failed runs should correlate with Service Bus dead-letter depth on the same subscription.
2. **Replay:** For a poison message, fix the workflow or payload contract, then use Service Bus explorer to **dead-letter requeue** after verifying schema under `schemas/integration-events/`.
3. **Disable fan-out:** Set workflow **Enabled** to false (or remove the Service Bus trigger subscription) before disabling the API feature flag that publishes the upstream event — avoids one-sided failures.
4. **Secrets:** Prefer **managed identity** connectors; if a connector requires a secret, store it in Key Vault and reference via Logic App settings — never commit secrets to workflow JSON in git.

## Constraints

- Do **not** place Logic Apps **in front of** anonymous billing webhooks; they consume **`com.archlucid.billing.marketplace.webhook.received.v1`** only after API + provider success.
- Align file share / storage networking with private-endpoint policy; do not expose **SMB (port 445)** publicly.

## Related

- `infra/terraform-logicapps/README.md`
- `docs/adr/0019-logic-apps-standard-edge-orchestration.md`
- `docs/CURSOR_PROMPTS_LOGIC_APPS.md`
