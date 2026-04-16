# Integration event schema registry

## Objective

Describe the **file-based schema registry** for outbound CloudEvents integration events and how it stays aligned with product code.

## Assumptions

- Consumers read committed JSON Schemas from the repository (or a mirrored artifact feed).
- Azure Schema Registry may be adopted later without changing the logical registry shape.

## Constraints

- Canonical event type strings live in **`ArchLucid.Core.Integration.IntegrationEventTypes`**.
- Schema files are the source of truth for payload shape; **`schemas/integration-events/catalog.json`** is the machine-readable index.

## Architecture overview

| Node | Role |
|------|------|
| **`IntegrationEventTypes`** | Compile-time constants for `com.archlucid.*` type strings. |
| **`schemas/integration-events/*.schema.json`** | JSON Schema documents with **`$id`** URIs. |
| **`schemas/integration-events/catalog.json`** | Registry manifest: event type, schema version, file name, transport notes. |
| **`IntegrationEventCatalogSyncTests`** | CI guard: catalog rows match constants and **`$id`** matches **`schemaUri`**. |

## Data flow

1. Product code publishes an event with a type from **`IntegrationEventTypes`**.
2. **`IntegrationEventPayloadContractTests`** (and related tests) validate sample payloads against schema files.
3. **`catalog.json`** is updated when a new event type or schema revision ships.

## Security model

- Schemas document PII-bearing fields; treat catalog + schemas as **internal contract** material unless explicitly published to customers.

## Operational considerations

- When adding an event: add **`IntegrationEventTypes`** constant, schema file, **`catalog.json`** row, contract test payload, and **`INTEGRATION_EVENT_CATALOG.md`** row.
- **`docs/INTEGRATION_EVENTS_AND_WEBHOOKS.md`** remains the narrative for delivery and webhooks.

## Related

- **`schemas/integration-events/README.md`**
- **`docs/INTEGRATION_EVENT_CATALOG.md`**
- **`ArchLucid.Core.Tests/Integration/IntegrationEventCatalogSyncTests.cs`**
