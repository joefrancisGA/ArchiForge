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
| `governance-approval-submitted.v1.schema.json` | `com.archlucid.governance.approval.submitted` |
| `governance-promotion-activated.v1.schema.json` | `com.archlucid.governance.promotion.activated` |
| `alert-fired.v1.schema.json` | `com.archlucid.alert.fired` |
| `alert-resolved.v1.schema.json` | `com.archlucid.alert.resolved` |
| `advisory-scan-completed.v1.schema.json` | `com.archlucid.advisory.scan.completed` |
| `trial-lifecycle-email.v1.schema.json` | `com.archlucid.notifications.trial-lifecycle-email.v1` |

External consumers can validate inbound Service Bus message bodies against these schemas. Each schema sets `additionalProperties: true` so new fields may appear in payloads without a schema-version bump (same additive contract as `IntegrationEventPayloadContractTests`).

### Event catalog (canonical types)

Payloads use `IntegrationEventJson` (camelCase, omit nulls). See **`docs/contracts/archlucid-asyncapi-2.6.yaml`** for structured schemas (aligned with the JSON Schema files above).

1. **`com.archlucid.authority.run.completed`** — `schemaVersion`, `runId`, `manifestId`, `tenantId`, `workspaceId`, `projectId`
2. **`com.archlucid.governance.approval.submitted`** — `schemaVersion`, scope ids, `approvalRequestId`, `runId`, `manifestVersion`, `sourceEnvironment`, `targetEnvironment`, `requestedBy`
3. **`com.archlucid.governance.promotion.activated`** — `schemaVersion`, scope ids, `activationId`, `runId`, `manifestVersion`, `environment`, `activatedBy`, `activatedUtc`
4. **`com.archlucid.alert.fired`** — `schemaVersion`, scope ids, `alertId`, optional `runId` / `comparedToRunId`, `ruleId`, `category`, `severity`, `title`, `deduplicationKey`
5. **`com.archlucid.alert.resolved`** — `schemaVersion`, scope ids, `alertId`, optional `runId`, `resolvedByUserId`, optional `comment`
6. **`com.archlucid.advisory.scan.completed`** — `schemaVersion`, scope ids, `scheduleId`, `executionId`, `hasRuns`, optional run/digest ids, `completedUtc`
7. **`com.archlucid.notifications.trial-lifecycle-email.v1`** — `schemaVersion`, `trigger`, scope ids, optional `runId`, optional `targetTier` (see `docs/EMAIL_NOTIFICATIONS.md`)

When no usable Service Bus configuration is present, a **no-op** publisher is registered.

- Use a **queue** for simplest at-least-once delivery; use a **topic** with subscriptions for fan-out.
- Grant identities **Azure Service Bus Data Sender** on the namespace for publish/outbox drain; grant the worker **Data Receiver** when using the subscription consumer.
