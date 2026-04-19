# Terraform — Azure Logic Apps (Standard)

Optional root for **Logic App Standard** hosts that subscribe to ArchLucid **integration events** on Service Bus (see `infra/terraform-servicebus/` and `docs/INTEGRATION_EVENTS_AND_WEBHOOKS.md`).

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
