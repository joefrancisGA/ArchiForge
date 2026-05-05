> **Scope:** Integration events and webhook interoperability - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Integration events and webhook interoperability

## CloudEvents on HTTP webhooks

When `WebhookDelivery:UseCloudEventsEnvelope` is **true**, digest and alert webhook POST bodies are wrapped in a [CloudEvents 1.0](https://github.com/cloudevents/spec/blob/v1.0.2/cloudevents/formats/json-format.md) JSON envelope (`specversion`, `type`, `source`, `id`, `time`, `datacontenttype`, `data`). The existing HMAC header still signs the **final** JSON (the envelope).

- Configure `CloudEventsSource` and `CloudEventsType` under `WebhookDelivery` if you need stable routing keys for external receivers.

## Azure Service Bus (optional)

`IIntegrationEventPublisher` publishes UTF-8 JSON payloads after lifecycle events. Messages set `MessageId` when provided (duplicate detection on the queue/topic) and include application property `event_type` plus `Subject` with the same logical type string.

**Managed identity (preferred in Azure):** set the namespace FQDN and queue/topic name. Optionally set `ServiceBusManagedIdentityClientId` for a user-assigned identity.

```json
"IntegrationEvents": {
  "ServiceBusFullyQualifiedNamespace": "mysb.servicebus.windows.net",
  "ServiceBusManagedIdentityClientId": "",
  "QueueOrTopicName": "archlucid-integration-events",
  "SubscriptionName": "archlucid-worker",
  "ConsumerEnabled": false,
  "MaxConcurrentCalls": 4,
  "PrefetchCount": 0
}
```

**Connection string (legacy bootstrap):** still supported when the namespace FQDN is not set.

```json
"IntegrationEvents": {
  "ServiceBusConnectionString": "<connection-string>",
  "QueueOrTopicName": "archlucid-integration-events"
}
```

**Logic App (governance) subscription:** when `enable_logic_app_governance_approval_subscription` is true in `infra/terraform-servicebus/`, a third subscription is created whose default rule is a SQL filter on **`event_type`** so only `com.archlucid.governance.approval.submitted` is delivered — use that subscription name as the Service Bus trigger in the **`governance-approval-routing`** workflow (see `infra/terraform-logicapps/workflows/governance-approval-routing/README.md`).

**Additional optional Logic App subscriptions (same module):** internal trial-lifecycle email dispatch (`com.archlucid.notifications.trial-lifecycle-email.v1` — worker idempotency ledger; **not** a public integration contract for external subscribers), incident ChatOps (`alert.fired` **or** `alert.resolved`), prod-only promotion customer notify (`event_type` + user property **`promotion_environment` = `prod`**), and Marketplace fulfillment hand-off (`com.archlucid.billing.marketplace.webhook.received.v1` only — emitted **after** JWT verification and provider success; see `infra/terraform-logicapps/workflows/marketplace-fulfillment-handoff/README.md`). The API/worker sets `promotion_environment` on Service Bus messages for `com.archlucid.governance.promotion.activated` so SQL filters can target production without parsing the JSON body.

**Promotion customer channel preferences:** `GET /v1/notifications/customer-channel-preferences` (Read authority) returns `TenantNotificationChannelPreferencesResponse` — booleans for email / Teams / outbound-webhook customer notifications and `isConfigured`. If there is no row in `dbo.TenantNotificationChannelPreferences` (migration **082**), GET returns conservative defaults with `isConfigured: false`. **`PUT`** the same path (`TenantNotificationChannelPreferencesUpsertRequest`, **Execute** authority + trial gate) upserts the row and emits durable audit **`TenantNotificationChannelPreferencesUpdated`**; **404** when `dbo.Tenants` has no row for the caller’s tenant (SQL mode). Logic Apps typically **GET** before fan-out; operators or the API **PUT** for configuration.

**Trial lifecycle scan ownership:** set `ArchLucid:Notifications:TrialLifecycle:Owner` to **`LogicApp`** to skip the in-process `TrialLifecycleEmailScanHostedService` / job scan (recurrence moves to Logic App); keep **`Hosted`** (default) until the external workflow is live.

**Alert ChatOps user properties:** for **`com.archlucid.alert.fired`**, publishers attach **`severity`** (lowercased) and **`deduplication_key`** when the JSON payload includes `severity` / `deduplicationKey`. For **`com.archlucid.alert.resolved`**, **`deduplication_key`** is set when the payload includes `deduplicationKey` (emitted from **`AlertIntegrationEventPublishing`**). Use these in Service Bus SQL filters or Logic App expressions; see **`docs/runbooks/LOGIC_APPS_INCIDENT_CHATOPS.md`**.

### Transactional outbox (`dbo.IntegrationEventOutbox`)

When `TransactionalOutboxEnabled` is **true** and storage is **Sql**, integration events are written to `dbo.IntegrationEventOutbox` and published asynchronously by the **leader-elected** `IntegrationEventOutboxHostedService` (same retry/dead-letter behavior for all event types that use the outbox path).

Events using the outbox today:

| Event type (canonical) | When enqueued |
|------------------------|---------------|
| `com.archlucid.authority.run.completed` | Via `OutboxAwareIntegrationEventPublishing.TryPublishOrEnqueueAsync` before commit: same SQL transaction as authority commit when `TransactionalOutboxEnabled` and `SupportsExternalTransaction`; otherwise standalone enqueue or best-effort direct publish (same helper as other events) |
| `com.archlucid.governance.approval.submitted` | After approval request create (standalone SQL connection if no ambient transaction) |
| `com.archlucid.governance.promotion.activated` | Same transaction as environment activation when `SupportsExternalTransaction`; otherwise standalone enqueue after commit |
| `com.archlucid.alert.fired` / `com.archlucid.alert.resolved` | After alert row write (standalone enqueue) |
| `com.archlucid.advisory.scan.completed` | After scan execution completes (standalone enqueue) |
| `com.archlucid.notifications.trial-lifecycle-email.v1` | After durable audit append (`TrialProvisioned`, `CoordinatorRunCommitCompleted`, `TenantTrialConverted`) or scheduled trial scan (standalone enqueue) |
| `com.archlucid.billing.marketplace.webhook.received.v1` | After Marketplace JWT verification, SQL dedupe insert, and successful `AzureMarketplaceBillingProvider` dispatch (`BillingMarketplaceWebhookController` → `MarketplaceWebhookIntegrationEventPublisher`) |

When `TransactionalOutboxEnabled` is **false**, the same call sites use **best-effort** `IIntegrationEventPublisher.PublishAsync` (failures are logged; domain commits are not rolled back).

**Operations:** pending and dead-letter depth surface in metrics; admin APIs remain `GET /admin/integration-outbox/dead-letters` and `POST /admin/integration-outbox/retry` (see API OpenAPI).

### Worker subscription consumer

When the host role is **Worker** and `IntegrationEvents:ConsumerEnabled` is **true**, `AzureServiceBusIntegrationEventConsumer` runs a `ServiceBusProcessor` on `QueueOrTopicName` + `SubscriptionName`. Handlers implement `IIntegrationEventHandler`; a default `LoggingIntegrationEventHandler` (`EventType` `*`) logs payload size/preview so subscriptions can be validated before custom logic is added.

- Successful handling → `CompleteMessageAsync`
- Handler errors → `AbandonMessageAsync` (redelivery; subscription `max_delivery_count` then moves to DLQ)
- Missing `event_type` / unknown poison shape → `DeadLetterMessageAsync` with explicit reason

### Terraform

Module: **`infra/terraform-servicebus`** — namespace (Standard), topic with duplicate detection, subscriptions `archlucid-worker` and `archlucid-external`, optional private endpoint variables, RBAC Sender/Receiver. See module `README.md`.

### JSON Schema catalog

A machine-readable catalog of all event schemas is available at `schemas/integration-events/catalog.json` (event type, `schemaVersion`, filename, transport, short description). Individual schema files still carry their own JSON Schema `$id` where present.

Individual event payload schemas are published as [JSON Schema Draft 2020-12](https://json-schema.org/draft/2020-12/schema) files under `schemas/integration-events/`:

| Schema file | Event type |
|-------------|------------|
| `authority-run-completed.v1.schema.json` | `com.archlucid.authority.run.completed` |
| `manifest-finalized.v1.schema.json` | `com.archlucid.manifest.finalized.v1` |
| `governance-approval-submitted.v1.schema.json` | `com.archlucid.governance.approval.submitted` |
| `governance-promotion-activated.v1.schema.json` | `com.archlucid.governance.promotion.activated` |
| `alert-fired.v1.schema.json` | `com.archlucid.alert.fired` |
| `alert-resolved.v1.schema.json` | `com.archlucid.alert.resolved` |
| `advisory-scan-completed.v1.schema.json` | `com.archlucid.advisory.scan.completed` |
| `data-consistency-check-completed.v1.schema.json` | `com.archlucid.system.data-consistency-check.completed.v1` |
| `compliance-drift-escalated.v1.schema.json` | `com.archlucid.compliance.drift.escalated` |
| `seat-reservation-released.v1.schema.json` | `com.archlucid.seat.reservation.released` |
| `billing-marketplace-webhook-received.v1.schema.json` | `com.archlucid.billing.marketplace.webhook.received.v1` |

**Internal (worker dispatch):** `com.archlucid.notifications.trial-lifecycle-email.v1` has a JSON Schema for worker contracts but is flagged `"internal": true` in `catalog.json`. This event is internal. External consumers should not subscribe to it.

External consumers can validate inbound Service Bus message bodies against the schemas in this table. Each schema sets `additionalProperties: true` so new fields may appear in payloads without a schema-version bump (same additive contract as `IntegrationEventPayloadContractTests`).

### Bridge receivers — Jira Cloud / ServiceNow (HTTP automation rules)

Full **customer-owned** walkthroughs (prerequisites, payload mapping, HMAC options, retries, test steps) for **ServiceNow** then **Jira** via **Power Automate** live under **[`docs/integrations/recipes/`](../integrations/recipes/README.md)**. Those recipes are **optional bridges** — **first-party** **Jira** and **ServiceNow** are **V1 commitments** ([INTEGRATION_CATALOG.md](../go-to-market/INTEGRATION_CATALOG.md), [`V1_SCOPE.md`](V1_SCOPE.md) §2.13).

Some enterprises expose incident/issue workflows behind SaaS automation URLs (`POST`, minimal validation beyond bearer/API-key gates). ArchLucid’s outbound webhook envelope (`WebhookDelivery:UseCloudEventsEnvelope` when enabled) matches CloudEvents JSON — automation layers typically unwrap `data` or route on `type`.

**Jira Cloud Automation — inbound webhook rule**

| Step | Action |
|------|--------|
| 1 | Automation → Create rule → Incoming webhook trigger — copy the webhook URL (HTTPS). |
| 2 | Optional: add header secret — paste into ArchLucid **tenant outbound webhook secret** only when your rule verifies static headers (otherwise rely on private URL). |
| 3 | Action: Create issue / transition — map CloudEvents fields via smart values (`{{webhookData.body.type}}`, JSON parsing on `data`). |
| 4 | Smoke locally with `archlucid webhooks test --url <incoming-webhook-url> [--secret …]` then archive the captured JSON under evidence. |

**ServiceNow — scripted REST / Flow Designer inbound**

| Step | Action |
|------|--------|
| 1 | Scripted REST API or Flow Designer **Inbound REST** step — fixed path + ACL restricted to automation IPs / OAuth client. |
| 2 | Parse body as JSON — branch on `event_type` Service Bus property **or** CloudEvents `type` after unwrap. |
| 3 | Insert/update `sn_si_incident` / CMDB per mapping table owned by platform — avoid executing arbitrary scripts from payload strings. |
| 4 | Validate using CLI probe (`archlucid webhooks test`) before enabling production subscriptions. |

**Security:** Treat inbound URLs as secrets; TLS only; pin automation identities; redact tokens in logs (same posture as digest/alert webhook receivers).

### Event catalog (canonical types)

Payloads use `IntegrationEventJson` (camelCase, omit nulls). See **`docs/contracts/archlucid-asyncapi-2.6.yaml`** for structured schemas (aligned with the JSON Schema files above).

1. **`com.archlucid.authority.run.completed`** — `schemaVersion`, `runId`, `manifestId`, `tenantId`, `workspaceId`, `projectId`
2. **`com.archlucid.governance.approval.submitted`** — `schemaVersion`, scope ids, `approvalRequestId`, `runId`, `manifestVersion`, `sourceEnvironment`, `targetEnvironment`, `requestedBy`
3. **`com.archlucid.governance.promotion.activated`** — `schemaVersion`, scope ids, `activationId`, `runId`, `manifestVersion`, `environment`, `activatedBy`, `activatedUtc`
4. **`com.archlucid.alert.fired`** — `schemaVersion`, scope ids, `alertId`, optional `runId` / `comparedToRunId`, `ruleId`, `category`, `severity`, `title`, `deduplicationKey`
5. **`com.archlucid.alert.resolved`** — `schemaVersion`, scope ids, `alertId`, optional `runId`, `resolvedByUserId`, optional `comment`
6. **`com.archlucid.advisory.scan.completed`** — `schemaVersion`, scope ids, `scheduleId`, `executionId`, `hasRuns`, optional run/digest ids, `completedUtc`
7. **`com.archlucid.manifest.finalized.v1`** — `schemaVersion`, `runId`, `manifestId`, `decisionTraceId`, scope ids, `findingsSnapshotId`, optional `artifactBundleId` / `manifestVersion`
8. **`com.archlucid.compliance.drift.escalated`** — `schemaVersion`, scope ids, `driftSignalId`, `escalatedUtc`, `metricKey`, optional threshold values
9. **`com.archlucid.seat.reservation.released`** — `schemaVersion`, scope ids, `reservationId`, `releasedUtc`, optional `releaseReason`
10. **`com.archlucid.billing.marketplace.webhook.received.v1`** — `schemaVersion`, scope ids, `providerDedupeKey`, `action`, `subscriptionId`, `billingProvider` (Marketplace webhook path; no raw JWT in payload)
11. **`com.archlucid.system.data-consistency-check.completed.v1`** — `checkedAtUtc`, `isHealthy`, `findings[]` (`checkName`, `severity`, `description`, `affectedEntityIds`)

> **`com.archlucid.notifications.trial-lifecycle-email.v1`:** This event is internal. External consumers should not subscribe to it.

When no usable Service Bus configuration is present, a **no-op** publisher is registered.

- Use a **queue** for simplest at-least-once delivery; use a **topic** with subscriptions for fan-out.
- Grant identities **Azure Service Bus Data Sender** on the namespace for publish/outbox drain; grant the worker **Data Receiver** when using the subscription consumer.

## SIEM-bound examples (illustrative)

Below are **illustrative** shapes for mapping ArchLucid CloudEvents / Service Bus bodies into common SIEM collectors. Normalize field names (`eventType`, `tenantId`, `payload`) to your analytic schema.

### Splunk HTTP Event Collector (wrapper around CloudEvents)

Assume the webhook receiver stores the ArchLucid POST body as `rawEnvelope` (UTF-8 JSON text). Index-time extraction maps `eventType`, `tenantId`, and `payload`.

```json
{
  "time": 1746120000,
  "host": "archlucid-ingest",
  "source": "archlucid:integration-events",
  "sourcetype": "archlucid:cloudevents",
  "event": "{\"specversion\":\"1.0\",\"type\":\"com.archlucid.governance.approval.submitted\",\"source\":\"/archlucid/tenant/11111111-1111-1111-1111-111111111111\",\"id\":\"a0d3c4d2-5c2b-4c2b-9c2b-000000000001\",\"time\":\"2026-05-01T12:00:00Z\",\"datacontenttype\":\"application/json\",\"data\":{\"schemaVersion\":1,\"approvalRequestId\":\"AR-1001\",\"runId\":\"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\",\"manifestVersion\":\"v1.0.0\"}}"
}
```

### Microsoft Sentinel / Azure Monitor custom log (JSON Lines row)

Function or Logic App unwraps Service Bus `body` and writes one JSON object per line. `TimeGenerated` is often set from `time` or message enqueue time.

```json
{
  "TimeGenerated": "2026-05-01T12:00:00Z",
  "ArchLucidEventType": "com.archlucid.authority.run.completed",
  "TenantId": "11111111-1111-1111-1111-111111111111",
  "WorkspaceId": "22222222-2222-2222-2222-222222222222",
  "ProjectId": "33333333-3333-3333-3333-333333333333",
  "RunId": "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
  "Payload": {
    "schemaVersion": 1,
    "manifestId": "bbbbbbbb-bbbb-4bbb-bbbb-bbbbbbbbbbbb"
  }
}
```
