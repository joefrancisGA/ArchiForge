# ADR 0007: Effective governance merge

- **Status:** Accepted (v1)
- **Date:** 2026-04-04

## Context

Multiple **policy pack** assignments can apply to a tenant / workspace / project. Runtime features (alerts, compliance filtering, advisory defaults) need a **single merged** `PolicyPackContentDocument` plus optional explainability for operators.

## Decision

- **Resolution** is implemented by **`IEffectiveGovernanceResolver`** (assignments → versions → merge with deterministic precedence). **`IEffectiveGovernanceLoader`** exposes only **`EffectiveContent`** for call sites that do not need decisions/conflicts.
- **Merge rules** for list fields (e.g. compliance / alert rule id lists) and dictionary fields (**`advisoryDefaults`**, **`metadata`**) are implemented in **`EffectiveGovernanceResolver`** and reflected in API responses `GET .../effective-content` and `GET /v1/governance-resolution`.

## Consequences

- **Positive:** One document drives alerts, compliance, and advisory behavior; conflicts are inspectable via `governance-resolution`.
- **Trade-off:** Callers must not bypass the resolver with hand-merged JSON — drift would break audit and operator trust.

## Links

- `docs/GLOSSARY.md` (effective governance entry)
