# Contributor onboarding

## Build

```bash
git clone <repo>
cd ArchiForge
dotnet restore
dotnet build
```

## Tests

- **Fast feedback (exclude HTTP integration):**  
  `dotnet test --filter "Category!=Integration"`
- **Integration (full API + SQL Server — see [TEST_STRUCTURE.md](TEST_STRUCTURE.md)):**  
  `dotnet test --filter "Category=Integration"`

See **[TEST_STRUCTURE.md](TEST_STRUCTURE.md)** for project layout and traits.

## Configuration

- **Local API:** Use **`appsettings.Development.json`** patterns; never commit secrets. Prefer **user secrets** or environment variables for connection strings.
- **CORS:** Empty **`Cors:AllowedOrigins`** denies browser origins by default — set explicit origins for SPA work.

## Optional integration against SQL Server

See **[BUILD.md](BUILD.md)** (SQL Server for persistence tests: `ARCHIFORGE_SQL_TEST` or LocalDB).

## Where to start reading

- **[API_CONTRACTS.md](API_CONTRACTS.md)** — versioning, ProblemDetails, ingestion fields.
- **[CONTEXT_INGESTION.md](CONTEXT_INGESTION.md)** — connector order and warnings.
- **[ALERTS.md](ALERTS.md)** — alert/advisory route map.
