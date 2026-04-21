> **Scope:** Canonical mapping of ArchLucid Azure subscriptions, the regions and tenants they target, and where each ID is consumed (CD pipeline, Terraform, runbooks).

# Azure subscriptions (ArchLucid)

**Last updated:** 2026-04-21
**Owner:** Platform / Operations (decision recorded by repo owner)
**Status:** Single source of truth — every other doc links here. Do not duplicate the production GUID anywhere else in the repo.

---

## 1. Objective

Give platform engineers and CD/CI authors **one** place to look up:

- Which Azure subscription each ArchLucid environment lives in.
- Which **GitHub Environment secret** in [`cd.yml`](../.github/workflows/cd.yml) maps to which subscription.
- Where (and where **not**) the subscription ID is allowed to appear in source.

This file replaces the prior "ask a person" pattern and resolves item 1 of [`PENDING_QUESTIONS.md`](PENDING_QUESTIONS.md).

---

## 2. Assumptions

- ArchLucid runs in **two** dedicated Azure subscriptions: one for **staging**, one for **production**. This was approved on 2026-04-21 — see resolved row in [`PENDING_QUESTIONS.md`](PENDING_QUESTIONS.md).
- Authentication to both subscriptions is **OIDC-only** from GitHub Actions (`azure/login@v2`, `id-token: write`). No long-lived service principal passwords are stored anywhere in the repo. See [`cd.yml`](../.github/workflows/cd.yml) lines 78–83 / 169–174 / 244–249 for the canonical login pattern.
- Subscription IDs are **operational identifiers, not credentials** ([Microsoft Learn — "What are Azure subscriptions"](https://learn.microsoft.com/en-us/azure/cost-management-billing/manage/add-change-subscription-administrator) treats subscription IDs as visible in URLs, ARM IDs, and audit logs). Recording the production GUID in this doc is intentional and safe; what we **never** commit are the **federated client IDs**, **tenant ID**, or anything else that would let a third party assume the OIDC identity.

---

## 3. Constraints

- Per the workspace rule **`Security-Default-Rule-Port-445-Alignment.mdc`**: SMB / port 445 must not be exposed in either subscription.
- Per the rename rule **`ArchLucid-Rename.mdc`**: `infra/**/*.tf` may not contain the substring `archiforge` (CI guard). Subscription configuration in Terraform must use the `archlucid_*` resource label set.
- Per **`Do-The-Work-Yourself.mdc`** and **"All infrastructure must be representable in Terraform"** (user rule): when the production stack is wired, the subscription ID is consumed by Terraform via the standard `azurerm` provider `subscription_id` attribute (or the `ARM_SUBSCRIPTION_ID` env var that the CD pipeline already exports through `azure/login@v2`). It is **not** committed to a checked-in tfvars file.
- Each environment must have its own GitHub Environment in the repository settings (`staging`, `production`) with **required reviewers** for production. Subscription IDs live as **environment-scoped secrets**, not repository-wide secrets.

---

## 4. Subscription map

| Environment | Subscription ID | Tenant | Default region | OIDC federated client | GitHub Environment | Status |
|---|---|---|---|---|---|---|
| **Production** | **`aab65184-5005-4b0d-a884-9e28328630b1`** | (record in env secret `AZURE_TENANT_ID`) | **`centralus`** | (record in env secret `AZURE_CLIENT_ID`) | `production` | **Live** — recorded 2026-04-21. Subscription is initially empty; wire stacks per [`REFERENCE_SAAS_STACK_ORDER.md`](REFERENCE_SAAS_STACK_ORDER.md) **after staging is green**. |
| **Staging** | (existing pre-rename subscription used in planning sessions) | (env secret `AZURE_TENANT_ID`) | `centralus` (override only with documented data-residency reason) | (env secret `AZURE_CLIENT_ID`) | `staging` | **Live** — wired ahead of production per the same resolved decision. |
| **Greenfield CI test** (quarterly) | dedicated empty subscription used by [`cd-saas-greenfield.yml`](../.github/workflows/cd-saas-greenfield.yml) | env secret `AZURE_GREENFIELD_TENANT_ID` | `eastus2` (set in workflow `env.REGION`) | env secret `AZURE_GREENFIELD_CLIENT_ID` | (workflow-scoped) | **Draft** — workflow runs `workflow_dispatch` only until OIDC is configured. |

> **Default region rationale (Central US):** matches the locked decision recorded in [`PENDING_QUESTIONS.md`](PENDING_QUESTIONS.md) (Resolved 2026-04-21) and the default in [`infra/terraform-container-apps/variables.tf`](../infra/terraform-container-apps/variables.tf). Regional overrides must be justified in the environment README and approved by the data-residency owner.

---

## 5. Where the subscription ID is consumed

### 5.1 CD pipeline (GitHub Actions)

[`cd.yml`](../.github/workflows/cd.yml) reads `AZURE_SUBSCRIPTION_ID` from the **GitHub Environment** matching `inputs.target` (`staging` or `production`):

```yaml
- name: Azure login (OIDC)
  uses: azure/login@v2
  with:
    client-id: ${{ secrets.AZURE_CLIENT_ID }}
    tenant-id: ${{ secrets.AZURE_TENANT_ID }}
    subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
```

This appears **eight times** across the build, plan, apply, deploy, smoke, rollback, and manual-rollback jobs — all of them resolve the same per-environment secret.

**Operator action when wiring production for the first time:**

1. Open the repository's **Settings → Environments → `production`**.
2. Add (or update) the secret **`AZURE_SUBSCRIPTION_ID`** with value **`aab65184-5005-4b0d-a884-9e28328630b1`**.
3. Confirm the sibling secrets (`AZURE_TENANT_ID`, `AZURE_CLIENT_ID`) are populated and that the OIDC federated credential on the App Registration includes the `production` environment as a subject claim.
4. Add **required reviewers** on the `production` environment so a human approves every deploy.

### 5.2 Terraform (multi-root path)

[`REFERENCE_SAAS_STACK_ORDER.md`](REFERENCE_SAAS_STACK_ORDER.md) describes the apply order. For the **multi-root** path the subscription ID is supplied to the `azurerm` provider in one of three ways — pick the one that matches your CI/CD posture:

| Method | When to use | Notes |
|---|---|---|
| **`ARM_SUBSCRIPTION_ID` env var** (set by `azure/login@v2`) | Default — the CD pipeline already exports this after OIDC login | Zero repo footprint; no tfvars edit |
| `provider "azurerm" { subscription_id = "…" }` referencing `var.azure_subscription_id` | When applying outside CI (e.g. manual operator backfill) | Pass via `-var` or `TF_VAR_azure_subscription_id`; **never** commit a tfvars file containing the prod GUID |
| Backend block `subscription_id` (rare) | Only if the **state** account lives in a different subscription | Document the exception in the root README |

The example `infra/environments/prod.example.tfvars` line `# azure_subscription_id = "00000000-0000-0000-0000-000000000000"` is a **placeholder** — do not replace the zero-GUID with the real value in source. Operators copy `prod.example.tfvars` → `production.tfvars` (gitignored) and substitute locally if they need the manual path.

### 5.3 Runbooks and ad-hoc tooling

- [`docs/runbooks/CMK_ENCRYPTION.md`](runbooks/CMK_ENCRYPTION.md) and [`docs/security/MANAGED_IDENTITY_SQL_BLOB.md`](security/MANAGED_IDENTITY_SQL_BLOB.md) reference `subscription_id` as a placeholder; they remain placeholder-only (operators substitute at run time).
- `az account set --subscription "aab65184-5005-4b0d-a884-9e28328630b1"` is the canonical CLI invocation when an operator needs to scope a manual session to production.

---

## 6. Data flow

```
                ┌────────────────────────────────────────────┐
                │ GitHub Environment: production             │
                │  • AZURE_SUBSCRIPTION_ID  (this doc, §4)   │
                │  • AZURE_TENANT_ID         (operator)      │
                │  • AZURE_CLIENT_ID         (operator)      │
                └─────────────────────┬──────────────────────┘
                                      │ workflow_dispatch (target=production)
                                      ▼
                ┌────────────────────────────────────────────┐
                │ .github/workflows/cd.yml                   │
                │  azure/login@v2 (OIDC)                     │
                │  → ARM_SUBSCRIPTION_ID exported to shell   │
                └─────────────────────┬──────────────────────┘
                                      │
        ┌─────────────────────────────┼─────────────────────────────┐
        ▼                             ▼                             ▼
 terraform plan/apply        az containerapp update        scripts/ci/cd-post-deploy-verify.sh
 (provider azurerm           (revision rollout +           (health/ready, OpenAPI, version)
  consumes ARM_*)             optional canary split)
```

The same flow runs against the staging subscription when `inputs.target=staging`; the GitHub Environment switch is the only difference.

---

## 7. Security model

| Concern | Control |
|---|---|
| Credential exposure | OIDC federated identity per environment; no client secrets stored anywhere. |
| Cross-environment blast radius | Subscriptions are **physically separate** — a misconfiguration in staging cannot reach production resources. |
| Approval gating | `production` GitHub Environment requires reviewers; the CD pipeline waits at the `environment: production` step until approved. |
| Audit | Every `azurerm` action is logged in Azure Activity Log; CD job IDs are correlated via the `IMAGE_TAG = github.sha` tag on Container Apps revisions. |
| Tenant residency | `centralus` default; deviations documented per environment. |
| Subscription ID disclosure | Treated as **non-secret operational ID** (Microsoft Learn position). Still kept in one canonical doc to avoid sprawl, not because it is sensitive. |

---

## 8. Operational considerations

- **First production apply:** staging must be green (CD pipeline has at least one successful deploy + smoke result against `staging`) before the `production` environment is unlocked. This was an explicit owner constraint in the resolved decision.
- **State backend:** the production Terraform state lives in its own storage account inside the production subscription (or a dedicated tooling subscription if the operator chooses to centralize state). See `infra/terraform-pilot/README.md` and [`FIRST_AZURE_DEPLOYMENT.md`](FIRST_AZURE_DEPLOYMENT.md) §"Example: backend block".
- **Cost ownership:** the production subscription's cost/budget alerts are the responsibility of the production runbook (`docs/runbooks/INFRASTRUCTURE_OPS.md` is the entry point).
- **Subscription change procedure:** if the production subscription ever needs to change (acquisition, vendor migration), update **only this doc** plus the GitHub Environment secret. Do **not** add ID literals elsewhere in the repo as part of the change.

---

## 9. Related

- [`PENDING_QUESTIONS.md`](PENDING_QUESTIONS.md) — resolved row "Production Azure subscription ID" links here.
- [`REFERENCE_SAAS_STACK_ORDER.md`](REFERENCE_SAAS_STACK_ORDER.md) — Terraform apply order.
- [`FIRST_AZURE_DEPLOYMENT.md`](FIRST_AZURE_DEPLOYMENT.md) — preflight checklist.
- [`DEPLOYMENT_CD_PIPELINE.md`](DEPLOYMENT_CD_PIPELINE.md) — CD pipeline contract and required secrets.
- [`.github/workflows/cd.yml`](../.github/workflows/cd.yml) — canonical OIDC login pattern.
- [`.github/workflows/cd-saas-greenfield.yml`](../.github/workflows/cd-saas-greenfield.yml) — quarterly greenfield smoke (separate subscription).
