> **Scope:** ArchLucid — Integration catalog - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# ArchLucid — Integration catalog

**Audience:** Technical evaluators and integration engineers assessing how ArchLucid connects to their ecosystem.

**Last reviewed:** 2026-05-05 — **Jira**, **ServiceNow**, **Slack**, and **Confluence** first-party surfaces in **V1** ([`../library/V1_SCOPE.md`](../library/V1_SCOPE.md) §2.13–§2.15). V1 copy-paste recipes unchanged under `docs/integrations/recipes/`. **Engineering order:** **ServiceNow** → **Confluence** → **Jira** — **Atlassian** (**Confluence** + **Jira**) is **one workstream**, **Confluence** first (*Resolved 2026-05-05 (Atlassian sequencing — Confluence before Jira)* in [`../PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md)); supersedes prior ServiceNow → Jira → Confluence ordering.

**Philosophy:** ArchLucid connects to your tools — you do not run our agents in your infrastructure. All integrations operate via the hosted API, webhooks, or managed connectors.

---

## 1. Available today (V1)

| Integration | Type | Description |
|-------------|------|-------------|
| **REST API** | Outbound / Inbound | OpenAPI 3.0 contract (`/openapi/v1.json`). Full CRUD for runs, manifests, findings, governance, audit, comparisons, alerts. See [../API_CONTRACTS.md](../library/API_CONTRACTS.md). |
| **.NET API client** | Client SDK | Generated NuGet package (`ArchLucid.Api.Client`) from NSwag / OpenAPI spec. |
| **CLI** | Command-line | `archlucid` CLI for scripting, support bundles, and automation. See [../CLI_USAGE.md](../library/CLI_USAGE.md). |
| **Webhook / CloudEvents** | Outbound | Configurable HTTP callbacks on run lifecycle, governance, and alert events. CloudEvents envelope format. |
| **Service Bus** | Outbound | Optional Azure Service Bus integration events for async processing and downstream systems. |
| **Microsoft Teams** | Outbound | Teams **Incoming Webhook** via Logic Apps Standard fan-out; operators register a **Key Vault secret name** per tenant (`GET/POST/DELETE /v1/integrations/teams/connections`). See [../integrations/MICROSOFT_TEAMS_NOTIFICATIONS.md](../integrations/MICROSOFT_TEAMS_NOTIFICATIONS.md). |
| **Slack** | Outbound | First-party **Slack incoming webhook** notifications for alerts / digests / routing — **parity** with Teams: same **`EnabledTriggersJson`**, **Key Vault** secret-name discipline, canonical Authority-shaped payloads ([`../library/V1_SCOPE.md`](../library/V1_SCOPE.md) §2.14). **App Directory** listing and in-Slack **interactive** actions are **not** committed V1 unless owner promotes. |
| **Confluence Cloud** | Outbound | First-party **page publish** (findings / run summaries) to a configured space — **MVP:** single fixed `Confluence:DefaultSpaceKey`, API token / basic auth ([`../library/V1_SCOPE.md`](../library/V1_SCOPE.md) §2.15). OAuth **follow-on** if buyer requires. **Atlassian tranche:** ship **before** **Jira**, **together** in the same V1 workstream. |
| **Procurement ZIP (static)** | Artifact | Reproducible **`dist/procurement-pack.zip`** via **`scripts/build_procurement_pack.sh`** / **`.ps1`** (manifest + SHA-256). No hosted public download — distribute through your procurement portal. See [TRUST_CENTER.md](TRUST_CENTER.md) procurement note. |
| **AsyncAPI** | Contract | Async event contract for webhook and Service Bus consumers. |

### V1 committed — first-party ITSM connectors

Ship tracks **V1 GA** ([`../library/V1_SCOPE.md`](../library/V1_SCOPE.md) §2.13); marketplace/store listings may trail usable connectors.

| Connector | MVP commitment |
|-----------|----------------|
| **ServiceNow** | Finding → **`incident`** with correlation back-link; OAuth 2.0 / basic auth. **`cmdb_ci`** via **`cmdb_ci_appl`** name lookup on **`SystemName`** ([§ Sequencing and CMDB](#sequencing-and-cmdb) below). **Two-way** SNOW→ArchLucid status sync **not** committed unless owner promotes. |
| **Jira** | Finding → issue with correlation back-link; **bi-directional** status sync **in V1** (may fast-follow). OAuth 2.0 / API token auth. **Atlassian tranche:** ships **after** **Confluence** in the **same** paired workstream. |

### V1 committed — Slack (chat-ops)

| Surface | MVP commitment |
|---------|------------------|
| **Slack** | Outbound **incoming-webhook** notifications — parity with **Teams** (`EnabledTriggersJson`, **Key Vault** secret names, Authority-shaped payloads). See [`../library/V1_SCOPE.md`](../library/V1_SCOPE.md) §2.14. **App Directory** listing / in-message **interactions**: not V1 unless promoted. |

### V1 committed — Confluence (documentation)

| Surface | MVP commitment |
|---------|------------------|
| **Confluence** | One-way **publish** to **`Confluence:DefaultSpaceKey`**; **API token + basic auth** for MVP; OAuth follow-on. See [`../library/V1_SCOPE.md`](../library/V1_SCOPE.md) §2.15. **Before** **Jira** in the **paired Atlassian** workstream. |

### Sequencing and CMDB

- **Build order:** **ServiceNow** first. Then **Atlassian pair**: **Confluence** publish **before** **Jira** issue sync — **same** engineering workstream / release tranche ([`../library/V1_SCOPE.md`](../library/V1_SCOPE.md) §2.13 / §2.15; [`../PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md) *Resolved 2026-05-05 (Atlassian sequencing — Confluence before Jira)*). Until connectors are enabled in your tenant, use **customer-owned** recipes ([§3](#3-build-your-own) below).
- **CMDB CI class:** **`cmdb_ci_appl`** (Application CI). Match ArchLucid **`SystemName`** to ServiceNow **`name`**; when a row is found, set incident **`cmdb_ci`** to that record’s **`sys_id`**. If no row matches, leave **`cmdb_ci`** empty. **Illustrative Table API lookup:** `GET /api/now/table/cmdb_ci_appl?sysparm_query=name={SystemName}&sysparm_limit=1` (escape/`encodeURIComponent` **`SystemName`** per instance rules).
- **`ServiceNow:AutoCreateCmdbCi`:** Tenant option, default **`false`**. When **`true`**, the connector may create a new **`cmdb_ci_appl`** row when lookup finds no match; when **`false`**, never auto-create.
- **Two-way status sync:** ServiceNow → ArchLucid status sync is **not** in committed **V1** scope.

### Authentication for integrations

| Method | Use case | Reference |
|--------|----------|-----------|
| **Entra ID (JWT)** | Production integrations, CI/CD pipelines with service principals | [../SECURITY.md](../library/SECURITY.md), [TENANT_ISOLATION.md](TENANT_ISOLATION.md) |
| **API keys** | Automation, scripts, lightweight integrations | [../SECURITY.md](../library/SECURITY.md) (RBAC, key rotation) |

---

## 2. Planned connectors [Roadmap]

**ITSM + documentation sequencing:** **ServiceNow** → **Confluence** → **Jira** for **V1** — **Confluence** and **Jira** are **paired** (*Resolved 2026-05-05 (Atlassian sequencing — Confluence before Jira)* in [`../PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md)).

### V1 supported patterns (copy-paste recipes)

These are **customer-operated** integration patterns that consume CloudEvents-style payloads — they **do not** replace **V1** first-party commitments for **Jira** / **ServiceNow** / **Confluence** ([§1](#1-available-today-v1)); they remain useful optional bridges.

| Pattern | Document |
|---------|----------|
| Azure Logic Apps webhook → Azure DevOps work item | [recipe-azure-logic-apps-webhook-to-ado-work-item.md](../integrations/recipes/recipe-azure-logic-apps-webhook-to-ado-work-item.md) |
| ServiceNow incident → **Logic Apps** (Azure-first) | [SERVICENOW_INCIDENT_VIA_LOGIC_APPS.md](../integrations/recipes/SERVICENOW_INCIDENT_VIA_LOGIC_APPS.md) |
| Event Grid / HTTPS subscriber hardening checklist | [recipe-event-grid-webhook-hardening-checklist.md](../integrations/recipes/recipe-event-grid-webhook-hardening-checklist.md) |

Broader recipe hub: [ITSM_BRIDGE_V1_RECIPES.md](../library/ITSM_BRIDGE_V1_RECIPES.md) · No-code folder index: [integrations/recipes/README.md](../integrations/recipes/README.md).

| Category | Connector | Description | Status |
|----------|-----------|-------------|--------|
| **Identity** | SCIM provisioning | Sync users and groups from Okta, Entra ID, or other IdPs | **Available today** — see [`docs/integrations/SCIM_PROVISIONING.md`](../integrations/SCIM_PROVISIONING.md) |
| **Architecture import** | Structurizr DSL | Import architecture models from Structurizr workspace files | [Planned] |
| **Architecture import** | ArchiMate XML | Import from TOGAF / ArchiMate modeling tools | [Planned] |
| **Architecture import** | Terraform state | Parse `terraform show -json` output into ArchLucid context | [Planned] |
| **ITSM / Atlassian** | Jira | Create Jira issues from findings; sync status back | **Canonical scope:** **[V1 — committed]** ([`../library/V1_SCOPE.md`](../library/V1_SCOPE.md) §2.13). **V1 bridge (customer-operated, optional):** [`../../templates/integrations/jira/jira-webhook-bridge-recipe.md`](../../templates/integrations/jira/jira-webhook-bridge-recipe.md) — HMAC, CloudEvents (`com.archlucid.authority.run.completed` + GET run for findings, or `com.archlucid.alert.fired` direct), Jira REST v3, Logic App / Function outline. |
| **Documentation / Atlassian** | Confluence | Publish architecture findings and run summaries to a Confluence space | **Canonical scope:** **[V1 — committed]** ([`../library/V1_SCOPE.md`](../library/V1_SCOPE.md) §2.15). **MVP:** one-way publish to **`Confluence:DefaultSpaceKey`**; API token / basic auth; OAuth follow-on. Supersedes prior **V1.1-only** catalog row (*owner scope update 2026-05-05*). Design intent: [PENDING_QUESTIONS.md](../PENDING_QUESTIONS.md) Improvement 3 (3a / 3b). [`../library/V1_DEFERRED.md` §6](../library/V1_DEFERRED.md). **Optional** Logic Apps recipe: [CONFLUENCE_PAGE_VIA_LOGIC_APPS.md](../integrations/recipes/CONFLUENCE_PAGE_VIA_LOGIC_APPS.md). |
| **ITSM** | ServiceNow | Create ServiceNow `incident` records from findings; **`cmdb_ci`** via **`cmdb_ci_appl`** lookup on **`SystemName`** (see [§1](#1-available-today-v1) **Sequencing and CMDB**) | **Canonical scope:** **[V1 — committed]** ([`../library/V1_SCOPE.md`](../library/V1_SCOPE.md) §2.13). **V1 bridge (customer-operated, optional):** [`../../templates/integrations/servicenow/servicenow-incident-recipe.md`](../../templates/integrations/servicenow/servicenow-incident-recipe.md) — same event types, ServiceNow Table API, field mapping, Logic App outline. |
| **ITSM** | Azure DevOps Work Items | Create work items from findings; sync status back | [Planned] |
| **Chat-ops** | Slack | Outbound notification sink (**V1 — committed**) — parity with the shipped Microsoft Teams connector (same **`EnabledTriggersJson`**, **Key Vault** secrets, canonical payloads). See [`../library/V1_SCOPE.md`](../library/V1_SCOPE.md) §2.14. **App Directory** / in-Slack **interactive** actions: not committed V1 unless promoted. |
| **Observability** | SIEM export (CEF/syslog) | Native audit log export in SIEM-friendly formats | [Planned] — see [SIEM_EXPORT.md](SIEM_EXPORT.md) for current methods |
| **CI/CD** | GitHub Actions | Architecture review as a PR check | [Example available] — see [../integrations/CICD_INTEGRATION.md](../integrations/CICD_INTEGRATION.md) |
| **CI/CD** | Azure DevOps Pipelines | Architecture review as a pipeline task | [Example available] — see [../integrations/CICD_INTEGRATION.md](../integrations/CICD_INTEGRATION.md) |
| **CI/CD** | Azure DevOps Repos (pipelines) | Same `GET /v1/compare` Markdown as GitHub Actions — job summary + sticky PR thread (`integrations/azure-devops-task-manifest-delta*`) | [Shipped] — see [../integrations/AZURE_DEVOPS_PIPELINE_TASK_MANIFEST_DELTA_PR_COMMENT.md](../integrations/AZURE_DEVOPS_PIPELINE_TASK_MANIFEST_DELTA_PR_COMMENT.md) · [ADR 0024](../adr/0024-azure-devops-pipeline-task-parity-with-github-action.md) |
| **CI/CD** | Azure DevOps Repos (Service Bus) | PR thread + status on manifest commit (`com.archlucid.authority.run.completed`) — **zero pipeline changes** | [Shipped] — opt-in Worker handler — see [../integrations/AZURE_DEVOPS_PR_DECORATION_SERVER_SIDE.md](../integrations/AZURE_DEVOPS_PR_DECORATION_SERVER_SIDE.md) |

---

## 3. Build your own

**End-to-end recipe hub (Azure DevOps PR decoration, CloudEvents consumer outline, Power Automate / Logic Apps):** see **[ITSM_BRIDGE_V1_RECIPES.md](../library/ITSM_BRIDGE_V1_RECIPES.md)** — consolidated walkthroughs with exact doc and repo paths. **First-party** **Jira**, **ServiceNow**, **Slack** (chat-ops), and **Confluence** are **V1 commitments** ([`V1_SCOPE.md` §2.13–§2.15](../library/V1_SCOPE.md)). Recipes stay **optional** customer-operated bridges.

ArchLucid's architecture is designed for extensibility:

- **Context connectors:** Implement `IContextConnector` to bring new data sources into the analysis pipeline. See the finding engine template: `dotnet new archlucid-finding-engine`.
- **Outbound consumers:** Subscribe to CloudEvents webhooks or Service Bus topics to trigger workflows in your systems.
- **API automation:** Use the REST API or .NET client to build custom integrations.
- **ITSM + chat-ops + docs:** **V1** ships first-party **Jira**, **ServiceNow**, **Slack**, and **Confluence** publish ([`V1_SCOPE.md` §2.13–§2.15](../library/V1_SCOPE.md)). Until enabled for your tenant — or when you prefer Microsoft automation — use **customer-owned** recipes under [`docs/integrations/recipes/`](../../integrations/recipes/README.md): **Logic Apps–first:** [ServiceNow (Logic Apps)](../../integrations/recipes/SERVICENOW_INCIDENT_VIA_LOGIC_APPS.md), [Confluence (Logic Apps)](../../integrations/recipes/CONFLUENCE_PAGE_VIA_LOGIC_APPS.md); **Power Automate:** [ServiceNow](../../integrations/recipes/SERVICENOW_INCIDENT_VIA_POWER_AUTOMATE.md), [Jira](../../integrations/recipes/JIRA_ISSUE_VIA_POWER_AUTOMATE.md); **webhook bridge** templates: [ServiceNow](../../templates/integrations/servicenow/servicenow-incident-recipe.md), [Jira](../../templates/integrations/jira/jira-webhook-bridge-recipe.md). Starter **fixture→mapping parity** for bridge authors (Node built-in **`--test`**) lives under [`templates/integrations/bridge-recipe-contract-tests/`](../../templates/integrations/bridge-recipe-contract-tests/README.md), matching CI. Event types: [schemas/integration-events/catalog.json](../../schemas/integration-events/catalog.json) and [INTEGRATION_EVENTS_AND_WEBHOOKS.md](../library/INTEGRATION_EVENTS_AND_WEBHOOKS.md).

---

## 4. Request an integration

Contact **integrations@archlucid.dev** (placeholder) with your use case. Integration requests inform the connector roadmap.

---

## Related documents

| Doc | Use |
|-----|-----|
| [TRUST_CENTER.md](TRUST_CENTER.md) | Trust index |
| [POSITIONING.md](POSITIONING.md) | Product positioning |
| [../API_CONTRACTS.md](../library/API_CONTRACTS.md) | API surface detail |
| [SIEM_EXPORT.md](SIEM_EXPORT.md) | Audit export for SIEM |
| [../integrations/CICD_INTEGRATION.md](../integrations/CICD_INTEGRATION.md) | CI/CD pipeline examples |
