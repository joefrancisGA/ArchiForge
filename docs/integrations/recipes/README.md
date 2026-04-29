> **Scope:** Index of no-code webhook integration recipes for V1 customers bridging ArchLucid CloudEvents to third-party tools via Power Automate or Logic Apps.

# Integration recipes (no-code webhook bridges)

**Audience:** V1 customers and integration engineers who need to connect ArchLucid events to ITSM or documentation tools **without writing custom code**, using Microsoft Power Automate or Azure Logic Apps.

**Why recipes?** First-party connectors for Jira, Confluence, and ServiceNow are [planned for V1.1](../../library/V1_DEFERRED.md) (§6). Until then, these recipes provide step-by-step, no-code flows that bridge ArchLucid CloudEvents webhooks to the target tool's API.

---

## Recipes

| Recipe | Target tool | Automation platform | Event type(s) |
|--------|-------------|---------------------|----------------|
| [Jira issue via Power Automate](JIRA_ISSUE_VIA_POWER_AUTOMATE.md) | Atlassian Jira Cloud | Microsoft Power Automate | `com.archlucid.authority.run.completed`, `com.archlucid.alert.fired` |
| [Confluence page via Logic Apps](CONFLUENCE_PAGE_VIA_LOGIC_APPS.md) | Atlassian Confluence Cloud | Azure Logic Apps (Standard) | `com.archlucid.authority.run.completed`, `com.archlucid.advisory.scan.completed` |
| [ServiceNow incident via Power Automate](SERVICENOW_INCIDENT_VIA_POWER_AUTOMATE.md) | ServiceNow | Microsoft Power Automate | `com.archlucid.authority.run.completed`, `com.archlucid.alert.fired` |

---

## Event catalog

All recipes subscribe to event types defined in [`IntegrationEventTypes.cs`](../../../ArchLucid.Core/Integration/IntegrationEventTypes.cs). For the full catalog, payload schemas, and delivery configuration:

| Resource | Path |
|----------|------|
| Event catalog (narrative) | [INTEGRATION_EVENT_CATALOG.md](../../library/INTEGRATION_EVENT_CATALOG.md) |
| Webhooks and CloudEvents delivery | [INTEGRATION_EVENTS_AND_WEBHOOKS.md](../../library/INTEGRATION_EVENTS_AND_WEBHOOKS.md) |
| Machine-readable schema catalog | [`schemas/integration-events/catalog.json`](../../../schemas/integration-events/catalog.json) |
| AsyncAPI contract | [`docs/contracts/archlucid-asyncapi-2.6.yaml`](../../contracts/archlucid-asyncapi-2.6.yaml) |

---

## Relationship to existing bridge templates

The [`templates/integrations/`](../../../templates/integrations/) folder contains **developer-oriented** bridge recipes (custom code, Azure Functions, HMAC verification) for Jira and ServiceNow. The recipes in **this** folder are the **no-code** equivalents — they use the same CloudEvents payloads but wire everything through Power Automate or Logic Apps designers.

| Audience | Folder |
|----------|--------|
| Developer writing custom bridge code | [`templates/integrations/jira/`](../../../templates/integrations/jira/jira-webhook-bridge-recipe.md) · **[`../JIRA_WEBHOOK_BRIDGE.md`](../JIRA_WEBHOOK_BRIDGE.md)** + [`scripts/integrations/jira/`](../../../scripts/integrations/jira/), [`templates/integrations/servicenow/`](../../../templates/integrations/servicenow/servicenow-incident-recipe.md) |
| Operator using no-code automation | `docs/integrations/recipes/` (this folder) |

---

## V1.1 first-party connectors

These recipes are **interim bridges**. V1.1 will ship first-party connectors that replace them:

- **Jira** — one-way finding → issue with correlation back-link; two-way status sync within V1.1 window.
- **Confluence** — one-way publish to a fixed `Confluence:DefaultSpaceKey`.
- **ServiceNow** — one-way finding → `incident` with correlation back-link.

See [V1_DEFERRED.md §6](../../library/V1_DEFERRED.md) for the full V1.1 commitment table and [INTEGRATION_CATALOG.md §2](../../go-to-market/INTEGRATION_CATALOG.md) for connector status.

---

*Last reviewed: 2026-04-26 — event types from [IntegrationEventTypes.cs](../../../ArchLucid.Core/Integration/IntegrationEventTypes.cs).*
