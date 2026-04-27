> **Scope:** GitHub repository settings required for staging CD and hosted probes — no secret values; key names and descriptions only.

# Staging GitHub environment setup

**Purpose:** Document the GitHub repository settings (environment secrets, repository variables) required to deploy ArchLucid to `staging.archlucid.net` via the CD pipeline and enable hosted probes.

**Last updated:** 2026-04-26

**Cross-references:**
- [AZURE_SUBSCRIPTIONS.md](../library/AZURE_SUBSCRIPTIONS.md) — canonical subscription map
- [DEPLOYMENT_CD_PIPELINE.md](../library/DEPLOYMENT_CD_PIPELINE.md) — CD workflow secrets and smoke behavior
- [STAGING_DEPLOYMENT_CHECKLIST.md](STAGING_DEPLOYMENT_CHECKLIST.md) — full deployment checklist
- [REFERENCE_SAAS_STACK_ORDER.md](../library/REFERENCE_SAAS_STACK_ORDER.md) — Terraform apply order

---

## 1. GitHub environment: `staging`

Create the environment at **Settings → Environments → New environment → `staging`**.

### 1.1 Environment secrets

| Secret name | Description | Source |
|-------------|-------------|--------|
| `AZURE_CLIENT_ID` | Service principal / federated identity client ID for the DEV subscription OIDC login | Entra app registration for staging |
| `AZURE_TENANT_ID` | Azure AD tenant ID | Entra admin center |
| `AZURE_SUBSCRIPTION_ID` | DEV subscription ID (see [AZURE_SUBSCRIPTIONS.md](../library/AZURE_SUBSCRIPTIONS.md) §4) | Azure Portal |
| `ACR_LOGIN_SERVER` | Azure Container Registry login server (e.g., `archluciddev.azurecr.io`) | `az acr show --name <registry> --query loginServer -o tsv` |
| `AZURE_RESOURCE_GROUP` | Resource group containing Container Apps, SQL, and supporting resources | Azure Portal |
| `CONTAINER_APP_API_NAME` | Name of the API Container App resource | `az containerapp list -g <rg> --query "[?name contains 'api'].name" -o tsv` |

### 1.2 Optional environment secrets

| Secret name | Description | When needed |
|-------------|-------------|-------------|
| `SMOKE_TEST_BASE_URL` | Public API base URL for post-deploy smoke (e.g., `https://staging.archlucid.net`) | Set to enable post-deploy verification in CD |
| `STRIPE_TEST_KEY` | Stripe test-mode secret key for trial funnel e2e | Required by `trial-funnel-test-mode.yml` |
| `STAGING_ONCALL_WEBHOOK_URL` | Slack/Teams/PagerDuty webhook for failure notifications | Optional; workflow is no-op if unset |

### 1.3 Environment protection rules (recommended)

| Rule | Value |
|------|-------|
| Required reviewers | At least 1 (owner or designated operator) |
| Wait timer | 0 minutes (manual approval is sufficient) |
| Deployment branches | `main` only |

---

## 2. Repository variables

Set at **Settings → Secrets and variables → Actions → Variables** (repository-scoped, not environment-scoped).

| Variable name | Value | Used by |
|---------------|-------|---------|
| `AUTO_DEPLOY_STAGING_MERGE` | `true` | `cd-staging-on-merge.yml` — enables automatic staging deploy on push to `main` |
| `ARCHLUCID_STAGING_BASE_URL` | `https://staging.archlucid.net` | `hosted-saas-probe.yml` — scheduled health probes |
| `IMAGE_TAG` | *(leave empty)* | CD workflows — defaults to commit SHA when empty |
| `ARCHLUCID_STAGING_SIGNUP_BASE_URL` | `https://signup.staging.archlucid.net` | `trial-funnel-test-mode.yml` — Playwright e2e base URL |

---

## 3. Verification

After configuring, verify the settings:

```bash
# List environment secrets (names only, not values)
gh api repos/{owner}/{repo}/environments/staging/secrets --jq '.secrets[].name'

# List repository variables
gh api repos/{owner}/{repo}/actions/variables --jq '.variables[] | "\(.name)=\(.value)"'
```

---

## 4. OIDC federation setup (if not already configured)

The CD pipeline uses `azure/login@v2` with OIDC (no long-lived passwords). If the federated credential is not already configured on the Entra app registration:

1. In Entra admin center → App registrations → select the staging app → Certificates & secrets → Federated credentials → Add credential
2. Set:
   - **Federated credential scenario:** GitHub Actions deploying Azure resources
   - **Organization:** `joefrancisGA`
   - **Repository:** `ArchLucid`
   - **Entity type:** Environment
   - **Environment name:** `staging`
3. Save. The `AZURE_CLIENT_ID` secret in the GitHub `staging` environment must match this app registration's Application (client) ID.

---

## Related documents

| Doc | Use |
|-----|-----|
| [AZURE_SUBSCRIPTIONS.md](../library/AZURE_SUBSCRIPTIONS.md) | Subscription IDs and CD secret mapping |
| [DEPLOYMENT_CD_PIPELINE.md](../library/DEPLOYMENT_CD_PIPELINE.md) | CD workflow details |
| [STAGING_DEPLOYMENT_CHECKLIST.md](STAGING_DEPLOYMENT_CHECKLIST.md) | Full staging checklist |
