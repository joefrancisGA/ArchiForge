# SQL relational backfill (JSON → child tables)

## Objective

One-time alignment for databases that still have authority data only in legacy JSON columns (`CanonicalObjectsJson`, `NodesJson`, `FindingsJson`, GoldenManifest slice JSON, `ArtifactsJson` / `TraceJson`). The utility **deserializes** using the same shapes as production code and **INSERT**s missing rows into relational tables. **JSON columns are not deleted or altered.**

## When to run

- After deploying dual-write repositories to an environment that already contained rows written **before** relational tables existed.
- Before relying on relational-first reads in reporting or analytics.

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

**Project:** `ArchiForge.Backfill.Cli`

**Connection string** (first match wins):

1. Environment variable: `ARCHIFORGE_SQL`
2. `--connection` / `-c` followed by the ADO.NET connection string
3. First positional argument that is not a flag (legacy)

**Example (PowerShell):**

```powershell
$env:ARCHIFORGE_SQL = "Server=localhost;Database=ArchiForge;User Id=...;Password=...;TrustServerCertificate=True"
dotnet run --project ArchiForge.Backfill.Cli
```

**Example (positional):**

```powershell
dotnet run --project ArchiForge.Backfill.Cli -- "Server=.;Database=ArchiForge;Integrated Security=true;TrustServerCertificate=True"
```

**Example (explicit connection + scope):**

```powershell
dotnet run --project ArchiForge.Backfill.Cli -- -c "Server=.;Database=ArchiForge;Integrated Security=true;TrustServerCertificate=True" --only context,graph
```

**Scope flags:**

| Flag | Effect |
|------|--------|
| *(default)* | All five stages run |
| `--only context,graph,findings,golden,artifact` | Only listed stages (comma-separated; aliases: `artifacts` → artifact) |
| `--skip-context`, `--skip-graph`, `--skip-findings`, `--skip-golden`, `--skip-artifact` | Disable that stage; others stay on |

If `--only` is present, `--skip-*` is ignored.

**Help:** `dotnet run --project ArchiForge.Backfill.Cli -- --help`

Programmatically, construct `SqlRelationalBackfillOptions` with `init` properties set to `false` to skip stages when not using the CLI.

## Programmatic usage

Inject or construct `SqlRelationalBackfillService` with:

- `ISqlConnectionFactory`
- Concrete SQL repositories: `SqlContextSnapshotRepository`, `SqlGraphSnapshotRepository`, `SqlFindingsSnapshotRepository`, `SqlGoldenManifestRepository`, `SqlArtifactBundleRepository`
- `ILogger<SqlRelationalBackfillService>`

Call `ISqlRelationalBackfillService.RunAsync(options, cancellationToken)` and inspect `SqlRelationalBackfillReport`.

## Tests

Integration tests: `ArchiForge.Persistence.Tests` → `SqlRelationalBackfillServiceSqlIntegrationTests` (requires Docker / SQL Server Testcontainers).

Filter:

```powershell
dotnet test ArchiForge.Persistence.Tests --filter "FullyQualifiedName~SqlRelationalBackfillServiceSqlIntegrationTests"
```

## Security and operations

- Use a **least-privilege** SQL login limited to the ArchiForge database; the tool only **INSERT**s into relational tables (no JSON column drops).
- Run during a **maintenance window** if the database is large (full table scans of snapshot/manifest/bundle ids).
- **Backup** the database before the first production run.

## Limitations

- Rows that are **structurally invalid** in JSON will fail for that key and be reported; fix source data or re-snapshot as needed.
- **Partial** provenance or trace slices on very old inconsistent data may require a second pass after manual cleanup (per-slice idempotency is designed for typical dual-write gaps).
