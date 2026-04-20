> **Scope:** ADR 0011: ArchLucid:StorageProvider — InMemory vs Sql - full detail, tables, and links in the sections below.

# ADR 0011: `ArchLucid:StorageProvider` — InMemory vs Sql

- **Status:** Accepted
- **Date:** 2026-04-04

## Context

ArchLucid must run in:

- **Local / CI / demos** without Azure SQL or migration prerequisites.
- **Production** with durable SQL, resilience, and optional RLS/session context.

A single configuration switch avoids duplicating entire host graphs.

## Decision

Use **`ArchLucid:StorageProvider`** with supported values:

- **`InMemory`** — singleton in-memory repositories for components bound to this option (see **`AddArchLucidStorage`**, **`RegisterCoordinatorDecisionEngineAndRepositories`**, **`RegisterComparisonReplayAndDrift`**, **`RegisterRunExportAndArchitectureAnalysis`**, **`RegisterGovernance`**). Suitable for development and automated tests; data is not durable and is shared per process for singleton stores.

- **`Sql`** (default) — Dapper repositories with scoped lifetimes where appropriate, `IDbConnectionFactory` / SQL connection stack from **`AddArchLucidStorage`** and API data infrastructure.

Governance repositories (**`IGovernanceApprovalRequestRepository`**, **`IGovernancePromotionRecordRepository`**, **`IGovernanceEnvironmentActivationRepository`**) follow the **same** `ArchLucid:StorageProvider` flag: InMemory registrations are **singleton**; Sql registrations remain **scoped** alongside **`IGovernanceWorkflowService`**.

## Consequences

- **Security:** InMemory mode must never be used for production tenants handling real data; treat it as **dev/test only** unless explicitly isolated (e.g. throwaway demo containers).
- **Reliability:** Sql mode inherits connection resilience and migration discipline; InMemory mode loses data on process exit.
- **Cost:** InMemory avoids SQL spend for labs; production remains on Azure SQL (or equivalent) with private connectivity.
- **Testing:** Contract tests validate InMemory and Dapper implementations against shared abstract scenarios where SQL is available.

## Related

- `ArchLucid.Host.Core.Configuration.ArchLucidOptions`.
- ADR 0010 (dual contracts; both paths may need InMemory implementations).
- `docs/GLOSSARY.md` (storage provider entry).
