> **Scope:** Typed findings schema (envelope + payloads) - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


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

- **`ArchLucid.Decisioning.Tests`** — factory, converter, migrator, serialization round-trip, and end-to-end graph → findings → manifest.
