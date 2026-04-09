# Deployment runbook — failed deploys and rollback (practical)

**Audience:** operators on call. **Scope:** Azure Container Apps + GitHub CD (see [DEPLOYMENT_CD_PIPELINE.md](DEPLOYMENT_CD_PIPELINE.md)). For schema and data rollback posture, use [runbooks/MIGRATION_ROLLBACK.md](runbooks/MIGRATION_ROLLBACK.md).

---

## 1. Deployment “succeeded” but health / post-deploy validation fails

**Symptoms:** GitHub **Post-deploy validation** job failed, or users see 5xx while the pipeline turned green on earlier jobs.

**Do this first**

1. Open the failed workflow run → **Post-deploy validation** (or **smoke-test**) log. It runs `scripts/ci/cd-post-deploy-verify.sh`: note HTTP codes and the **`/health/ready`** JSON (overall **`status`** and **`entries`**).
2. From a machine that can reach the API (same URL as **`SMOKE_TEST_BASE_URL`**):
   - `GET {base}/health/live` — process up?
   - `GET {base}/health/ready` — which check is **Unhealthy** / **Degraded**? (SQL, blob, schema, etc.)
   - `GET {base}/version` — which **commit** / **informationalVersion** is actually running?

**If automated rollback is on:** set repository variable **`CD_ROLLBACK_ON_SMOKE_FAILURE`** to **`true`** *before* the next deploy so a failed validation deactivates the new **API** and **worker** revisions (see §4 and CD workflow comments).

**If you must fix forward:** resolve the failing dependency (connection string, Key Vault, network, RLS, storage). Redeploy the same or a fixed image tag after config is correct.

**Deeper checks:** [RELEASE_SMOKE.md](RELEASE_SMOKE.md) (local **`release-smoke`**) for a fuller path than CD’s HTTP gate.

---

## 2. Image publish succeeded but Container Apps deploy failed

**Symptoms:** **Build and push images** green; **deploy-container-apps** or **`az containerapp update`** failed (RBAC, quota, bad image digest, wrong RG/name).

**Do this**

1. Read the failing step output (Azure CLI error text).
2. Confirm secrets: **`AZURE_RESOURCE_GROUP`**, **`CONTAINER_APP_API_NAME`**, optional **`CONTAINER_APP_WORKER_NAME`** / **`CONTAINER_APP_UI_NAME`**, **`ACR_LOGIN_SERVER`** — must match the real app names in Azure.
3. Verify the identity used in GitHub (**federated credential**) can update those apps and pull from ACR (**AcrPull** / registry attachment as you configured in Terraform).
4. In Azure Portal → Container App → **Revisions** / **System logs**, or run:
   ```bash
   az containerapp show -g <rg> -n <app> -o table
   az containerapp revision list -g <rg> -n <app> -o table
   ```
5. Re-run the failed job or run **CD** again with **workflow_dispatch** after fixing IAM or naming. The image tag (often **git SHA**) is already in ACR; you do not need to rebuild unless the image itself was wrong.

**Terraform note:** If you use **`terraform apply`** with image variables, a later apply can reset images to tfvars. Align tfvars with the tag you intend, or rely on CLI-only rollouts until the next planned apply ([DEPLOYMENT_CD_PIPELINE.md](DEPLOYMENT_CD_PIPELINE.md)).

---

## 3. How to identify the currently deployed version

| Method | What you learn |
|--------|----------------|
| **`GET https://{api-host}/version`** | Anonymous JSON: **informationalVersion**, **commit** (or equivalent build fields), **environment**. Best single check for “what code is live.” |
| **GitHub Actions run** | **IMAGE_TAG** output / variable (defaults to **`github.sha`** for that deploy). |
| **ACR** | Repository tags on **`archlucid-api`** / **`archlucid-ui`** (e.g. digest or `:abc1234` SHA tag). |
| **Azure CLI** | Image on the active revision: `az containerapp show -g <rg> -n <api-app> --query "properties.template.containers[0].image" -o tsv` |

Repeat for the **worker** app if present (same **`archlucid-api:<tag>`** image, different command).

---

## 4. Manual rollback (no automated revision deactivation)

**Goal:** Traffic should use a **previous healthy revision** (Container Apps **Single** revision mode: deactivate the bad **latest** revision so the platform routes to the prior one).

**Prerequisites:** Azure CLI logged in; **`AZURE_RESOURCE_GROUP`**; app names (**`CONTAINER_APP_API_NAME`**, and **`CONTAINER_APP_WORKER_NAME`** if you roll the worker with the API).

**Option A — GitHub (preferred if configured)**  
Run workflow **CD** → **workflow_dispatch** → **action = rollback**, pick **staging** or **production**. This uses the repo’s OIDC setup and deactivates the current latest API revision (see `.github/workflows/cd.yml` **manual-rollback** job). If you also updated the **worker**, deactivate its latest revision the same way via CLI (Option B) so API and worker stay on matching bits.

**Option B — Azure CLI**

```bash
# API: list revisions, confirm names
az containerapp revision list -g "$RG" -n "$API_APP" -o table

# Deactivate the broken *latest* revision (replace REVISION_NAME)
az containerapp revision deactivate -g "$RG" -n "$API_APP" --revision "$REVISION_NAME"

# Worker (same image family as API — roll back if you rolled forward together)
az containerapp revision list -g "$RG" -n "$WORKER_APP" -o table
az containerapp revision deactivate -g "$RG" -n "$WORKER_APP" --revision "$WORKER_REVISION_NAME"
```

Then **`GET /version`** and **`GET /health/ready`** again.

**Caveats**

- **Schema:** If the bad deploy ran **forward-only migrations**, rolling the container back does not undo SQL. See [runbooks/MIGRATION_ROLLBACK.md](runbooks/MIGRATION_ROLLBACK.md); you may need a **forward fix** instead of only deactivating a revision.
- **UI-only regressions:** Roll back **`archlucid-ui`** tag the same way (`az containerapp update --image` to a known-good tag, or revision management if you use multiple UI revisions).

---

## Related links

| Topic | Document |
|--------|----------|
| CD jobs, secrets, post-deploy script | [DEPLOYMENT_CD_PIPELINE.md](DEPLOYMENT_CD_PIPELINE.md) |
| Umbrella deploy / rollback story | [DEPLOYMENT.md](DEPLOYMENT.md) |
| Local / release smoke depth | [RELEASE_SMOKE.md](RELEASE_SMOKE.md) |
| SQL / migration rollback | [runbooks/MIGRATION_ROLLBACK.md](runbooks/MIGRATION_ROLLBACK.md) |
