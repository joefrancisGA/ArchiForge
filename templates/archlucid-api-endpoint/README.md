# ArchLucid API endpoint template

Install locally from the repo root:

```bash
dotnet new install ./templates/archlucid-api-endpoint
```

Scaffold (example name `BillingExport`):

```bash
dotnet new archlucid-api-endpoint -n BillingExport -o ../../artifacts/scaffold-billing
```

Then:

1. Copy `BillingExport/BillingExportController.cs` into `ArchLucid.Api/Controllers/{Area}/`.
2. Fix **namespace** to match the folder (`Admin`, `Planning`, `Authority`, etc.).
3. Replace **route**, **policies** (`ArchLucidPolicies`), and **rate limiting** to match the area.
4. Add application services, contracts, persistence, and tests per **`docs/GOLDEN_CHANGE_PATH.md`**.
5. Regenerate the OpenAPI snapshot when the route is exposed in production builds.

Uninstall:

```bash
dotnet new uninstall ./templates/archlucid-api-endpoint
```
