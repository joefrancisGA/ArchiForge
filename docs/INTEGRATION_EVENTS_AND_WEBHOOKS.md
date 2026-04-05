# Integration events and webhook interoperability

## CloudEvents on HTTP webhooks

When `WebhookDelivery:UseCloudEventsEnvelope` is **true**, digest and alert webhook POST bodies are wrapped in a [CloudEvents 1.0](https://github.com/cloudevents/spec/blob/v1.0.2/cloudevents/formats/json-format.md) JSON envelope (`specversion`, `type`, `source`, `id`, `time`, `datacontenttype`, `data`). The existing HMAC header still signs the **final** JSON (the envelope).

- Configure `CloudEventsSource` and `CloudEventsType` under `WebhookDelivery` if you need stable routing keys for external receivers.

## Azure Service Bus (optional)

`IIntegrationEventPublisher` publishes UTF-8 JSON payloads after selected lifecycle events (e.g. authority run completed).

Configure:

```json
"IntegrationEvents": {
  "ServiceBusConnectionString": "<connection-string>",
  "QueueOrTopicName": "archiforge-integration-events"
}
```

When either value is empty, a **no-op** publisher is registered (no overhead).

- Use a **queue** for simplest at-least-once delivery; use a **topic** with subscriptions for fan-out.
- Grant the API/worker managed identity **Azure Service Bus Data Sender** if you switch to AAD-based clients in a future iteration (today the connection string path is supported for bootstrap).

## Event type: `com.archiforge.authority.run.completed`

Payload shape (UTF-8 JSON):

```json
{
  "runId": "...",
  "manifestId": "...",
  "tenantId": "...",
  "workspaceId": "...",
  "projectId": "..."
}
```

Publish failures are logged as warnings and **do not** roll back the committed authority run.
