> **Scope:** ArchLucid — Integration catalog - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# ArchLucid — Integration catalog

**Audience:** Technical evaluators and integration engineers assessing how ArchLucid connects to their ecosystem.

**Last reviewed:** 2026-05-05 — **Jira** + **ServiceNow** first-party connectors promoted to **V1** ([`../library/V1_SCOPE.md`](../library/V1_SCOPE.md) §2.13; [`../PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md) *Resolved 2026-05-05*). **Confluence** stays **V1.1**. V1 copy-paste recipes unchanged under `docs/integrations/recipes/`. ITSM **engineering order:** ServiceNow before Jira (*Resolved 2026-04-27*, superseded only for scope pinning by *Resolved 2026-05-05*).

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
| **Procurement ZIP (static)** | Artifact | Reproducible **`dist/procurement-pack.zip`** via **`scripts/build_procurement_pack.sh`** / **`.ps1`** (manifest + SHA-256). No hosted public download — distribute through your procurement portal. See [TRUST_CENTER.md](TRUST_CENTER.md) procurement note. |
| **AsyncAPI** | Contract | Async event contract for webhook and Service Bus consumers. |

### V1 committed — first-party ITSM connectors

Ship tracks **V1 GA** ([`../library/V1_SCOPE.md`](../library/V1_SCOPE.md) §2.13); marketplace/store listings may trail usable connectors.

| Connector | MVP commitment |
|-----------|----------------|
| **ServiceNow** | Finding → **`incident`** with correlation back-link; OAuth 2.0 / basic auth. Optional **`cmdb_ci`** mapping is an **open planning** topic (same release vs fast-follow). **Two-way** SNOW→ArchLucid status sync **not** committed unless owner promotes. |
| **Jira** | Finding → issue with correlation back-link; **bi-directional** status sync **in V1** (may fast-follow). OAuth 2.0 / API token auth. |

**Build order:** ServiceNow **before** Jira ([`../PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md) *Resolved 2026-04-27*, scope pinning updated *Resolved 2026-05-05*). Until enabled in your tenant, use **customer-owned** recipes ([§3](#3-build-your-own) below).

### Authentication for integrations

| Method | Use case | Reference |
|--------|----------|-----------|
| **Entra ID (JWT)** | Production integrations, CI/CD pipelines with service principals | [../SECURITY.md](../library/SECURITY.md), [TENANT_ISOLATION.md](TENANT_ISOLATION.md) |
| **API keys** | Automation, scripts, lightweight integrations | [../SECURITY.md](../library/SECURITY.md) (RBAC, key rotation) |

---

## 2. Planned connectors [Roadmap]

**ITSM sequencing:** **ServiceNow** **before** **Jira** for **V1** first-party connectors ([`../PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md) *Resolved 2026-04-27*, scope *Resolved 2026-05-05*). **Confluence** remains **V1.1**.

### V1 supported patterns (copy-paste recipes)

These are **customer-operated** integration patterns that consume CloudEvents-style payloads — they **do not** replace **V1** first-party commitments for **Jira** / **ServiceNow** ([§1](#1-available-today-v1)); they remain useful optional bridges.

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
| **Documentation / Atlassian** | Confluence | Publish architecture findings and run summaries to a Confluence space | **[V1.1 — planned]** — explicitly **out of scope for V1**, **in scope for V1.1** (owner decision 2026-04-24; **Jira** is **not** grouped here — see §1 **V1 committed** table). Minimum viable shape: one-way publish to a single fixed `Confluence:DefaultSpaceKey` using API token / basic auth. OAuth 2.0 is a follow-on. Design intent in [PENDING_QUESTIONS.md](../PENDING_QUESTIONS.md) Improvement 3 (sub-decisions 3a + 3b). See [`../library/V1_DEFERRED.md` §6](../library/V1_DEFERRED.md). For V1, push findings to Confluence via **CloudEvents webhooks** or **REST API**. |
| **ITSM** | ServiceNow | Create ServiceNow `incident` records from findings (optional `cmdb_ci` mapping under planning) | **Canonical scope:** **[V1 — committed]** ([`../library/V1_SCOPE.md`](../library/V1_SCOPE.md) §2.13). **V1 bridge (customer-operated, optional):** [`../../templates/integrations/servicenow/servicenow-incident-recipe.md`](../../templates/integrations/servicenow/servicenow-incident-recipe.md) — same event types, ServiceNow Table API, field mapping, Logic App outline. |
| **ITSM** | Azure DevOps Work Items | Create work items from findings; sync status back | [Planned] |
| **Chat-ops** | Slack | Outbound notification sink (parity with the shipped Microsoft Teams connector — same per-tenant `EnabledTriggersJson` opt-in matrix, secrets in Azure Key Vault) | **[V2 — planned]** — explicitly **out of scope for V1 and V1.1** (Resolved 2026-04-23). **Microsoft Teams** is the supported first-party chat-ops surface for V1 and V1.1 (see [`../integrations/MICROSOFT_TEAMS_NOTIFICATIONS.md`](../integrations/MICROSOFT_TEAMS_NOTIFICATIONS.md)). For V1 / V1.1, integrate via **CloudEvents webhooks** or **REST API** and bridge to Slack yourself. See [`../library/V1_SCOPE.md` §3](../library/V1_SCOPE.md) and [`../library/V1_DEFERRED.md` §6a](../library/V1_DEFERRED.md). |
| **Observability** | SIEM export (CEF/syslog) | Native audit log export in SIEM-friendly formats | [Planned] — see [SIEM_EXPORT.md](SIEM_EXPORT.md) for current methods |
| **CI/CD** | GitHub Actions | Architecture review as a PR check | [Example available] — see [../integrations/CICD_INTEGRATION.md](../integrations/CICD_INTEGRATION.md) |
| **CI/CD** | Azure DevOps Pipelines | Architecture review as a pipeline task | [Example available] — see [../integrations/CICD_INTEGRATION.md](../integrations/CICD_INTEGRATION.md) |
| **CI/CD** | Azure DevOps Repos (pipelines) | Same `GET /v1/compare` Markdown as GitHub Actions — job summary + sticky PR thread (`integrations/azure-devops-task-manifest-delta*`) | [Shipped] — see [../integrations/AZURE_DEVOPS_PIPELINE_TASK_MANIFEST_DELTA_PR_COMMENT.md](../integrations/AZURE_DEVOPS_PIPELINE_TASK_MANIFEST_DELTA_PR_COMMENT.md) · [ADR 0024](../adr/0024-azure-devops-pipeline-task-parity-with-github-action.md) |
| **CI/CD** | Azure DevOps Repos (Service Bus) | PR thread + status on manifest commit (`com.archlucid.authority.run.completed`) — **zero pipeline changes** | [Shipped] — opt-in Worker handler — see [../integrations/AZURE_DEVOPS_PR_DECORATION_SERVER_SIDE.md](../integrations/AZURE_DEVOPS_PR_DECORATION_SERVER_SIDE.md) |

---

## 3. Build your own

**End-to-end recipe hub (Azure DevOps PR decoration, CloudEvents consumer outline, Power Automate / Logic Apps):** see **[ITSM_BRIDGE_V1_RECIPES.md](../library/ITSM_BRIDGE_V1_RECIPES.md)** — consolidated walkthroughs with exact doc and repo paths. **First-party** **Jira** and **ServiceNow** are **V1 commitments** ([`V1_SCOPE.md` §2.13](../library/V1_SCOPE.md)); **Confluence** first-party remains **V1.1**. Recipes stay **optional** customer-operated bridges.

ArchLucid's architecture is designed for extensibility:

- **Context connectors:** Implement `IContextConnector` to bring new data sources into the analysis pipeline. See the finding engine template: `dotnet new archlucid-finding-engine`.
- **Outbound consumers:** Subscribe to CloudEvents webhooks or Service Bus topics to trigger workflows in your systems.
- **API automation:** Use the REST API or .NET client to build custom integrations.
- **ITSM (Jira / ServiceNow):** **V1** ships **first-party** connectors ([`V1_SCOPE.md` §2.13](../library/V1_SCOPE.md)). Until enabled for your tenant — or when you prefer Microsoft automation — use **customer-owned** recipes under [`docs/integrations/recipes/`](../../integrations/recipes/README.md): **Logic Apps–first:** [ServiceNow (Logic Apps)](../../integrations/recipes/SERVICENOW_INCIDENT_VIA_LOGIC_APPS.md), [Confluence (Logic Apps)](../../integrations/recipes/CONFLUENCE_PAGE_VIA_LOGIC_APPS.md) *(Confluence bridge remains relevant while Confluence first-party is V1.1)*; **Power Automate:** [ServiceNow](../../integrations/recipes/SERVICENOW_INCIDENT_VIA_POWER_AUTOMATE.md), [Jira](../../integrations/recipes/JIRA_ISSUE_VIA_POWER_AUTOMATE.md); **webhook bridge** templates: [ServiceNow](../../templates/integrations/servicenow/servicenow-incident-recipe.md), [Jira](../../templates/integrations/jira/jira-webhook-bridge-recipe.md). Starter **fixture→mapping parity** for bridge authors (Node built-in **`--test`**) lives under [`templates/integrations/bridge-recipe-contract-tests/`](../../templates/integrations/bridge-recipe-contract-tests/README.md), matching CI. Event types: [schemas/integration-events/catalog.json](../../schemas/integration-events/catalog.json) and [INTEGRATION_EVENTS_AND_WEBHOOKS.md](../library/INTEGRATION_EVENTS_AND_WEBHOOKS.md).

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
