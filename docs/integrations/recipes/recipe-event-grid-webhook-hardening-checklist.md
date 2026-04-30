> **Scope:** Checklist for hardening HTTPS endpoints that receive integration traffic — Azure Event Grid subscription validation, optional ArchLucid HMAC, and idempotent processing; no ArchLucid product changes.

# Event Grid & webhook receiver — hardening checklist

**Audience:** Security and platform engineers placing **Azure Event Grid**, **API Management**, **Logic Apps**, or **Functions** between enterprise networking zones and automation that ultimately consumes **ArchLucid-compatible CloudEvents** (directly from ArchLucid or after fan-out).

**Not ArchLucid configuration.** This document describes **subscriber-side** controls only. It does not add routes or connectors to ArchLucid.Api.

**Contracts:** [INTEGRATION_EVENTS_AND_WEBHOOKS.md](../../library/INTEGRATION_EVENTS_AND_WEBHOOKS.md) · [`schemas/integration-events/catalog.json`](../../../schemas/integration-events/catalog.json)

---

## V1 scope boundary (ITSM / connectors)

[V1_SCOPE.md §3](../../library/V1_SCOPE.md) lists **Jira** and **ServiceNow** first-party connectors as **out of scope for V1** (V1.1 candidates). V1 customers integrate via **webhooks**, **Service Bus**, and **REST**. Event Grid is **your** enterprise fan-out layer — using it does **not** imply ArchLucid ships Jira/ServiceNow connectors in V1.

---

## 1. Topology — which checks apply?

| Topology | Inbound trust | Primary checklist sections |
|----------|---------------|----------------------------|
| **A — ArchLucid → your HTTPS URL** | ArchLucid tenant config | §3 HMAC, §5 TLS/network, §6 Idempotency |
| **B — ArchLucid → APIM/Function → Logic Apps** | Same as A | §3–§6; verify **raw body** at edge before transforms |
| **C — Custom Event Grid topic → push to subscriber URL** | Event Grid | §2 Subscription validation, §4 Event Grid headers, §6 |
| **D — Service Bus → Event Grid / Functions** (enterprise relay) | Azure AD / RBAC on SB | §6 message `MessageId` duplicate detection + §3 if ArchLucid path still signs webhooks upstream |

Mixing **C** with **A**: if Event Grid **republishes** the same JSON ArchLucid signed, HMAC may break when intermediaries re-serialize JSON — prefer verifying HMAC **before** Event Grid or verify at **first hop** only.

---

## 2. Event Grid subscription validation

When you create an Event Grid subscription pointing at your HTTPS endpoint, Azure sends a **`SubscriptionValidation`** event first.

| Step | Action |
|------|--------|
| Detect | HTTP header **`aeg-event-type`** = `SubscriptionValidation` (or parse array envelope per [Event Grid webhook delivery](https://learn.microsoft.com/en-us/azure/event-grid/webhook-event-delivery)). |
| Respond | Return **200** with JSON body **`{ "validationResponse": "<validationCode>" }`** using the **`validationCode`** from the event payload. |
| Timeout | Validation expires quickly — automate deployment pipelines to complete handshake without manual delay. |

Skipping this causes **permanent subscription failure** unrelated to ArchLucid health.

---

## 3. ArchLucid HMAC (when ArchLucid POSTs directly)

Align with [INTEGRATION_EVENTS_AND_WEBHOOKS.md](../../library/INTEGRATION_EVENTS_AND_WEBHOOKS.md) and [ALERTS.md § Outbound webhook HMAC](../../library/ALERTS.md):

| Check | Detail |
|-------|--------|
| Secret storage | **Key Vault** reference — never plaintext in Logic App designer defaults for production. |
| Signed payload | **Full** POST body bytes after CloudEvents wrapping (`WebhookDelivery:UseCloudEventsEnvelope` = **true**). |
| Header | **`X-ArchLucid-Webhook-Signature`** → **`sha256=`** + lowercase **HMAC-SHA256**. |
| Comparison | **Constant-time** compare; reject missing header when secret is configured server-side. |

If traffic passes through a component that alters whitespace or field order, **recompute on the exact bytes received at the verifier**.

---

## 4. Event Grid delivery authentication (subscriber-facing)

| Control | Use case |
|---------|----------|
| **System topics / partner topics** | Prefer **managed identity** delivery or **event subscription** filters limited to required subject prefixes. |
| **Webhook secrets** | Where Event Grid supports query-string **validation secrets**, treat as passwords — rotate with IaC. |
| **IP restrictions** | Restrict Logic App / Function ingress to Event Grid egress ranges **only** when ops can maintain allowlists (often paired with APIM or regional restrictions). |

Event Grid does **not** replace ArchLucid HMAC for topology **A**; it adds **its own** delivery semantics.

---

## 5. TLS, quotas, and abuse

| Check | Action |
|-------|--------|
| TLS | HTTPS only; TLS 1.2+ at ingress (APIM / App Gateway / Function). |
| Payload size | Oversized bodies → **413** + logging; protect downstream Logic App statement limits. |
| Rate limits | APIM **rate-limit / quota** policies per caller identity or subnet. |
| Logging | Redact **`X-Api-Key`**, PATs, and webhook secrets from Application Insights traces. |

---

## 6. Idempotency guidance

| Source | Mechanism |
|--------|-----------|
| **CloudEvents** | Persist processed **`id`** (unique per event instance). |
| **Event Grid** | Delivery attempts may retry — use **`event.id`** from Event Grid schema where present in addition to CloudEvents `id` inside `data`. |
| **Service Bus** | Enable **duplicate detection** on queue/topic (`MessageId`) — see [INTEGRATION_EVENTS_AND_WEBHOOKS.md § Azure Service Bus](../../library/INTEGRATION_EVENTS_AND_WEBHOOKS.md). |
| **Downstream ADO / ITSM** | Use **`deduplicationKey`** from [`alert-fired.v1.schema.json`](../../../schemas/integration-events/alert-fired.v1.schema.json) when alerts drive tickets. |

---

## 7. Example payload pointers (`schemas/integration-events/`)

Use [`catalog.json`](../../../schemas/integration-events/catalog.json) as the authoritative list. Typical webhook automation entry points:

| Schema file | Event type |
|-------------|------------|
| [`authority-run-completed.v1.schema.json`](../../../schemas/integration-events/authority-run-completed.v1.schema.json) | `com.archlucid.authority.run.completed` |
| [`alert-fired.v1.schema.json`](../../../schemas/integration-events/alert-fired.v1.schema.json) | `com.archlucid.alert.fired` |
| [`manifest-finalized.v1.schema.json`](../../../schemas/integration-events/manifest-finalized.v1.schema.json) | `com.archlucid.manifest.finalized.v1` |

Validate **after** unwrap from Event Grid array envelope (`[].data` vs full CloudEvents depending on publisher configuration).

---

## 8. Failure modes

| Failure | Cause | Response pattern |
|---------|-------|------------------|
| **401 from verifier** | Wrong secret or body mutation | Fix hop ordering; verify raw body capture in Functions. |
| **Validation never completes** | Blocked response or wrong JSON shape | Automated health check on subscription state in Azure portal. |
| **Silent duplicate work items** | Missing idempotency store | Add persistence layer for CloudEvents `id`. |
| **DLQ growth** | Poison schema | Route unknown `type` to dead-letter after N tries; alert ops. |

---

## Related

| Doc | Use |
|-----|-----|
| [recipe-azure-logic-apps-webhook-to-ado-work-item.md](recipe-azure-logic-apps-webhook-to-ado-work-item.md) | End-to-end ADO example |
| [CONFLUENCE_PAGE_VIA_LOGIC_APPS.md](CONFLUENCE_PAGE_VIA_LOGIC_APPS.md) | Logic Apps + HMAC edge pattern |

---

*Last reviewed: 2026-04-29 — subscriber-side checklist only; ArchLucid delivery semantics unchanged.*
