> **Scope:** Azure Logic Apps (Standard) variant of the ServiceNow incident recipe — same CloudEvents contracts as the Power Automate flow, Azure-first operational model.

# ServiceNow incident via Azure Logic Apps (Logic Apps–first recipe)

**Audience:** Teams standardized on **Azure Logic Apps Standard** (single-tenant) who want ServiceNow incidents from ArchLucid without Power Automate Premium HTTP connectors.

**V1 interim bridge.** Same status as [SERVICENOW_INCIDENT_VIA_POWER_AUTOMATE.md](SERVICENOW_INCIDENT_VIA_POWER_AUTOMATE.md): first-party ServiceNow connector is **V1.1** — see [V1_DEFERRED.md](../../library/V1_DEFERRED.md) (section 6) and [INTEGRATION_CATALOG.md](../../go-to-market/INTEGRATION_CATALOG.md).

> **Customer-owned:** Workflow runs in **your** Azure subscription; calls **your** ServiceNow Table API. ArchLucid delivers signed webhooks per [INTEGRATION_EVENTS_AND_WEBHOOKS.md](../../library/INTEGRATION_EVENTS_AND_WEBHOOKS.md).

**Canonical field mapping and JSON schema for the HTTP trigger:** copy from [SERVICENOW_INCIDENT_VIA_POWER_AUTOMATE.md](SERVICENOW_INCIDENT_VIA_POWER_AUTOMATE.md) §2–4 (event types `com.archlucid.authority.run.completed` and `com.archlucid.alert.fired`, parse `type`, branch, optional `GET /v1/authority/runs/{runId}`).

---

## Logic Apps–specific outline

1. **Ingress** — Prefer **Service Bus** subscription (filtered on `event_type`) or **HTTP trigger** if you terminate TLS + verify HMAC at APIM/Function in front of the workflow; align with [INTEGRATION_EVENTS_AND_WEBHOOKS.md](../../library/INTEGRATION_EVENTS_AND_WEBHOOKS.md).
2. **Parse** — Use **Parse JSON** on the CloudEvents body; switch on `@body('Parse_JSON')?['type']` the same way as the Power Automate **Condition** branches.
3. **Run completed** — **HTTP** action: `GET` ArchLucid `v1/authority/runs/{runId}` with API key / Entra token; **For each** finding → **HTTP POST** `https://<instance>.service-now.com/api/now/table/incident` with JSON body matching the Power Automate recipe’s field table.
4. **Alert fired** — Single **HTTP POST** to the same Table API using fields from `data` (no extra ArchLucid GET).
5. **Idempotency** — Persist CloudEvents `id` / Service Bus `MessageId` in a small Azure Table or workflow run-scope cache if you must suppress duplicates on redelivery.

**Operator runbook (hosts, identities, diagnostics):** [LOGIC_APPS_STANDARD.md](../../runbooks/LOGIC_APPS_STANDARD.md).

**Related:** [recipe-azure-logic-apps-webhook-to-ado-work-item.md](recipe-azure-logic-apps-webhook-to-ado-work-item.md) (pattern for Logic Apps + integration events), [CONFLUENCE_PAGE_VIA_LOGIC_APPS.md](CONFLUENCE_PAGE_VIA_LOGIC_APPS.md).

---

*Last reviewed: 2026-05-01 — Logic Apps–first ServiceNow bridge; defers detailed steps to Power Automate twin for schema parity.*
