# Typed findings schema (envelope + payloads)

## Schema versions

- **`Finding.FindingSchemaVersion`** — bump when the finding envelope or payload contracts change. Current: `FindingsSchema.CurrentFindingVersion`.
- **`FindingsSnapshot.SchemaVersion`** — snapshot container version. Current: `FindingsSchema.CurrentSnapshotVersion`.
- **`FindingsSnapshotMigrator.Apply`** — normalizes legacy findings (e.g. missing `Category` / `PayloadType`) after load or before persist.

## Payload registry & JSON

- **`FindingPayloadRegistry`** maps `PayloadType` name → CLR type.
- **`FindingJsonConverter`** + **`FindingsSerialization.SerializeSnapshot` / `DeserializeSnapshot`** round-trip snapshots with typed `Payload` rehydration.

## Rules & manifest

- **`InMemoryDecisionRuleProvider`** includes rules for `SecurityControlFinding` (allow) and `CostConstraintFinding` (prefer).
- **`CostConstraintFindingEngine`** ingests `CostConstraint` graph nodes.
- **`DefaultGoldenManifestBuilder`** adds a **Security** `ResolvedArchitectureDecision** when a control is **missing**.

## Observability

- **`FindingsOrchestrator`** logs per-engine duration and finding counts, plus snapshot totals (inject `ILogger<FindingsOrchestrator>` in production).

## Tests

- **`ArchiForge.Decisioning.Tests`** — factory, converter, migrator, serialization round-trip, and end-to-end graph → findings → manifest.
