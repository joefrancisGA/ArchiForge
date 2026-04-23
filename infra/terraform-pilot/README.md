# Terraform pilot profile (canonical entry)

**Objective:** Give operators a **single default Terraform footprint** under `infra/terraform-pilot`: opinionated **cost / sampling** variables and **machine-readable nested stack order** for Azure. This root intentionally creates **no Azure resources** — it is the profile and sequencing contract.

**Multi-root path (opt-in):** Applying each `infra/terraform-*` directory with **its own remote state** remains supported for teams that want separate state files and blast-radius isolation. That workflow is **advanced** — see `nested_infrastructure_roots` in `outputs.tf` / `terraform output`, and [docs/REFERENCE_SAAS_STACK_ORDER.md](../../docs/library/REFERENCE_SAAS_STACK_ORDER.md).

## Default workflow

1. Copy or author a **gitignored** `terraform.tfvars` if you need non-default FinOps knobs (`pilot_monthly_budget_usd`, `app_insights_sampling_percent`, …).
2. From this directory:

   ```bash
   terraform init
   terraform plan
   ```

3. Use **`terraform output`** (especially `nested_infrastructure_roots` and `cost_variables`) when planning applies in downstream roots, or rely on [infra/apply-saas.ps1](../apply-saas.ps1) (default = **pilot profile only**).

4. When you are ready for the **opt-in multi-root** sequential applies, set **`multi_root_apply_opt_in = true`** in tfvars (documentation signal in outputs) and follow the ordered paths in `nested_infrastructure_roots`, **or** run `../apply-saas.ps1 -MultiRoot`.

## Guardrails

- **Never** commit secrets; use Key Vault references per [docs/CONFIGURATION_KEY_VAULT.md](../../docs/library/CONFIGURATION_KEY_VAULT.md).
- **CI** rejects `archiforge` in any `infra/**/*.tf` — keep **ArchLucid** / `archlucid` naming.

## Related

- [docs/deployment/PILOT_PROFILE.md](../../docs/deployment/PILOT_PROFILE.md) — pilot vs production posture.
- [docs/REFERENCE_SAAS_STACK_ORDER.md](../../docs/library/REFERENCE_SAAS_STACK_ORDER.md) — full narrative and advanced table.
