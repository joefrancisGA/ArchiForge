> **Scope:** Copy-paste Azure integration recipe — Logic Apps (and supporting Azure components) consuming ArchLucid CloudEvents webhooks to create Azure DevOps work items; customer-operated bridge only.

# Azure Logic Apps: webhook → Azure DevOps work item

**Audience:** V1 platform engineers who want **work items in Azure DevOps** from the same CloudEvents payloads ArchLucid posts to HTTPS subscribers, without waiting for a first-party ArchLucid connector.

**Not a product connector.** This recipe wires together **your** Logic App, **your** Azure DevOps organization/project, and optional **API Management** or **Azure Functions** for signature verification. It does **not** change ArchLucid.Api routing or imply a shipped ArchLucid “Azure DevOps connector.” The connector row for **Azure DevOps Work Items** in [INTEGRATION_CATALOG.md §2](../../go-to-market/INTEGRATION_CATALOG.md) remains **[Planned]** for first-party scope.

**Contracts:** [`schemas/integration-events/catalog.json`](../../../schemas/integration-events/catalog.json) · [INTEGRATION_EVENTS_AND_WEBHOOKS.md](../../library/INTEGRATION_EVENTS_AND_WEBHOOKS.md) · [ALERTS.md](../../library/ALERTS.md) (HMAC header semantics)

**Event catalog (code):** [`IntegrationEventTypes.cs`](../../../ArchLucid.Core/Integration/IntegrationEventTypes.cs)

---

## V1 scope boundary (ITSM / connectors)

Per [V1_SCOPE.md §3 — Out of scope for V1](../../library/V1_SCOPE.md), **first-party Jira and ServiceNow ITSM bridges are V1.1 candidates**, not V1. V1 supported paths are **CloudEvents webhooks**, **REST API**, and **customer-operated automation** (including this recipe). Creating Azure DevOps work items here is an **ADO-side** integration you operate; it does not contradict V1 because ArchLucid does not promise Jira/ServiceNow connectors in V1.

---

## 1. Objective

On each subscribed CloudEvents delivery from ArchLucid:

1. Verify authenticity (shared secret + HMAC over the **raw** POST body).
2. Branch on CloudEvents `type`.
3. Create or update an Azure DevOps **Work Item** (REST `PATCH` `application/json-patch+json`) using metadata from `data` and, when needed, a follow-on **`GET /v1/authority/runs/{runId}`** for finding detail.

---

## 2. Authentication choices

| Layer | Option | Notes |
|-------|--------|--------|
| **Inbound from ArchLucid** | **HMAC-SHA256** (`WebhookDelivery:HmacSha256SharedSecret`) | ArchLucid signs the **exact UTF-8 JSON body** (CloudEvents envelope included when `WebhookDelivery:UseCloudEventsEnvelope` is **true**). Header: **`X-ArchLucid-Webhook-Signature`** with value **`sha256=`** + lowercase hex — see [INTEGRATION_EVENTS_AND_WEBHOOKS.md](../../library/INTEGRATION_EVENTS_AND_WEBHOOKS.md) and [ALERTS.md § Outbound webhook HMAC](../../library/ALERTS.md). Compare with **constant-time** equality over raw bytes. |
| **Inbound from ArchLucid** | **Private URL only** (no HMAC) | Possible when `HmacSha256SharedSecret` is unset — weaker; combine with **API Management** IP restriction, **Private Link**, or **Front Door + WAF** where feasible. |
| **Inbound verification placement** | **Azure Functions** or **API Management policy** | Logic Apps “When a HTTP request is received” does not replace a dedicated HMAC verifier over **unchanged raw body**; mirror [CONFLUENCE_PAGE_VIA_LOGIC_APPS.md § Step 2 — Validate HMAC](CONFLUENCE_PAGE_VIA_LOGIC_APPS.md). |
| **Outbound to Azure DevOps** | **PAT** (scoped **Work Items (read & write)**) stored in **Key Vault** | Invoke `PATCH https://dev.azure.com/{org}/{project}/_apis/wit/workitems/$Bug?api-version=7.1` (or `$Task`) per [Azure DevOps REST — Work Items](https://learn.microsoft.com/en-us/rest/api/azure/devops/wit/work-items/create). |
| **Outbound to Azure DevOps** | **OAuth / Managed identity via federated credential** | Prefer when your enterprise forbids long-lived PATs — still **your** identity connecting to ADO, not ArchLucid’s. |
| **Outbound to ArchLucid API** | **API key** (`X-Api-Key`) or **Entra ID JWT** | Needed only for **`com.archlucid.authority.run.completed`** when enriching from run JSON (`GET /v1/authority/runs/{runId}`). |

---

## 3. Example payload pointers (`schemas/integration-events/`)

| Event type (CloudEvents `type`) | JSON Schema file | Typical ADO action |
|--------------------------------|------------------|--------------------|
| `com.archlucid.authority.run.completed` | [`authority-run-completed.v1.schema.json`](../../../schemas/integration-events/authority-run-completed.v1.schema.json) | One work item per finding (cap N) after **GET run**, or one rollup item using only envelope fields. |
| `com.archlucid.alert.fired` | [`alert-fired.v1.schema.json`](../../../schemas/integration-events/alert-fired.v1.schema.json) | Single work item from `data.title`, `severity`, `deduplicationKey`. |

Full index: [`catalog.json`](../../../schemas/integration-events/catalog.json). Sample envelopes for local replay: [`scripts/integrations/jira/sample-alert-fired.json`](../../../scripts/integrations/jira/sample-alert-fired.json), [`sample-authority-run-completed.json`](../../../scripts/integrations/jira/sample-authority-run-completed.json) (same CloudEvents shape as other bridges).

---

## 4. Idempotency guidance

| Signal | Suggested key | Behavior |
|--------|----------------|----------|
| CloudEvents | **`id`** | Treat **`id`** as a natural deduplication key for “exactly this delivery”; persist processed IDs (Cosmos / SQL / Logic Apps **Stateful** external storage) for **at-least-once** webhook retries. |
| Alerts | **`data.deduplicationKey`** | When present, upsert a single ADO work item keyed by custom field or tag (`ArchLucidDedupKey`) instead of creating duplicates. |
| Runs | **`runId` + `manifestId`** (from `data`) | Avoid duplicate “rollup” items per commit using a composite external key. |
| ADO create | **API response `id`** | On transient failure after HTTP 200 from ArchLucid, query ADO by custom link field pointing to ArchLucid `runId` before creating. |

ArchLucid may retry failed webhook deliveries; **assume duplicates**.

---

## 5. Flow overview

```text
ArchLucid HTTPS POST (CloudEvents JSON + optional X-ArchLucid-Webhook-Signature)
  │
  ├─ APIM or Azure Function: HMAC verify (raw body)
  │
  └─ Logic App HTTP workflow
        ├─ Parse JSON → Switch on "type"
        │
        ├─ authority.run.completed → GET ArchLucid run → For each finding → PATCH ADO work item create
        │
        ├─ alert.fired → PATCH ADO work item create (single)
        │
        └─ Default → 202 Accepted (log + skip)
```

Use the same CloudEvents JSON Schema bootstrap as [JIRA_ISSUE_VIA_POWER_AUTOMATE.md § Step 1](JIRA_ISSUE_VIA_POWER_AUTOMATE.md) for the Logic Apps **Parse JSON** shape.

---

## 6. Failure modes

| Failure | Symptom | Mitigation |
|---------|---------|------------|
| **HMAC mismatch** | Receiver rejects valid traffic | Secret drift across ArchLucid vs verifier — rotate via [SECRET_AND_CERT_ROTATION.md](../../runbooks/SECRET_AND_CERT_ROTATION.md) coordination; verify **full envelope** bytes. |
| **Malformed JSON / schema drift** | Parser throws | `additionalProperties` is permissive in schemas — still guard missing required fields; dead-letter unhandled shapes. |
| **ADO 401 / 403** | PAT expired or wrong scope | Short-lived PAT rotation; least-privilege project scope. |
| **ADO rate limiting** | 429 responses | Backoff; cap findings-per-run like Jira bridge (`MAX_FINDINGS_PER_RUN` pattern). |
| **ArchLucid GET timeout** | Empty finding list | Retry with idempotent GET; do not create empty placeholder items without ops approval. |
| **Duplicate deliveries** | Multiple identical ADO items | Apply §4 idempotency keys and ADO query-before-create. |

Smoke inbound deliveries with **`archlucid webhooks test`** when available — see [INTEGRATION_EVENTS_AND_WEBHOOKS.md](../../library/INTEGRATION_EVENTS_AND_WEBHOOKS.md).

---

## 7. Related recipes

| Doc | Use |
|-----|-----|
| [CONFLUENCE_PAGE_VIA_LOGIC_APPS.md](CONFLUENCE_PAGE_VIA_LOGIC_APPS.md) | Logic Apps + HMAC placement pattern |
| [JIRA_ISSUE_VIA_POWER_AUTOMATE.md](JIRA_ISSUE_VIA_POWER_AUTOMATE.md) | CloudEvents schema for triggers |
| [../JIRA_WEBHOOK_BRIDGE.md](../JIRA_WEBHOOK_BRIDGE.md) | Developer-oriented HMAC + replay |

---

*Last reviewed: 2026-04-29 — aligned with [INTEGRATION_EVENTS_AND_WEBHOOKS.md](../../library/INTEGRATION_EVENTS_AND_WEBHOOKS.md) CloudEvents + HMAC.*
