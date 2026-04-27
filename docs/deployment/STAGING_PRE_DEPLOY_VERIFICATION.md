> **Scope:** Pre-deployment verification checklist for `staging.archlucid.net` — confirms all prerequisites before running `terraform apply` or CD pipeline.

# Staging pre-deploy verification (`staging.archlucid.net`)

**Purpose:** Confirm all prerequisites are in place before deploying the ArchLucid SaaS trial funnel to staging. Use this checklist before running `terraform apply` or triggering the CD pipeline.

**Last updated:** 2026-04-26

---

## 1. Domain alignment verification

| Check | Command | Expected result |
|-------|---------|-----------------|
| No `archlucid.com` in non-archive source | `rg "archlucid\.com" --glob "*.{cs,ts,tsx,json,yml,yaml,ps1}" --glob "!**/archive/**" -l` | Only CloudEvents files (`com.archlucid.*` URIs) |
| No `archlucid.com` in active docs | `rg "archlucid\.com" docs/ --glob "!docs/archive/**" -l` | Only files containing `com.archlucid.*` CloudEvents URIs |
| `appsettings.json` BaseUrl | Inspect `ArchLucid.Api/appsettings.json` → `ArchLucid:PublicSite:BaseUrl` | `https://archlucid.net` |
| CLI staging URL | Inspect `ArchLucid.Cli/Commands/TrialSmokeCommandOptions.cs` → `StagingApiBaseUrl` | `https://staging.archlucid.net` |

**Status:** COMPLETED 2026-04-26. All source, docs, CI, and schemas updated.

---

## 2. Azure resource prerequisites

| Resource | Check command | Expected state |
|----------|---------------|----------------|
| **DEV subscription** | `az account show --subscription <DEV_SUB_ID>` | Active |
| **ACR** | `az acr show --name <registry> --query loginServer -o tsv` | Returns login server URL |
| **Entra app registrations** | Entra admin center → App registrations | API + UI registrations exist with redirect URIs for `https://staging.archlucid.net` |
| **DNS zone** | `nslookup staging.archlucid.net` | Resolves (CNAME to Front Door endpoint, or placeholder A record) |
| **SQL Server** | `az sql server list -g <rg> --query "[].name" -o tsv` | Server exists in DEV subscription |
| **Key Vault** | `az keyvault show --name <vault> --query "properties.vaultUri" -o tsv` | Returns vault URI |

**Status:** Owner confirmed all resources exist (2026-04-26).

---

## 3. Terraform validation

Run from the repo root against each infrastructure root. These commands are **read-only** and do not create resources.

```powershell
# Validate all roots
$roots = @(
    "infra/terraform-private",
    "infra/terraform-keyvault",
    "infra/terraform-sql-failover",
    "infra/terraform-storage",
    "infra/terraform-servicebus",
    "infra/terraform-entra",
    "infra/terraform-container-apps",
    "infra/terraform-edge",
    "infra/terraform-monitoring"
)

foreach ($root in $roots) {
    Write-Host "--- Validating $root ---"
    Push-Location $root
    terraform init -backend=false
    terraform validate
    Pop-Location
}
```

For a full plan (requires Azure credentials):

```powershell
# From infra/terraform-pilot (single entry point)
cd infra/terraform-pilot
terraform init
terraform plan -out=staging.tfplan
```

**Status:** PENDING — operator must run with Azure credentials.

---

## 4. GitHub environment configuration

See [STAGING_GITHUB_ENVIRONMENT_SETUP.md](STAGING_GITHUB_ENVIRONMENT_SETUP.md) for the complete list of secrets and variables.

| Setting | Status |
|---------|--------|
| `staging` environment created | Verify in **Settings → Environments** |
| `AZURE_CLIENT_ID` secret set | Verify secret exists (not value) |
| `AZURE_TENANT_ID` secret set | Verify |
| `AZURE_SUBSCRIPTION_ID` secret set | Verify |
| `ACR_LOGIN_SERVER` secret set | Verify |
| `AZURE_RESOURCE_GROUP` secret set | Verify |
| `CONTAINER_APP_API_NAME` secret set | Verify |
| `AUTO_DEPLOY_STAGING_MERGE` variable = `true` | Verify in **Settings → Variables** |
| `ARCHLUCID_STAGING_BASE_URL` variable = `https://staging.archlucid.net` | Verify |

**Status:** PENDING — operator must configure in GitHub repository settings.

---

## 5. CORS configuration

The `Cors:AllowedOrigins` array must include `https://staging.archlucid.net` for the staging deployment. This is set via **Container App environment variable override**, not in the checked-in `appsettings.json` (which contains only `localhost:3000` for development).

| Configuration method | Value |
|---------------------|-------|
| Container App env var | `Cors__AllowedOrigins__0=https://staging.archlucid.net` |
| Or via Terraform | `infra/terraform-container-apps/variables.tf` → `cors_allowed_origins` |

Verify after deploy: `BillingProductionSafetyRules` and `ArchLucidConfigurationRules.CollectErrors` require non-empty, non-wildcard origins.

**Status:** PENDING — operator must configure in Container App or Terraform variables.

---

## 6. Entra redirect URIs

The Entra app registration for the UI must include:

| Redirect URI | Type |
|--------------|------|
| `https://staging.archlucid.net/auth/callback` | Web |
| `https://staging.archlucid.net/api/auth/callback/azure-ad` | Web (if using NextAuth) |

Verify in Entra admin center → App registrations → Authentication → Redirect URIs.

**Status:** PENDING — operator must verify in Entra admin center.

---

## 7. Post-deploy smoke tests

After the first successful deploy, run these checks:

```bash
# Health probes
curl -fsS https://staging.archlucid.net/health/live
# Expected: 200

curl -fsS https://staging.archlucid.net/health/ready
# Expected: 200, JSON body with top-level "Healthy"

# Version endpoint
curl -fsS https://staging.archlucid.net/version
# Expected: 200, JSON with build info

# UI accessibility
# Open https://staging.archlucid.net/signup in browser
# Expected: Signup page renders, Entra login flow works

# CLI smoke (optional)
dotnet run --project ArchLucid.Cli -- trial smoke --staging --org "Smoke Test" --email "smoke@example.com"
# Expected: PASS line with correlation ID
```

---

## 8. Rollback procedure

If the staging deployment fails or produces errors:

1. **Container App revision rollback:**

```bash
# List revisions
az containerapp revision list -g <rg> -n <api-app> -o table

# Activate previous revision
az containerapp revision activate -g <rg> -n <api-app> --revision <previous-revision-name>

# Deactivate failed revision
az containerapp revision deactivate -g <rg> -n <api-app> --revision <failed-revision-name>
```

2. **Database rollback:** Forward-fix preferred. If schema migration caused the issue, see [MIGRATION_ROLLBACK.md](../runbooks/MIGRATION_ROLLBACK.md).

3. **DNS rollback:** If Front Door is misconfigured, revert the custom domain binding in `infra/terraform-edge` and re-apply.

---

## 9. Deployment execution order

When all checks above are green:

1. Apply Terraform roots in order per [REFERENCE_SAAS_STACK_ORDER.md](../library/REFERENCE_SAAS_STACK_ORDER.md)
2. Build and push Docker images to ACR
3. Update Container App revisions (API, worker, UI)
4. Run post-deploy smoke tests (§7)
5. Set `ARCHLUCID_STAGING_BASE_URL` repo variable if not already set
6. Trigger `hosted-saas-probe.yml` manually to verify scheduled probes work

---

## Related documents

| Doc | Use |
|-----|-----|
| [STAGING_DEPLOYMENT_CHECKLIST.md](STAGING_DEPLOYMENT_CHECKLIST.md) | Detailed operator checklist |
| [STAGING_GITHUB_ENVIRONMENT_SETUP.md](STAGING_GITHUB_ENVIRONMENT_SETUP.md) | GitHub secrets and variables |
| [REFERENCE_SAAS_STACK_ORDER.md](../library/REFERENCE_SAAS_STACK_ORDER.md) | Terraform apply order |
| [AZURE_SUBSCRIPTIONS.md](../library/AZURE_SUBSCRIPTIONS.md) | Subscription IDs |
| [DEPLOYMENT_CD_PIPELINE.md](../library/DEPLOYMENT_CD_PIPELINE.md) | CD pipeline details |
| [MIGRATION_ROLLBACK.md](../runbooks/MIGRATION_ROLLBACK.md) | Database rollback |
