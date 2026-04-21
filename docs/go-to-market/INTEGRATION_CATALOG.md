> **Scope:** ArchLucid — Integration catalog - full detail, tables, and links in the sections below.

# ArchLucid — Integration catalog

**Audience:** Technical evaluators and integration engineers assessing how ArchLucid connects to their ecosystem.

**Last reviewed:** 2026-04-15

**Philosophy:** ArchLucid connects to your tools — you do not run our agents in your infrastructure. All integrations operate via the hosted API, webhooks, or managed connectors.

---

## 1. Available today (V1)

| Integration | Type | Description |
|-------------|------|-------------|
| **REST API** | Outbound / Inbound | OpenAPI 3.0 contract (`/openapi/v1.json`). Full CRUD for runs, manifests, findings, governance, audit, comparisons, alerts. See [../API_CONTRACTS.md](../API_CONTRACTS.md). |
| **.NET API client** | Client SDK | Generated NuGet package (`ArchLucid.Api.Client`) from NSwag / OpenAPI spec. |
| **CLI** | Command-line | `archlucid` CLI for scripting, support bundles, and automation. See [../CLI_USAGE.md](../CLI_USAGE.md). |
| **Webhook / CloudEvents** | Outbound | Configurable HTTP callbacks on run lifecycle, governance, and alert events. CloudEvents envelope format. |
| **Service Bus** | Outbound | Optional Azure Service Bus integration events for async processing and downstream systems. |
| **AsyncAPI** | Contract | Async event contract for webhook and Service Bus consumers. |

### Authentication for integrations

| Method | Use case | Reference |
|--------|----------|-----------|
| **Entra ID (JWT)** | Production integrations, CI/CD pipelines with service principals | [../SECURITY.md](../SECURITY.md), [TENANT_ISOLATION.md](TENANT_ISOLATION.md) |
| **API keys** | Automation, scripts, lightweight integrations | [../SECURITY.md](../SECURITY.md) (RBAC, key rotation) |

---

## 2. Planned connectors [Roadmap]

| Category | Connector | Description | Status |
|----------|-----------|-------------|--------|
| **Identity** | SCIM provisioning | Sync users and groups from Okta, Entra ID, or other IdPs | [Planned] |
| **Architecture import** | Structurizr DSL | Import architecture models from Structurizr workspace files | [Planned] |
| **Architecture import** | ArchiMate XML | Import from TOGAF / ArchiMate modeling tools | [Planned] |
| **Architecture import** | Terraform state | Parse `terraform show -json` output into ArchLucid context | [Planned] |
| **ITSM** | Jira | Create Jira issues from findings; sync status back | [Planned] |
| **ITSM** | Azure DevOps Work Items | Create work items from findings; sync status back | [Planned] |
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

---

## 4. Request an integration

Contact **integrations@archlucid.dev** (placeholder) with your use case. Integration requests inform the connector roadmap.

---

## Related documents

| Doc | Use |
|-----|-----|
| [TRUST_CENTER.md](TRUST_CENTER.md) | Trust index |
| [POSITIONING.md](POSITIONING.md) | Product positioning |
| [../API_CONTRACTS.md](../API_CONTRACTS.md) | API surface detail |
| [SIEM_EXPORT.md](SIEM_EXPORT.md) | Audit export for SIEM |
| [../integrations/CICD_INTEGRATION.md](../integrations/CICD_INTEGRATION.md) | CI/CD pipeline examples |
