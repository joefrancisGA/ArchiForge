# Terraform — Azure Logic Apps (Standard)

Optional root for **Logic App Standard** hosts that subscribe to ArchLucid **integration events** on Service Bus (see `infra/terraform-servicebus/` and `docs/INTEGRATION_EVENTS_AND_WEBHOOKS.md`).

## Governance approval host (optional)

Set **`enable_governance_approval_logic_app = true`** to deploy a **second** Logic App (Standard) site intended for the **`governance-approval-routing`** workflow (Teams / Outlook fan-out → callbacks to `POST /v1/governance/...` approve/reject routes). Use outputs **`governance_logic_app_principal_id`** when wiring **`governance_logic_app_managed_identity_principal_id`** in `infra/terraform-servicebus` for the filtered subscription (see that root’s README).

Workflow JSON and connections are still authored in Portal or CI; placeholder notes live under [`workflows/governance-approval-routing/README.md`](workflows/governance-approval-routing/README.md).

## Marketplace fulfillment host (optional)

Set **`enable_marketplace_fulfillment_logic_app = true`** to deploy a dedicated Logic App (Standard) site for **`marketplace-fulfillment-handoff`** (separate WS1 plan + storage from the generic **`edge`** and **governance** hosts). Use output **`marketplace_fulfillment_logic_app_principal_id`** when wiring **`marketplace_fulfillment_logic_app_managed_identity_principal_id`** in `infra/terraform-servicebus` for the filtered **`com.archlucid.billing.marketplace.webhook.received.v1`** subscription (two-step apply: deploy this root first, then pass the principal id into Service Bus and re-apply).

Workflow notes: [`workflows/marketplace-fulfillment-handoff/README.md`](workflows/marketplace-fulfillment-handoff/README.md).

## Other documented workflows (Portal / export)

- [`workflows/trial-lifecycle-email/README.md`](workflows/trial-lifecycle-email/README.md) — scheduled trial email; pair with `ArchLucid:Notifications:TrialLifecycle:Owner=LogicApp` when the API scan is off.
- [`workflows/incident-chatops/README.md`](workflows/incident-chatops/README.md) — alert fired / resolved to Teams or PagerDuty.
- [`workflows/promotion-customer-notifications/README.md`](workflows/promotion-customer-notifications/README.md) — prod-only promotion fan-out.
- [`workflows/marketplace-fulfillment-handoff/README.md`](workflows/marketplace-fulfillment-handoff/README.md) — **`com.archlucid.billing.marketplace.webhook.received.v1`** after API success (sales / CRM / Teams).

**Dedicated Terraform hosts today:** generic **`edge`** (`enable_logic_apps`), **governance** (`enable_governance_approval_logic_app`), **Marketplace fulfillment** (`enable_marketplace_fulfillment_logic_app`). Trial lifecycle, incident ChatOps, and prod promotion customer workflows are still documented under `workflows/*` only — run them on **`edge`** or add hosts by copying the governance / Marketplace pattern in a fork.

## When to enable

Set `enable_logic_apps = true` only after:

1. A resource group and region are chosen.
2. A **globally unique** `storage_account_name` is reserved (24 chars, lowercase alphanumeric).
3. VNet integration and private endpoints are planned per org policy (align with `infra/terraform-private/`; do not expose SMB publicly).

## Apply

```bash
cd infra/terraform-logicapps
terraform init
terraform plan -var="enable_logic_apps=false"
```

Workflow JSON and in-app connections are **not** defined here; export from the designer or CI and attach via storage share files per Microsoft guidance.

## Related

- `docs/CURSOR_PROMPTS_LOGIC_APPS.md` — implementation prompts for governance, trial email, marketplace, ChatOps, and customer notifications.
- `docs/adr/0019-logic-apps-standard-edge-orchestration.md` — architecture decision.
- `docs/runbooks/LOGIC_APPS_STANDARD.md` — operator notes.
- `docs/runbooks/LOGIC_APPS_INCIDENT_CHATOPS.md` — alert fired/resolved Service Bus user properties and callback routes.
