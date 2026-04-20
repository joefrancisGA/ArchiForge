> **Scope:** ADR 0008: Alert deduplication scopes - full detail, tables, and links in the sections below.

# ADR 0008: Alert deduplication scopes

- **Status:** Accepted (v1)
- **Date:** 2026-04-04

## Context

Simple and composite alerts must avoid spamming operators when the same condition fires repeatedly across evaluations. Deduplication keys must be stable and scope-aware.

## Decision

- **Simple alerts** use **`IAlertRecordRepository.GetOpenByDeduplicationKeyAsync`** with keys produced by evaluation (`AlertRecord.DeduplicationKey`), scoped by tenant / workspace / project.
- **Composite alerts** use **`IAlertSuppressionPolicy`** to decide **`ShouldCreateAlert`**, **`DeduplicationKey`**, and suppression reasons; composite rules carry **`DedupeScope`** (**`CompositeDedupeScope`**) influencing how keys are built (e.g. rule-only vs rule-and-run).

## Consequences

- **Positive:** Predictable fan-out: one open row per logical incident until resolved/suppressed.
- **Trade-off:** Changing dedupe rules requires migration/communication — existing open alerts may not align with new keys.
