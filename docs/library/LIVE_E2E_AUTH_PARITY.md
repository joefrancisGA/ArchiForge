> **Scope:** Live E2E — auth parity (DevelopmentBypass vs ApiKey vs JWT) - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Live E2E — auth parity (DevelopmentBypass vs ApiKey vs JWT)

## 1. Objective

Record which **`live-api-*.spec.ts`** scenarios run under **PR CI** vs **nightly**, and how **DevelopmentBypass**, **ApiKey**, and **JwtBearer (local PEM)** differ, so operators know what integration proof exists for production-like behavior.

## 2. Assumptions

- **Simulator** is orthogonal to auth: `AgentExecution:Mode=Simulator` can pair with either auth mode.
- **JWT (local RSA PEM)** live gates mint RS256 tokens in CI (`scripts/ci/mint_ci_jwt.py`); **Entra** metadata path remains the production default (`Authority` + OIDC) when **`JwtSigningPublicKeyPemPath`** is unset.
- **Anonymous health:** `GET /health/ready` stays **`AllowAnonymous`** in all modes.

## 3. Constraints

- **ApiKey mode** uses fixed principals (**`ApiKeyAdmin`** / **`ApiKeyReadOnly`**) — true multi-user segregation is a **JWT** concern; body field **`reviewedBy`** still enforces governance segregation vs stored **`RequestedBy`**.
- **Nightly** workflows do not run on **forks** (`github.event.repository.fork == false`).

## 4. Architecture overview

**Nodes:** GitHub Actions runners, SQL Server service container, `ArchLucid.Api`, Playwright + Next.js `webServer`, `LIVE_API_*` env vars.

**Edges:** CI job → API env (`ArchLucidAuth:Mode`, `Authentication:ApiKey:*`, optional `JwtSigningPublicKeyPemPath`) → Playwright → `live-api-client` (optional `X-Api-Key` or `Authorization: Bearer`).

**Flows:**

```mermaid
flowchart LR
  PR[PR ci.yml]
  N[nightly workflow]
  API[ArchLucid.Api]
  PW[Playwright live-api-*]
  PR --> API --> PW
  N --> API --> PW
```

## 5. Component breakdown

| Artifact | Role |
|----------|------|
| **`ci.yml` → `ui-e2e-live`** | Full **`live-api-*.spec.ts`** under DevelopmentBypass + `DevelopmentBypassAll`; no `LIVE_API_KEY`. |
| **`ci.yml` → `ui-e2e-live-apikey`** | ApiKey API + subset: **`live-api-apikey-auth`**, **`live-api-journey`**, **`live-api-negative-paths`**. |
| **`ci.yml` → `ui-e2e-live-jwt`** | JwtBearer + local public PEM + subset (same three specs as ApiKey subset); **`continue-on-error: true`**. |
| **`live-e2e-nightly.yml`** | Scheduled + `workflow_dispatch`: **full** suite ×3 (DevelopmentBypass + ApiKey + JwtBearer DBs). |
| **`e2e/helpers/live-api-client.ts`** | Injects `X-Api-Key` when `LIVE_API_KEY` set (unless `LIVE_JWT_TOKEN` wins); Bearer when JWT configured; exports **`liveAuthActorName`**, **`livePeerReviewerActorName`**. |

## 6. Auth mode matrix (spec files)

| Spec | PR `ui-e2e-live` | PR `ui-e2e-live-apikey` | PR `ui-e2e-live-jwt` | Nightly (each mode) |
|------|------------------|-------------------------|----------------------|----------------------|
| `live-api-apikey-auth.spec.ts` | skipped (no key) | ✅ | skipped (no API key) | ✅ (ApiKey job only meaningful; Bypass/Jwt runs skip tests) |
| `live-api-jwt-auth.spec.ts` | skipped (no JWT) | skipped (prefers JWT when set; use ApiKey-only job) | ✅ | ✅ (Jwt job meaningful) |
| `live-api-journey.spec.ts` | ✅ | ✅ | ✅ | ✅ |
| `live-api-negative-paths.spec.ts` | ✅ | ✅ | ✅ | ✅ |
| All other `live-api-*.spec.ts` | ✅ | — | — | ✅ |

## 7. Data flow (headers)

1. **DevelopmentBypass + no `LIVE_API_KEY` / `LIVE_JWT_TOKEN`:** helpers send JSON/Accept only; API synthesizes **Developer** admin principal.
2. **ApiKey + `LIVE_API_KEY`:** helpers add **`X-Api-Key`**; submitter for governance is **`ApiKeyAdmin`**; **`liveAuthActorName`** matches for self-approval tests.
3. **JWT + `LIVE_JWT_TOKEN`:** helpers add **`Authorization: Bearer`** (overrides API key when both set); submitter name is **`LIVE_JWT_ACTOR_NAME`** (default **`JwtE2eAdmin`**) — must match JWT **`name`** claim. Next proxy: set **`ARCHLUCID_PROXY_BEARER_TOKEN`** to the same token when UI hits the BFF without a browser Bearer.

## 8. Security model

- CI keys are **throwaway** strings in workflow env (not secrets). Do not reuse outside ephemeral CI.
- **`LIVE_API_KEY_READONLY`** exercises least privilege (read list allowed; create run **403**).

## 9. Operational considerations

- **Cost:** Nightly runs the full live suite three times (three DBs, three API boots). Adjust cron if spend matters.
- **Flakes:** Full nightly is the pressure valve if PR subset is green but a rare spec regresses.
- **Artifacts:** Nightly uploads API logs **on failure** only (`live-e2e-nightly.yml`).

## 10. Related links

- [LIVE_E2E_AUTH_ASSUMPTIONS.md](LIVE_E2E_AUTH_ASSUMPTIONS.md)
- [LIVE_E2E_JWT_SETUP.md](LIVE_E2E_JWT_SETUP.md)
- [LIVE_E2E_HAPPY_PATH.md](LIVE_E2E_HAPPY_PATH.md)
- [TEST_STRUCTURE.md](TEST_STRUCTURE.md)
- [TEST_EXECUTION_MODEL.md](TEST_EXECUTION_MODEL.md)
- [runbooks/API_KEY_ROTATION.md](../runbooks/API_KEY_ROTATION.md)

## 11. Addendum — quality assessment (2026-04-14)

**Priority 1** (“Production-like live gates”) from the archived assessment **`docs/archive/QUALITY_ASSESSMENT_2026_04_14.md`** (stub **`docs/QUALITY_ASSESSMENT_2026_04_14.md`**): ApiKey subset on PR + full matrix nightly + documentation; **JWT with local PEM** added as PR subset (**`ui-e2e-live-jwt`**, non-blocking) + nightly full suite job. **Entra OIDC** in CI remains optional (use **`Authority`** when not using **`JwtSigningPublicKeyPemPath`**).
