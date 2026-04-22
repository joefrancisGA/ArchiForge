# Breaking changes

## 2026-04-08 — Phase 7 ArchLucid rename (application configuration and CLI)

### Who is affected

Operators and integrators who still use **legacy ArchiForge-branded** configuration keys, environment variables, OIDC browser storage keys, CLI filenames, or Terraform **variable** names from pre–Phase 7 deployments.

### What changed

| Area | Before | After |
|------|--------|--------|
| SQL connection string name | `ConnectionStrings:ArchiForge` | `ConnectionStrings:ArchLucid` |
| Product section | `ArchiForge:*` | `ArchLucid:*` |
| Auth section | `ArchiForgeAuth:*` | `ArchLucidAuth:*` |
| CLI / automation env | `ARCHIFORGE_API_URL`, `ARCHIFORGE_API_KEY`, `ARCHIFORGE_SQL`, etc. | `ARCHLUCID_*` equivalents |
| UI server env | `ARCHIFORGE_API_BASE_URL`, etc. | `ARCHLUCID_API_BASE_URL`, `NEXT_PUBLIC_ARCHLUCID_*` |
| OIDC sessionStorage | `archiforge_oidc_*` | `archlucid_oidc_*` only |
| CLI project manifest | `archiforge.json` | `archlucid.json` |
| Global .NET tool command | `dotnet tool run archiforge` | `dotnet tool run archlucid` |
| Integration event types | `com.archiforge.*` (and any aliases) | **Removed** — use `com.archlucid.*` only |
| Local SQL test env (CI/scripts) | `ARCHIFORGE_SQL_TEST` | `ARCHLUCID_SQL_TEST` |
| Release smoke SQL env | `ARCHIFORGE_SMOKE_SQL` | `ARCHLUCID_SMOKE_SQL` |
| Docker Compose SQL password / DB name (dev sample) | `ArchiForge_*` | `ArchLucid_*` |
| Terraform (`infra/terraform/`) APIM backend URL variable | `archiforge_api_backend_url` | `archlucid_api_backend_url` |

### Migration steps

1. Update **Key Vault** / **App Service** / **Container Apps** configuration and **secret names** so applications receive **`ArchLucid`**, **`ArchLucidAuth`**, and **`ConnectionStrings__ArchLucid`** (environment variable form) as appropriate.
2. Rotate CI and developer shell variables to **`ARCHLUCID_SQL_TEST`**, **`ARCHLUCID_API_TEST_SQL`**, **`ARCHLUCID_SMOKE_SQL`** where used.
3. Rename **`archiforge.json`** → **`archlucid.json`** in CLI workspaces; reinstall or update the global tool so the command **`archlucid`** is on the PATH.
4. Clear or migrate browser **`sessionStorage`** for the operator UI (users may need to sign in again after OIDC key rename).
5. **Webhook / Service Bus consumers** must expect only **`com.archlucid.*`** `event_type` values (legacy **`com.archiforge.*`** strings are no longer published).

### What still intentionally contains the old token

- **Historical** SQL migration filenames (`001_*.sql` …) are unchanged.
- **Deployed** RLS object names (e.g. policy **`ArchiforgeTenantScope`**, predicate **`archiforge_scope_predicate`**) remain until a dedicated database maintenance migration.
- **Terraform** resource **addresses** may still include the historical `archiforge` token until **`terraform state mv`** (see `docs/ARCHLUCID_RENAME_CHECKLIST.md` Phase 7.5). Rename **`terraform.tfvars`** keys **`archiforge_api_backend_url`** → **`archlucid_api_backend_url`** when upgrading the `infra/terraform` root.

### Detection

API and Worker hosts log a **warning** at startup if legacy configuration **sections** are present (ignored). The operator UI logs a **warning** if legacy **`ARCHIFORGE_*`** environment variables are set.
