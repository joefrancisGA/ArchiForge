> **Scope:** Index of **customer-owned** no-code webhook integration recipes: ArchLucid CloudEvents to third-party tools via Power Automate or Logic Apps **you** maintain — complements optional **or interim** coverage alongside **V1** first-party **Jira** / **ServiceNow** ([`V1_SCOPE.md`](../../library/V1_SCOPE.md) §2.13).

# Integration recipes (no-code webhook bridges)

**Audience:** Customers and integration engineers who prefer **customer-operated** automation (Microsoft Power Automate or Azure Logic Apps) for ITSM/documentation bridges — whether **before** first-party connectors are enabled, **instead of** them for operational preference, or for **Confluence** while first-party remains **V1.1**. **You** deploy, secure, and operate these flows.

**Customer-owned means:** These documents are **reference recipes only**. They are **not** marketplace listings, vendor-certified apps, or ArchLucid-managed integrations. ArchLucid publishes CloudEvents (or Service Bus messages); **your** tenant wires webhooks and calls third-party REST APIs under **your** contracts with Microsoft, Atlassian, and ServiceNow.

**Why recipes?** **First-party** **Jira** and **ServiceNow** are **V1 GA** commitments ([`V1_SCOPE.md`](../../library/V1_SCOPE.md) §2.13, [INTEGRATION_CATALOG.md](../../go-to-market/INTEGRATION_CATALOG.md)). **Confluence** first-party remains **V1.1** ([`V1_DEFERRED.md`](../../library/V1_DEFERRED.md) §6). These recipes use the same [event catalog](../../../schemas/integration-events/catalog.json) and [webhook / HMAC contracts](../../library/INTEGRATION_EVENTS_AND_WEBHOOKS.md) as any subscriber.

**Roadmap truth check:** Connector SKU status and “planned vs shipped” remain authoritative in [INTEGRATION_CATALOG.md](../../go-to-market/INTEGRATION_CATALOG.md).

---

## Recipes

| Recipe | Target tool | Automation platform | Event type(s) |
|--------|-------------|---------------------|----------------|
| [Azure Logic Apps → Azure DevOps work item](recipe-azure-logic-apps-webhook-to-ado-work-item.md) | Azure DevOps (Boards) | Azure Logic Apps + APIM/Function (HMAC) | `com.archlucid.authority.run.completed`, `com.archlucid.alert.fired` |
| [ServiceNow incident via Logic Apps](SERVICENOW_INCIDENT_VIA_LOGIC_APPS.md) | ServiceNow | Azure Logic Apps (Standard) | `com.archlucid.authority.run.completed`, `com.archlucid.alert.fired` |
| [Jira issue via Logic Apps](JIRA_ISSUE_VIA_LOGIC_APPS.md) | Atlassian Jira Cloud | Azure Logic Apps (Standard) | `com.archlucid.authority.run.completed`, `com.archlucid.alert.fired` |
| [Event Grid / webhook hardening checklist](recipe-event-grid-webhook-hardening-checklist.md) | *(subscriber hardening)* | Event Grid, APIM, Logic Apps, Functions | *(delivery semantics — see checklist)* |
| [ServiceNow incident via Power Automate](SERVICENOW_INCIDENT_VIA_POWER_AUTOMATE.md) | ServiceNow | Microsoft Power Automate | `com.archlucid.authority.run.completed`, `com.archlucid.alert.fired` |
| [Jira issue via Power Automate](JIRA_ISSUE_VIA_POWER_AUTOMATE.md) | Atlassian Jira Cloud | Microsoft Power Automate | `com.archlucid.authority.run.completed`, `com.archlucid.alert.fired` |
| [Confluence page via Logic Apps](CONFLUENCE_PAGE_VIA_LOGIC_APPS.md) | Atlassian Confluence Cloud | Azure Logic Apps (Standard) | `com.archlucid.authority.run.completed`, `com.archlucid.advisory.scan.completed` |

---

## Event catalog

All recipes subscribe to event types defined in [`IntegrationEventTypes.cs`](../../../ArchLucid.Core/Integration/IntegrationEventTypes.cs). For the full catalog, payload schemas, and delivery configuration:

| Resource | Path |
|----------|------|
| Event catalog (narrative) | [INTEGRATION_EVENT_CATALOG.md](../../library/INTEGRATION_EVENT_CATALOG.md) |
| **Webhooks, CloudEvents envelope, HMAC** | [INTEGRATION_EVENTS_AND_WEBHOOKS.md](../../library/INTEGRATION_EVENTS_AND_WEBHOOKS.md) |
| **Connector roadmap (first-party vs customer bridge)** | [INTEGRATION_CATALOG.md](../../go-to-market/INTEGRATION_CATALOG.md) |
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

## First-party roadmap vs these recipes

When you adopt **managed** ArchLucid connectors, prefer **[INTEGRATION_CATALOG.md](../../go-to-market/INTEGRATION_CATALOG.md)** §1 (**V1 committed — first-party ITSM**). These no-code flows remain valid **optional** paths:

- **Jira** + **ServiceNow** — **V1** first-party ships per [`V1_SCOPE.md`](../../library/V1_SCOPE.md) §2.13; recipes help pre-cutover tenants or hybrid topologies.
- **Confluence** — first-party **V1.1** ([`V1_DEFERRED.md`](../../library/V1_DEFERRED.md) §6); this folder’s Confluence recipe stays the primary Azure-first bridge until then.

See [INTEGRATION_CATALOG.md](../../go-to-market/INTEGRATION_CATALOG.md) for connector SKU status.

---

*Last reviewed: 2026-05-05 — Jira + ServiceNow first-party promoted to V1 scope; Confluence remains V1.1; recipes reframed as optional bridges.*
