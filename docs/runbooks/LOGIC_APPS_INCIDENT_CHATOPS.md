> **Scope:** Runbook — Incident ChatOps (Logic Apps + Service Bus) - full detail, tables, and links in the sections below.

# Runbook — Incident ChatOps (Logic Apps + Service Bus)

**Priority:** P3 — Reference  
**Last reviewed:** 2026-04-19

## 1. Objective

Give operators **Teams / PagerDuty** visibility into **`com.archlucid.alert.fired`** and **`com.archlucid.alert.resolved`** without moving alert evaluation or persistence out of ArchLucid. Logic Apps **subscribe** to the integration topic, render adaptive cards, and **call back** into existing **`POST /v1/alerts/...`** routes so lifecycle and audit semantics stay in **`AlertService`**.

## 2. Assumptions

- `infra/terraform-servicebus/` is applied with **`enable_logic_app_incident_chatops_subscription`** when this workflow is active (subscription SQL: fired **OR** resolved — see module `main.tf`).
- The API publishes alert integration events with user properties derived in **`IntegrationEventServiceBusApplicationProperties`** ( **`severity`**, **`deduplication_key`** on fired; **`deduplication_key`** on resolved when the JSON payload includes `deduplicationKey`).
- Callbacks use **Entra-protected** HTTP (managed identity or OAuth connector) to a reachable **`ArchLucid.Api`** base URL (often via **APIM**).

## 3. Constraints

- **Do not** have the Logic App emit new **`com.archlucid.alert.fired`** events (no synthetic “button clicked” alerts); only **HTTP actions** into ArchLucid.
- **Scope:** `POST /v1/alerts/{id}/action` and **`POST /v1/alerts/acknowledge-batch`** enforce **tenant / workspace / project** from **`IScopeContextProvider`** — the connector identity must map to a principal that can assume the correct **scope headers** (same as human operators) or you use a dedicated automation account documented in your landing zone.
- **SQL filters** reference **user properties** (body is not filterable on subscriptions). Fired messages include **`severity`** (lowercased) and **`deduplication_key`** when present in the JSON body.

## 4. Architecture Overview

**Nodes:** Service Bus topic → filtered subscription → Logic App (Standard) workflow → HTTPS → ArchLucid.Api alerts controller.  
**Edges:** JSON integration event payloads (schemas under `schemas/integration-events/`) + optional user properties for routing.  
**Boundaries:** ArchLucid remains **system of record** for `AlertRecord` rows; Logic Apps are **presentation + transport** only.

## 5. Component Breakdown

| Component | Role |
|-----------|------|
| **`AlertIntegrationEventPublishing`** | Builds fired/resolved JSON payloads; enqueue or direct publish via outbox helper. |
| **`IntegrationEventServiceBusApplicationProperties`** | Adds **`severity`** / **`deduplication_key`** user properties for ChatOps subscription rules and Teams correlation. |
| **`AlertsController`** | **`POST v1/alerts/{alertId}/action`** (acknowledge / resolve / suppress), **`POST v1/alerts/acknowledge-batch`** (partial success per id). |
| **Terraform `infra/terraform-servicebus`** | Optional subscription + **`$Default`** SQL filter on **`event_type`**; optional **Data Receiver** role for the Logic App managed identity. |

## 6. Data Flow

1. Alert row persisted → **`AlertIntegrationEventPublishing.TryPublishFiredAsync`** → Service Bus message with `event_type`, JSON body (`alertId`, `deduplicationKey`, `severity`, …), user properties **`severity`**, **`deduplication_key`**.
2. Logic App **When a message is received** (peek-lock) → parse JSON → branch on **`severity`** for routing if desired (or use separate subscription rules later).
3. Operator **Acknowledge** → HTTP **POST** `…/v1/alerts/acknowledge-batch` with `{ "alertIds": ["{alertId}"] }` **or** `…/v1/alerts/{alertId}/action` with `{ "action": "Acknowledge" }`.
4. Resolve path → **`com.archlucid.alert.resolved`** message includes optional **`deduplicationKey`** in JSON and **`deduplication_key`** user property → Logic App updates Teams card using the same key as ADR **0008** dedupe scope.

## 7. Security Model

- **Least privilege:** Logic App MI = **Data Receiver** on Service Bus; separate API role = **ExecuteAuthority** (or tighter automation role if you add one later).
- **Network:** Prefer **private endpoints** and APIM policies so automation callbacks are not exposed on the public internet without controls.
- **Reliability:** Use **idempotent** acknowledge (safe retries); batch API returns per-id success/failure — treat non-200 as connector retry with backoff.

## 8. Operational Considerations

- **Dead letters:** Failed HTTP callbacks → fix Entra scope / URL → replay from DLQ after verifying payload against **`alert-fired.v1.schema.json`** / **`alert-resolved.v1.schema.json`**.
- **Correlation:** Store **`deduplication_key`** (message property) next to Teams **`activityId`** in workflow run state or Logic App **variables** if the connector supports update-message flows.
- **Cost:** One subscription for both fired and resolved keeps SB filter simple; split subscriptions if you need independent DLQ tuning.

## Related

- `infra/terraform-logicapps/workflows/incident-chatops/README.md`
- `docs/adr/0008-alert-dedupe-scopes.md`
- `docs/INTEGRATION_EVENTS_AND_WEBHOOKS.md`
- `docs/runbooks/LOGIC_APPS_STANDARD.md`
