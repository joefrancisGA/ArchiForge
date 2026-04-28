> **Scope:** Staging trial funnel validation checklist — manual and automated steps to confirm the trial path works on staging before production release.

# Staging trial funnel validation checklist

**Audience:** Pre-release validation. Run this checklist against `https://staging.archlucid.net` before marking a release candidate as production-ready.

**Related docs:**
- [TRIAL_FUNNEL_END_TO_END.md](TRIAL_FUNNEL_END_TO_END.md) — full architecture map
- [TRIAL_END_TO_END.md](TRIAL_END_TO_END.md) — Playwright live acceptance suite
- [TRIAL_FUNNEL.md](TRIAL_FUNNEL.md) — Prometheus observability runbook

---

## Prerequisites

| Requirement | How to verify |
|-------------|--------------|
| Staging API is healthy | `Invoke-RestMethod https://staging.archlucid.net/health/ready` returns `Healthy` |
| Staging UI loads | Browser navigates to `https://staging.archlucid.net` without errors |
| DNS resolves | `nslookup staging.archlucid.net` returns expected Front Door CNAME |
| TLS certificate valid | Browser padlock shows valid certificate (not self-signed, not expired) |
| Billing provider configured | Staging uses `Billing:Provider=Noop` or Stripe TEST keys (never live keys on staging) |

---

## Validation steps

### Phase 1: Landing and signup (anonymous)

| # | Step | Expected | Pass |
|---|------|----------|------|
| 1.1 | Navigate to `https://staging.archlucid.net` | Marketing home page loads; no console errors | [ ] |
| 1.2 | Click **Sign up** / navigate to `/signup` | Signup form renders with email, name, and organization fields | [ ] |
| 1.3 | Submit signup form with a test email | Registration succeeds; redirected to verification or operator home (depending on auth mode) | [ ] |
| 1.4 | Verify email (if `Auth:Trial:Modes=LocalIdentity`) | Verification page accepts the code; session is established | [ ] |
| 1.5 | After auth, operator home page loads | Home page renders; trial banner is visible; sample run link is present | [ ] |

### Phase 2: Sample run experience (first-value path)

| # | Step | Expected | Pass |
|---|------|----------|------|
| 2.1 | Click sample run link from home page | Run detail page loads with simulator-generated results | [ ] |
| 2.2 | Click **Finalize** / commit the sample run | Commit succeeds; manifest is generated; version badge appears | [ ] |
| 2.3 | Navigate to manifest view | Manifest renders with decisions, findings, and metadata sections | [ ] |
| 2.4 | Download DOCX artifact | DOCX file downloads; opens in Word/LibreOffice without corruption | [ ] |

### Phase 3: Operator flow (new run)

| # | Step | Expected | Pass |
|---|------|----------|------|
| 3.1 | Navigate to `/runs/new` | New run wizard renders; contextual help info icon is present | [ ] |
| 3.2 | Submit an architecture request (use a template or free-text) | Run is created; pipeline status page shows agent tasks | [ ] |
| 3.3 | Wait for agent execution (simulator: < 10s) | All agent tasks complete; green status indicators | [ ] |
| 3.4 | Finalize the run | Commit succeeds; manifest version increments | [ ] |

### Phase 4: Trial metering and limits

| # | Step | Expected | Pass |
|---|------|----------|------|
| 4.1 | Create runs until trial write limit is reached | API returns `402 Payment Required` with `application/problem+json` body | [ ] |
| 4.2 | Verify trial banner shows upgrade CTA | Banner text includes "upgrade" or "subscribe" language | [ ] |

### Phase 5: Checkout and conversion (Noop/Stripe TEST)

| # | Step | Expected | Pass |
|---|------|----------|------|
| 5.1 | Click upgrade CTA | Checkout URL is returned (Noop: immediate; Stripe TEST: redirects to Stripe test checkout) | [ ] |
| 5.2 | Complete checkout (Noop: automatic; Stripe: use test card `4242...`) | Trial status transitions to `Converted` | [ ] |
| 5.3 | Verify trial banner is hidden after conversion | No trial/upgrade banner on home page | [ ] |
| 5.4 | Verify write limits are lifted | New runs can be created without `402` | [ ] |

### Phase 6: Observability and audit

| # | Step | Expected | Pass |
|---|------|----------|------|
| 6.1 | Check `/v1/audit` for `TrialProvisioned` event | Event exists with correct `TenantId` and timestamp | [ ] |
| 6.2 | Check Prometheus metrics (if available) | `archlucid_trial_registrations_total` counter incremented | [ ] |
| 6.3 | Check Application Insights (if configured) | Request telemetry shows signup and commit traces | [ ] |

### Phase 7: Security and edge cases

| # | Step | Expected | Pass |
|---|------|----------|------|
| 7.1 | Attempt signup with an already-registered email | API returns appropriate error (conflict or idempotent success) | [ ] |
| 7.2 | Attempt to access operator routes without auth | Redirect to signin page; no data leakage | [ ] |
| 7.3 | Verify CORS headers | API responses include correct `Access-Control-Allow-Origin` for staging UI origin | [ ] |
| 7.4 | Verify rate limiting | Rapid-fire registration requests eventually return `429 Too Many Requests` | [ ] |

---

## Automated validation

Run the live API Playwright suite against staging:

```powershell
$env:LIVE_E2E_API_BASE_URL = "https://staging.archlucid.net"
$env:LIVE_E2E_HARNESS_SECRET = "<staging-harness-secret>"
cd archlucid-ui
npx playwright test live-api-trial-end-to-end.spec.ts
```

The CI workflow `ui-e2e-live.yml` runs this suite as part of the staging deployment pipeline.

---

## Sign-off

| Role | Name | Date | Result |
|------|------|------|--------|
| Engineering | | | |
| Product | | | |
| Security | | | |

---

## Notes

- This checklist covers the **buyer/prospect** path only. Internal operator onboarding (Docker, .NET SDK) has a separate validation path via `release-smoke.ps1`.
- Staging should use **Stripe TEST keys** or **Noop billing** — never live payment processing.
- Clean up test tenants after validation using the cleanup procedure in [TRIAL_END_TO_END.md](TRIAL_END_TO_END.md#cleaning-up-test-tenants).
