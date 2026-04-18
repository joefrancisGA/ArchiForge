# Self-serve trial — end-to-end live acceptance

**Audience:** engineers running the merge-blocking Playwright suite locally, cleaning up after manual runs, or replaying billing harness calls.

**Canonical spec:** [`archlucid-ui/e2e/live-api-trial-end-to-end.spec.ts`](../../archlucid-ui/e2e/live-api-trial-end-to-end.spec.ts)

---

## Objective

Prove **self-serve trial in production shape**: anonymous `POST /v1/register`, SQL-backed tenant + trial rows, operator UI (DevelopmentBypass scope), trial metering (`402` + `application/problem+json`), **Noop** checkout URL, harness-simulated **subscription activation** (same activator as Stripe `checkout.session.completed`), Prometheus funnel counters, and **Converted** trial UI (trial banner hidden).

---

## Preconditions

| Requirement | Notes |
|-------------|--------|
| **ArchLucid.Api** | Sql storage, `ArchLucidAuth:Mode=DevelopmentBypass`, `AgentExecution:Mode=Simulator`, `Billing:Provider=Noop` (typical local `appsettings.Development.json`). |
| **Prometheus scrape** | `Observability:Prometheus:Enabled=true` and `RequireScrapeAuthentication=false` for unattended `/metrics` reads (CI sets this). |
| **Harness secret** | `ArchLucid:E2eHarness:SharedSecret` (≥ 16 chars) on the API **and** `LIVE_E2E_HARNESS_SECRET` in the Playwright environment. In **Production**, harness routes are not supported (`E2eHarnessRules` + controller returns **404** when disabled). |
| **RLS break-glass (Sql + demo seed)** | When `SqlServer:RowLevelSecurity:ApplySessionContext` is **true**, startup demo seed calls `SqlRowLevelSecurityBypassAmbient.Enter()` and requires **`ARCHLUCID_ALLOW_RLS_BYPASS=true`** and **`ArchLucid:Persistence:AllowRlsBypass=true`** (see [SECURITY.md](../SECURITY.md)). CI **`ui-e2e-live`** jobs set both; without them the API exits before binding **5128**, so health checks (`curl`) fail until timeout. |
| **Rate limits** | Anonymous registration uses the `registration` policy; raise `RateLimiting:Registration:PermitLimit` for multi-spec CI on one IP if needed. |

---

## Run locally

1. Start **SQL Server** and apply schema (DbUp on API start when connection string points at the DB).
2. Export configuration (minimal):

   ```bash
   set ARCHLUCID_E2E_HARNESS_SECRET=your-local-secret-at-least-16-chars
   set LIVE_E2E_HARNESS_SECRET=%ARCHLUCID_E2E_HARNESS_SECRET%
   ```

   API (`UserSecrets` or env):

   - `ArchLucid__E2eHarness__SharedSecret=%ARCHLUCID_E2E_HARNESS_SECRET%`
   - `Observability__Prometheus__Enabled=true`
   - `Observability__Prometheus__RequireScrapeAuthentication=false`

3. `dotnet run --project ArchLucid.Api`
4. `cd archlucid-ui && npx playwright test live-api-trial-end-to-end.spec.ts`

---

## Email + integration events

- **`TrialProvisioned`** audit is emitted when the trial row is committed; the **audit decorator** attempts to publish the welcome lifecycle envelope (`TrialLifecycleEmailPublishingAuditDecorator`).
- With **no Service Bus** queue configured, `IIntegrationEventPublisher` is a no-op: the outbox may still **enqueue** when `IntegrationEvents:TransactionalOutboxEnabled=true`, and the hosted outbox loop marks rows processed without an external consumer. **`FakeEmailSender`** is wired for digest/advisory paths but **welcome mail is not guaranteed to hit `FakeEmailSender` in this configuration** — treat **`TrialProvisioned`** as the durable “email intent” signal for CI unless you run a Worker + bus.

---

## Cleaning up test tenants

Test tenants are normal rows in **`dbo.Tenants`** / workspaces / runs. For disposable databases (CI catalog `ArchLucidLiveE2e`), tear down the database or catalog between runs.

For shared dev SQL:

1. Identify **`tenantId`** from Playwright output or `POST /v1/register` response.
2. Delete in dependency order (runs, manifests, billing, audit, tenant) or restore a snapshot — follow your DBA playbook; **do not** expose SMB (445) for backups.

---

## Replaying the billing harness (idempotency)

`POST /v1/e2e/billing/simulate-subscription-activated` calls `BillingWebhookTrialActivator.OnSubscriptionActivatedAsync`, which activates via **`dbo.sp_Billing_Activate`** and **`MarkTrialConvertedAsync`**.

- Use a **fresh** `providerSubscriptionId` (the Noop checkout **`providerSessionId`**) per activation attempt.
- If you need to re-run activation against the same tenant after a successful conversion, **reset tenant billing state** in SQL (subscriptions + tenant trial columns) using a maintenance script — not shipped in product code.

Webhook **idempotency keys** for real Stripe live in `dbo.BillingWebhookEvents`; the harness bypasses HTTP webhooks and does not insert those rows.

---

## Related docs

- [TRIAL_FUNNEL.md](TRIAL_FUNNEL.md) — metrics and audit matrix.
- [V1_RELEASE_CHECKLIST.md](../V1_RELEASE_CHECKLIST.md) — release gate checklist.
- [PRICING_PHILOSOPHY.md](../go-to-market/PRICING_PHILOSOPHY.md) — re-rate gate #3 (planning conversation only).
