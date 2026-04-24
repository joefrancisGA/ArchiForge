> **Scope:** ArchLucid — Integration catalog - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# ArchLucid — Integration catalog

**Audience:** Technical evaluators and integration engineers assessing how ArchLucid connects to their ecosystem.

**Last reviewed:** 2026-04-24

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

### Authentication for integrations

| Method | Use case | Reference |
|--------|----------|-----------|
| **Entra ID (JWT)** | Production integrations, CI/CD pipelines with service principals | [../SECURITY.md](../library/SECURITY.md), [TENANT_ISOLATION.md](TENANT_ISOLATION.md) |
| **API keys** | Automation, scripts, lightweight integrations | [../SECURITY.md](../library/SECURITY.md) (RBAC, key rotation) |

---

## 2. Planned connectors [Roadmap]

| Category | Connector | Description | Status |
|----------|-----------|-------------|--------|
| **Identity** | SCIM provisioning | Sync users and groups from Okta, Entra ID, or other IdPs | [Planned] |
| **Architecture import** | Structurizr DSL | Import architecture models from Structurizr workspace files | [Planned] |
| **Architecture import** | ArchiMate XML | Import from TOGAF / ArchiMate modeling tools | [Planned] |
| **Architecture import** | Terraform state | Parse `terraform show -json` output into ArchLucid context | [Planned] |
| **ITSM** | Jira | Create Jira issues from findings; sync status back | **[V1.1 — planned]** — explicitly **out of scope for V1**, **in scope for V1.1** (Resolved 2026-04-23). See [`../library/V1_SCOPE.md` §3](../library/V1_SCOPE.md) and [`../library/V1_DEFERRED.md` §6](../library/V1_DEFERRED.md). For V1, integrate via **CloudEvents webhooks** or **REST API**. **V1 bridge (customer-operated):** [`../../templates/integrations/jira/jira-webhook-bridge-recipe.md`](../../templates/integrations/jira/jira-webhook-bridge-recipe.md) — HMAC, CloudEvents (`com.archlucid.authority.run.completed` + GET run for findings, or `com.archlucid.alert.fired` direct), Jira REST v3, Logic App / Function outline. |
| **ITSM** | ServiceNow | Create ServiceNow `incident` records from findings (optional `cmdb_ci` mapping under V1.1 planning) | **[V1.1 — planned]** — explicitly **out of scope for V1**, **in scope for V1.1** (Resolved 2026-04-23). One-way (finding → `incident`) is committed; whether `cmdb_ci` mapping ships in the same V1.1 release or as a fast-follow is an open V1.1-planning question. Two-way status sync is **not** committed for V1.1. See [`../library/V1_SCOPE.md` §3](../library/V1_SCOPE.md) and [`../library/V1_DEFERRED.md` §6](../library/V1_DEFERRED.md). For V1, integrate via **CloudEvents webhooks** or **REST API**. **V1 bridge (customer-operated):** [`../../templates/integrations/servicenow/servicenow-incident-recipe.md`](../../templates/integrations/servicenow/servicenow-incident-recipe.md) — same event types, ServiceNow Table API, field mapping, Logic App outline. |
| **ITSM** | Azure DevOps Work Items | Create work items from findings; sync status back | [Planned] |
| **Chat-ops** | Slack | Outbound notification sink (parity with the shipped Microsoft Teams connector — same per-tenant `EnabledTriggersJson` opt-in matrix, secrets in Azure Key Vault) | **[V2 — planned]** — explicitly **out of scope for V1 and V1.1** (Resolved 2026-04-23). **Microsoft Teams** is the supported first-party chat-ops surface for V1 and V1.1 (see [`../integrations/MICROSOFT_TEAMS_NOTIFICATIONS.md`](../integrations/MICROSOFT_TEAMS_NOTIFICATIONS.md)). For V1 / V1.1, integrate via **CloudEvents webhooks** or **REST API** and bridge to Slack yourself. See [`../library/V1_SCOPE.md` §3](../library/V1_SCOPE.md) and [`../library/V1_DEFERRED.md` §6a](../library/V1_DEFERRED.md). |
| **Observability** | SIEM export (CEF/syslog) | Native audit log export in SIEM-friendly formats | [Planned] — see [SIEM_EXPORT.md](SIEM_EXPORT.md) for current methods |
| **CI/CD** | GitHub Actions | Architecture review as a PR check | [Example available] — see [../integrations/CICD_INTEGRATION.md](../integrations/CICD_INTEGRATION.md) |
| **CI/CD** | Azure DevOps Pipelines | Architecture review as a pipeline task | [Example available] — see [../integrations/CICD_INTEGRATION.md](../integrations/CICD_INTEGRATION.md) |
| **CI/CD** | Azure DevOps Repos (pipelines) | Same `GET /v1/compare` Markdown as GitHub Actions — job summary + sticky PR thread (`integrations/azure-devops-task-manifest-delta*`) | [Shipped] — see [../integrations/AZURE_DEVOPS_PIPELINE_TASK_MANIFEST_DELTA_PR_COMMENT.md](../integrations/AZURE_DEVOPS_PIPELINE_TASK_MANIFEST_DELTA_PR_COMMENT.md) · [ADR 0024](../adr/0024-azure-devops-pipeline-task-parity-with-github-action.md) |
| **CI/CD** | Azure DevOps Repos (Service Bus) | PR thread + status on manifest commit (`com.archlucid.authority.run.completed`) — **zero pipeline changes** | [Shipped] — opt-in Worker handler — see [../integrations/AZURE_DEVOPS_PR_DECORATION_SERVER_SIDE.md](../integrations/AZURE_DEVOPS_PR_DECORATION_SERVER_SIDE.md) |

---

## 3. Build your own

ArchLucid's architecture is designed for extensibility:

- **Context connectors:** Implement `IContextConnector` to bring new data sources into the analysis pipeline. See the finding engine template: `dotnet new archlucid-finding-engine`.
- **Outbound consumers:** Subscribe to CloudEvents webhooks or Service Bus topics to trigger workflows in your systems.
- **API automation:** Use the REST API or .NET client to build custom integrations.
- **ITSM (Jira / ServiceNow) V1:** Until first-party connectors ship (V1.1), use the **webhook bridge** recipes: [Jira — `jira-webhook-bridge-recipe.md`](../../templates/integrations/jira/jira-webhook-bridge-recipe.md), [ServiceNow — `servicenow-incident-recipe.md`](../../templates/integrations/servicenow/servicenow-incident-recipe.md). Both reference canonical event types from [schemas/integration-events/catalog.json](../../schemas/integration-events/catalog.json) and [INTEGRATION_EVENTS_AND_WEBHOOKS.md](../library/INTEGRATION_EVENTS_AND_WEBHOOKS.md).

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
