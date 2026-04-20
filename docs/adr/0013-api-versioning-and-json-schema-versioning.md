> **Scope:** ADR 0013 — API versioning and JSON schemaVersion on persisted aggregates - full detail, tables, and links in the sections below.

# ADR 0013 — API versioning and JSON schemaVersion on persisted aggregates

**Status:** Accepted  
**Date:** 2026-04-14

## Context

ArchLucid ships a single URL namespace under `/v1/...` today. Operators and integrators need a forward path for breaking HTTP changes without silent client drift. Persisted JSON payloads (`GoldenManifest`, `GraphSnapshot`, findings) need an explicit **additive evolution** story.

## Decision

1. **HTTP:** Use **Asp.Versioning.Mvc** with default **1.0**, URL segment `v{version:apiVersion}`, **`ReportApiVersions`**, and **`[ApiVersion("1.0")]`** on versioned controllers (see `ArchLucid.Api/Startup/MvcExtensions.cs`). **`VersionController`** remains **`[ApiVersionNeutral]`**.
2. **JSON:** Add **`schemaVersion`** (CLR: **`SchemaVersion`**, default **1**) on **`ArchLucid.Decisioning.Models.GoldenManifest`** and **`ArchLucid.KnowledgeGraph.Models.GraphSnapshot`**. Consumers should ignore unknown properties and branch on **`schemaVersion`** when breaking changes are introduced.

## Consequences

- **Positive:** Clear extension point for `/v2` and for JSON migrations without guessing wire compatibility.
- **Negative:** New fields increase payload size slightly; serializers must continue to default missing **`schemaVersion`** to **1** for older rows (System.Text.Json typically defaults numeric properties when absent only if not using custom converters — verify loaders for any hand-rolled deserialization).

## Alternatives considered

- **Header-only API versioning** — rejected: less discoverable than URL segment for operators and OpenAPI links.
- **Per-table SQL migration only** — rejected: JSON blobs need an in-payload version for out-of-band tools and partial reads.
