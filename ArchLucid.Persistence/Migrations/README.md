# `ArchLucid.Persistence/Migrations`

SQL Server **incremental** migrations consumed by **DbUp** at API startup.

- **Embedded** in assembly **ArchLucid.Persistence** (`EmbeddedResource Include="Migrations\*.sql"` in `ArchLucid.Persistence.csproj`).
- **Ordering:** Resource names must sort lexicographically — use prefixes `001_`, `002_`, … (see `ArchLucid.Api.Tests/DatabaseMigrationScriptTests.cs`).
- **Included by DbUp** only when the embedded resource name contains `.Migrations.` and ends with `.sql` (see `DatabaseMigrator.Run` in `Data/Infrastructure/DatabaseMigrator.cs`).

**Catalog, consolidated scripts, Persistence bootstrap, and change workflow:** [../../docs/SQL_SCRIPTS.md](../../docs/SQL_SCRIPTS.md).

After editing migrations, update **`Scripts/ArchLucid.sql`** so greenfield installs stay aligned.
