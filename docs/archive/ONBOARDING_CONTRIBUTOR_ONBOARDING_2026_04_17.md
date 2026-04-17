# Contributor onboarding

→ New here? Start at [docs/START_HERE.md](START_HERE.md) instead.

The shipped product is **ArchLucid**; .NET projects use the **`ArchLucid.*`** naming (solution: **`ArchLucid.sln`**). The workspace or remote repository folder may still be named **`ArchLucid`** until Phase 7.6.

**Where does this fit?** For the full **clone → Azure** narrative (not just build/tests), see **[GOLDEN_PATH.md](GOLDEN_PATH.md)**.

## Build

```bash
git clone <repo>
cd <repository-root>
dotnet restore
dotnet build
```

## Tests

- **Core corset (matches CI `dotnet-fast-core` test step):**  
  `dotnet test --filter "Suite=Core&Category!=Slow&Category!=Integration"`
- **Fast feedback (exclude HTTP integration):**  
  `dotnet test --filter "Category!=Integration"`
- **Integration (full API + SQL Server — see [TEST_STRUCTURE.md](TEST_STRUCTURE.md)):**  
  `dotnet test --filter "Category=Integration"`

See **[TEST_STRUCTURE.md](TEST_STRUCTURE.md)** for project layout and traits, and **[TEST_EXECUTION_MODEL.md](TEST_EXECUTION_MODEL.md)** (54R) for Core / Fast core / SQL / Full scripts at repo root.

- **Operator UI (`archlucid-ui/`):** `npm ci` then `npm test` (Vitest) for fast checks; `npm run test:e2e` for Playwright smoke. From repo root: **`test-ui-unit.cmd`** / **`test-ui-smoke.cmd`** (see **TEST_EXECUTION_MODEL.md**).

## Configuration

- **Local API:** Use **`appsettings.Development.json`** patterns; never commit secrets. Prefer **user secrets** or environment variables for connection strings.
- **CORS:** Empty **`Cors:AllowedOrigins`** denies browser origins by default — set explicit origins for SPA work.

## Optional integration against SQL Server

See **[BUILD.md](BUILD.md)** (SQL Server for persistence tests: `ARCHLUCID_SQL_TEST` or LocalDB).

## Where to start reading

- **[API_CONTRACTS.md](API_CONTRACTS.md)** — versioning, ProblemDetails, ingestion fields.
- **[CONTEXT_INGESTION.md](CONTEXT_INGESTION.md)** — connector order and warnings.
- **[ALERTS.md](ALERTS.md)** — alert/advisory route map.
