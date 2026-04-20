> **Scope:** SQL relational backfill and cutover (JSON → child tables) - full detail, tables, and links in the sections below.

# SQL relational backfill and cutover (JSON → child tables)

## Objective

One-time alignment for databases that still have authority data only in legacy JSON columns (`CanonicalObjectsJson`, `NodesJson`, `FindingsJson`, GoldenManifest slice JSON, `ArtifactsJson` / `TraceJson`). The utility **deserializes** using the same shapes as production code and **INSERT**s missing rows into relational tables. **JSON columns are not deleted or altered.**

## Read path (relational-first)

Runtime **`JsonFallbackPolicy` / `PersistenceReadMode` were removed.** Reads use relational child tables for the audited slices; if child rows are missing, collections are **empty** (not hydrated from JSON), except the **graph edge metadata merge** documented in **[JSON_FALLBACK_AUDIT.md](JSON_FALLBACK_AUDIT.md)**.

Use **`ArchLucid.Backfill.Cli --readiness`** to confirm per-slice relational coverage before assuming historical rows are visible through the API.

## When to run the backfill

- After deploying dual-write repositories to an environment that already contained rows written **before** relational tables existed.
- Before relying on relational reads for reporting, analytics, or operator workflows.

## What it does

| Stage | Source | Relational targets |
|--------|--------|---------------------|
| ContextSnapshots | JSON columns on `dbo.ContextSnapshots` | `ContextSnapshotCanonicalObjects`, properties, warnings, errors, source hashes |
| GraphSnapshots | `NodesJson` / `EdgesJson` / `WarningsJson` | Nodes, warnings, indexed edges, edge properties |
| FindingsSnapshots | `FindingsJson` | `FindingRecords` and finding child tables |
| GoldenManifests | Phase-1 slice JSON | `GoldenManifestAssumptions`, `GoldenManifestWarnings`, provenance lists, decisions + links |
| ArtifactBundles | `ArtifactsJson` / `TraceJson` | Artifact rows, metadata, decision links, trace lists |

## Idempotency and failures

- Each **slice** is skipped when child rows already exist for that slice (safe to re-run).
- **Per-entity** failures are logged and recorded in the report; processing **continues** for the next key.
- Exit code from the CLI: `0` = no failures, `2` = at least one entity failed (see stderr / report output).

## CLI usage

**Project:** `ArchLucid.Backfill.Cli`

**Connection string** (first match wins):

1. Environment variable: `ARCHLUCID_SQL`
2. `--connection` / `-c` followed by the ADO.NET connection string
3. First positional argument that is not a flag (legacy)

**Example (PowerShell):**

```powershell
$env:ARCHLUCID_SQL = "Server=localhost;Database=ArchLucid;User Id=...;Password=...;TrustServerCertificate=True"
dotnet run --project ArchLucid.Backfill.Cli
```

**Example (positional):**

```powershell
dotnet run --project ArchLucid.Backfill.Cli -- "Server=.;Database=ArchLucid;Integrated Security=true;TrustServerCertificate=True"
```

**Example (explicit connection + scope):**

```powershell
dotnet run --project ArchLucid.Backfill.Cli -- -c "Server=.;Database=ArchLucid;Integrated Security=true;TrustServerCertificate=True" --only context,graph
```

**Scope flags:**

| Flag | Effect |
|------|--------|
| *(default)* | All five stages run |
| `--only context,graph,findings,golden,artifact` | Only listed stages (comma-separated; aliases: `artifacts` → artifact) |
| `--skip-context`, `--skip-graph`, `--skip-findings`, `--skip-golden`, `--skip-artifact` | Disable that stage; others stay on |

If `--only` is present, `--skip-*` is ignored.

**Help:** `dotnet run --project ArchLucid.Backfill.Cli -- --help`

Programmatically, construct `SqlRelationalBackfillOptions` with `init` properties set to `false` to skip stages when not using the CLI.

## Programmatic usage

Inject or construct `SqlRelationalBackfillService` with:

- `ISqlConnectionFactory`
- Concrete SQL repositories: `SqlContextSnapshotRepository`, `SqlGraphSnapshotRepository`, `SqlFindingsSnapshotRepository`, `SqlGoldenManifestRepository`, `SqlArtifactBundleRepository`
- `ILogger<SqlRelationalBackfillService>`

Call `ISqlRelationalBackfillService.RunAsync(options, cancellationToken)` and inspect `SqlRelationalBackfillReport`.

## Tests

Integration tests: `ArchLucid.Persistence.Tests` → `SqlRelationalBackfillServiceSqlIntegrationTests` (requires SQL Server: set **`ARCHLUCID_SQL_TEST`** or use LocalDB on Windows; CI uses a SQL Server service container).

Filter:

```powershell
dotnet test ArchLucid.Persistence.Tests --filter "FullyQualifiedName~SqlRelationalBackfillServiceSqlIntegrationTests"
```

## Security and operations

- Use a **least-privilege** SQL login limited to the target SQL database; the tool only **INSERT**s into relational tables (no JSON column drops).
- Run during a **maintenance window** if the database is large (full table scans of snapshot/manifest/bundle ids).
- **Backup** the database before the first production run.

## Readiness report (`--readiness`)

Run the **readiness report** to verify that every header row has relational children across all monitored slices (read-only; no data is modified). Use it before assuming **relational-only** reads will return full data for legacy databases.

```powershell
dotnet run --project ArchLucid.Backfill.Cli -- --readiness -c "Server=.;Database=ArchLucid;Integrated Security=true;TrustServerCertificate=True"
```

Or with the environment variable:

```powershell
$env:ARCHLUCID_SQL = "Server=.;Database=ArchLucid;Integrated Security=true;TrustServerCertificate=True"
dotnet run --project ArchLucid.Backfill.Cli -- --readiness
```

**Example output:**

```
=== Relational Cutover Readiness Report ===

Slice                                          Total   Ready   Missing     Status
--------------------------------------------------------------------------------
ContextSnapshot.CanonicalObjects                 200     200         0      READY
ContextSnapshot.Warnings                         200     200         0      READY
GraphSnapshot.Nodes                              200     200         0      READY
GraphSnapshot.EdgeProperties                     200     180        20  NOT READY
FindingsSnapshot.Findings                        150     150         0      READY
GoldenManifest.Provenance                        100      98         2  NOT READY
ArtifactBundle.Artifacts                          80      80         0      READY
...
--------------------------------------------------------------------------------

2 slice(s) NOT READY. Run backfill before relying on relational-only reads.
```

**Exit codes:** `0` = all slices ready, `3` = one or more slices not ready.

**Programmatic usage:**

```csharp
ICutoverReadinessService readiness = provider.GetRequiredService<ICutoverReadinessService>();
CutoverReadinessReport report = await readiness.AssessAsync(ct);

if (report.IsFullyReady)
    // relational coverage complete for monitored slices
else
    foreach (CutoverSliceReadiness slice in report.SlicesNotReady)
        Console.WriteLine($"{slice.SliceName}: {slice.HeadersMissingRelationalRows} rows need backfill");
```

**Slices assessed (14 total):**

| Entity type | Slices |
|------------|--------|
| ContextSnapshot | CanonicalObjects, Warnings, Errors, SourceHashes |
| GraphSnapshot | Nodes, Edges, Warnings, EdgeProperties |
| FindingsSnapshot | Findings |
| GoldenManifest | Assumptions, Warnings, Decisions, Provenance |
| ArtifactBundle | Artifacts |

---

## Cutover checklist

1. **Run the backfill** across all environments that have legacy JSON-only rows.
2. **Run `--readiness`** until all monitored slices report `READY`.
3. **Verify** application behavior (saved snapshots, manifests, findings) in staging before production.
4. **Optional:** stop dual-writing JSON columns and remove the remaining `EdgesJson` merge path after edge-property backfill (see **JSON_FALLBACK_AUDIT.md**).

---

## Limitations

- Rows that are **structurally invalid** in JSON will fail for that key and be reported; fix source data or re-snapshot as needed.
- **Partial** provenance or trace slices on very old inconsistent data may require a second pass after manual cleanup (per-slice idempotency is designed for typical dual-write gaps).
- The readiness report uses **`WHERE EXISTS`** correlated subqueries; on very large databases this is efficient but may benefit from a maintenance window if the server is heavily loaded.
