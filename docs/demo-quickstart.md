# Demo quickstart (Corrected 50R — Contoso Retail Modernization)

This guide gets a **fresh SQL-backed** environment to a repeatable demo state: two committed runs (baseline vs hardened), governance workflow rows, environment activations for preview/compare, and an optional sample export-history row. See **[TRUSTED_BASELINE.md](TRUSTED_BASELINE.md)** for what is baseline-trusted vs optional (export replay is not part of the minimal proof).

## Prerequisites

- .NET 10 SDK
- SQL Server connection string in `ConnectionStrings:ArchiForge` (LocalDB, Docker from `archiforge dev up`, or your own instance)
- `ArchiForge:StorageProvider` = `Sql` in configuration (not `InMemory`) so the same database receives DbUp migrations and demo rows

## 1. Migrations (DbUp)

On API startup, when `ConnectionStrings:ArchiForge` is set and is **not** SQLite (see `DatabaseMigrator.IsSqliteConnection`), [DatabaseMigrator](../ArchiForge.Data/Infrastructure/DatabaseMigrator.cs) runs embedded scripts whose resource name contains **`.Migrations.`** (i.e. files under `ArchiForge.Data/Migrations/`) in **lexicographic** order. Console output lists each script. Governance workflow DDL is in **`017_GovernanceWorkflow.sql`** (approval requests, promotion records, environment activations).

If migration fails, the process throws and the host does not start.

## 2. Enable demo seed

Configuration section: **`Demo`** (see [DemoOptions](../ArchiForge.Api/Configuration/DemoOptions.cs)).

| Setting | Meaning |
|--------|---------|
| `Demo:Enabled` | Master switch for `POST /v1.0/demo/seed` and startup seeding. |
| `Demo:SeedOnStartup` | When `true` **and** the host environment is **Development**, runs `IDemoSeedService.SeedAsync()` once after DbUp. |

`appsettings.Development.json` ships with both flags **`false`** so seeding is opt-in.

### Option A — seed on startup (Development only)

```json
"Demo": {
  "Enabled": true,
  "SeedOnStartup": true
}
```

Also set `ArchiForge:StorageProvider` to `Sql` and a valid SQL Server connection string (e.g. via user secrets).

### Option B — explicit HTTP call

1. Set `Demo:Enabled` to `true` (keep `SeedOnStartup` false if you prefer).
2. Start the API in Development.
3. Call **`POST /v1.0/demo/seed`** with a principal that satisfies **`ExecuteAuthority`** (DevelopmentBypass Admin works by default).

Returns **204** when complete. **404** if not Development. **400** if `Demo:Enabled` is false.

## 3. What gets created

Stable identifiers are defined in [ContosoRetailDemoIdentifiers](../ArchiForge.Application/Bootstrap/ContosoRetailDemoIdentifiers.cs). The seed is **idempotent**: existing keys are skipped.

| Area | Content |
|------|---------|
| Request | `request-contoso-demo` — Contoso Retail Platform scenario |
| Runs | `run-baseline-demo` (weaker posture), `run-hardened-demo` (improved controls) |
| Manifests | `contoso-baseline-v1`, `contoso-hardened-v1` |
| Tasks / results | One topology task + result per run |
| Decision traces | One commit trace per run |
| Governance | Approved approval `apr-demo-001`, promotion `promo-demo-001` on hardened run |
| Activations | **dev** → baseline manifest; **test** → hardened manifest (`act-demo-dev-001`, `act-demo-test-001`) |
| Export history (optional) | `export-demo-baseline-001` — placeholder row for export **history**; not used for consulting DOCX replay (see [TRUSTED_BASELINE.md](TRUSTED_BASELINE.md)) |

## 4. Verify with HTTP

Replace base URL and version as needed (`v1.0`).

- **Run detail:** `GET /v1.0/architecture/run/run-baseline-demo` (and `run-hardened-demo`).
- **Compare agents:** `GET /v1.0/architecture/run/compare/agents?leftRunId=run-baseline-demo&rightRunId=run-hardened-demo`
- **Export history:** `GET /v1.0/architecture/run/run-baseline-demo/exports`
- **Governance preview:** use existing governance preview endpoints with environments **dev** / **test** and manifest versions above.
- **Governance workflow:** list approvals/promotions/activations by run via your governance APIs.

## 5. Tests

[DemoSeedServiceTests](../ArchiForge.Api.Tests/DemoSeedServiceTests.cs) cover idempotency, canonical run detail, and governance environment comparison against the SQLite test factory.

## Safety

Do **not** enable `Demo:SeedOnStartup` in production. Keep demo seeding to Development or controlled lab environments.
