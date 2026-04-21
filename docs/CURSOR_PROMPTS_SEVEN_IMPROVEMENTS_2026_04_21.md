# Seven improvements — execution log (2026-04-21)

This file records what was implemented from the seven-improvement prompt set versus what remains **owner-blocked**.

## 1. Reference customer publication

**Shipped in repo:** Table and CI guard already exist (`docs/go-to-market/reference-customers/README.md`, `scripts/ci/check_reference_customer_status.py`).

**Blocked on you:** Customer name, signed reference agreement, and moving a row to **Published** to clear the −15% reference discount gate.

## 2. ADR 0021 Phase 3 (coordinator retirement)

**Not executed:** Full Phase 3 deletes `ICoordinator*` and coordinator audit constants — gated by ADR 0021 soak windows and live E2E.

**Shipped:** [`docs/adr/0022-coordinator-phase3-deferred.md`](adr/0022-coordinator-phase3-deferred.md) tracking placeholder.

## 3. ArchLucid.Api coverage ≥ 79%

**Not executed in this batch:** Strict-profile uplift remains an open workstream (`docs/CODE_COVERAGE.md`).

**Follow-up:** Targeted controller tests in `ArchLucid.Api.Tests` until merged Cobertura meets the gate.

## 4. Azure DevOps PR decoration

**Shipped:** `ArchLucid.Integrations.AzureDevOps`, Worker integration handler, `AzureDevOps` configuration section, tests, [`docs/integrations/AZURE_DEVOPS_PR_DECORATION.md`](integrations/AZURE_DEVOPS_PR_DECORATION.md).

**Blocked on you:** PAT in Key Vault, real `RepositoryId` / `PullRequestId` for a pilot PR (until run→PR mapping is stored in SQL).

## 5. Marketplace + production hostname + Central US

**Shipped:** `infra/terraform-container-apps` default **`location = "centralus"`**, [`docs/REFERENCE_SAAS_STACK_ORDER.md`](REFERENCE_SAAS_STACK_ORDER.md) primary-region note, [`docs/go-to-market/MARKETPLACE_PUBLICATION.md`](go-to-market/MARKETPLACE_PUBLICATION.md) checklist, [`docs/PENDING_QUESTIONS.md`](PENDING_QUESTIONS.md) subscription item aligned to Central US.

**Blocked on you:** Production Azure subscription id, Partner Center go-live, DNS/TLS cutover for `archlucid.com` / `staging.archlucid.com`.

## 6. External pen test + PGP

**Shipped:** [`SECURITY.md`](../SECURITY.md) canonical `security.txt` URL + `.well-known/pgp-key.txt` plan; [`archlucid-ui/public/.well-known/security.txt`](../archlucid-ui/public/.well-known/security.txt).

**Blocked on you:** External assessor SoW (`<<vendor>>`), redacted summary after delivery, generating and publishing a real PGP public key.

## 7. Stryker — Decisioning.Merge + Application.Governance

**Shipped:** `stryker-config.decisioning-merge.json`, `stryker-config.application-governance.json`, `stryker-baselines.json` entries (**55.0** start), `stryker-scheduled.yml` matrix, `refresh_stryker_baselines.py` targets, `stryker_pr_plan.py` rules + self-test, docs (`MUTATION_TESTING_STRYKER.md`, `TEST_STRUCTURE.md`).

**CI:** First scheduled run establishes measured scores; use `python scripts/ci/refresh_stryker_baselines.py` after green (do not run full Stryker matrix locally unless needed).
