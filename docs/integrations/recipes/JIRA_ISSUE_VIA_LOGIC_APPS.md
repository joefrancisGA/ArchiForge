> **Scope:** Azure Logic Apps (Standard) companion to the Power Automate Jira recipe — same CloudEvents payload and Jira REST calls, Azure-first operational model preferred for V1 evaluator docs.

# Jira issue via Azure Logic Apps (Logic Apps–first recipe)

**Audience:** Teams standardized on **Azure Logic Apps Standard** who need Jira issues from ArchLucid findings or alerts **without Power Automate Premium HTTP connectors.**

**Optional customer-owned bridge.** **First-party Jira** is **in scope for V1 GA** ([`V1_SCOPE.md`](../../library/V1_SCOPE.md) §2.13). Same Automation-platform variant as [JIRA_ISSUE_VIA_POWER_AUTOMATE.md](JIRA_ISSUE_VIA_POWER_AUTOMATE.md).

> **Customer-owned:** Workflow runs in **your** Azure subscription; calls **your** Jira Cloud REST API using **your** Atlassian credentials. Not an Atlassian Marketplace “ArchLucid” app.

**Canonical narrative, JSON schema on the HTTP trigger, field tables, severity mapping, example `POST /rest/api/3/issue` bodies:** copy from [JIRA_ISSUE_VIA_POWER_AUTOMATE.md](JIRA_ISSUE_VIA_POWER_AUTOMATE.md) §1–7 (same event types: `com.archlucid.authority.run.completed`, `com.archlucid.alert.fired`).

---

## Logic Apps–specific outline

1. **Ingress** — Mirror [SERVICENOW_INCIDENT_VIA_LOGIC_APPS.md](SERVICENOW_INCIDENT_VIA_LOGIC_APPS.md): Service Bus filtered subscription preferred, or HTTP trigger behind APIM / Function terminating TLS + HMAC per [INTEGRATION_EVENTS_AND_WEBHOOKS.md](../../library/INTEGRATION_EVENTS_AND_WEBHOOKS.md).
2. **Parse** — Parse JSON action on CloudEvents envelope; `@body('Parse_JSON')?['type']` switch matches the Power Automate conditions.
3. **Run completed** — HTTP GET `GET /v1/authority/runs/{runId}` with API key / Entra token; foreach finding → HTTP POST `https://{site}.atlassian.net/rest/api/3/issue` (issue body identical to Power Automate recipe §5).
4. **Alert fired** — Single HTTP POST to Jira `/issue` using `data` fields from alert payload without extra ArchLucid GET.
5. **Idempotency / retries** — Track CloudEvents `id` (Azure Table Storage or Logic Apps run-scope variables) before creating issues; respect Jira 429 backoff.

**Hosts and Standard SKU notes:** [LOGIC_APPS_STANDARD.md](../../runbooks/LOGIC_APPS_STANDARD.md). **Pattern reference:** [recipe-azure-logic-apps-webhook-to-ado-work-item.md](recipe-azure-logic-apps-webhook-to-ado-work-item.md).

---

*Last reviewed: 2026-05-01 — Logic Apps companion to JIRA_ISSUE_VIA_POWER_AUTOMATE; detailed REST steps deferred to sibling doc for parity.*
