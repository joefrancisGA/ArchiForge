# Integration event payload schemas

This directory contains **JSON Schema Draft 2020-12** documents describing UTF-8 JSON bodies for **Azure Service Bus** integration events (`com.archlucid.*` type strings). Use them to validate inbound messages in consumers or code generators.

- **Machine-readable index:** [catalog.json](catalog.json) — event types, schema versions, filenames, transport, and descriptions (sync-guarded in `IntegrationEventPayloadContractTests`).
- **AsyncAPI:** [docs/contracts/archlucid-asyncapi-2.6.yaml](../../docs/contracts/archlucid-asyncapi-2.6.yaml) — channels, bindings, and payload `$ref` into these files.
- **Operations & config:** [docs/INTEGRATION_EVENTS_AND_WEBHOOKS.md](../../docs/INTEGRATION_EVENTS_AND_WEBHOOKS.md) — transactional outbox, webhooks, Terraform, and tuning.

**Compatibility:** each schema sets `additionalProperties: true`, so **new fields may appear** without a schema file bump; treat **new event type strings** or **higher `schemaVersion`** values as the contract for breaking changes.
