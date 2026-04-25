> **Scope:** Operators exercising Stripe Test mode against staging (checkout, webhooks, SQL) without code changes — not production billing policy or contract terms.

# Stripe staging — end-to-end verification (test mode)

**Objective:** An operator with **no code changes** can **wire, exercise, and verify** Stripe Test mode + ArchLucid staging: checkout session, webhook, SQL ledger, and tenant conversion.

**Context and product copy:** [STRIPE_CHECKOUT.md](../go-to-market/STRIPE_CHECKOUT.md) (canonical Stripe hand-off). This runbook is the **operational, command-level** sequence and SQL.

**Code references (read-only):**

| Item | Location |
|------|----------|
| Checkout API (Admin) | `ArchLucid.Api/Controllers/Billing/BillingCheckoutController.cs` — `POST` **`/v1/tenant/billing/checkout`**, policy **`AdminAuthority`**, model **`BillingCheckoutPostRequest`** |
| Webhook | `ArchLucid.Api/Controllers/Billing/BillingStripeWebhookController.cs` — `POST` **`/v1/billing/webhooks/stripe`**, `AllowAnonymous`, signature inside `StripeBillingProvider` |
| Provider logic | `ArchLucid.Persistence/Billing/Stripe/StripeBillingProvider.cs` — activation on **`checkout.session.completed`** only |
| Billing data model | [BILLING.md](../library/BILLING.md) — `dbo.BillingSubscriptions`, `dbo.BillingWebhookEvents` |
| UI pricing (optional CTA) | `archlucid-ui/src/lib/pricing-types.ts` — optional **`teamStripeCheckoutUrl`** on `public/pricing.json` for marketing **Team** link |

**Stripe event type that drives paid activation:** The implementation’s **`HandleWebhookAsync`** path that calls `HandleCheckoutSessionCompletedAsync` runs only when `stripeEvent.Type` is **`checkout.session.completed`** (case-insensitive) **and** `stripeEvent.Data.Object` is a **`Session`**. Other event types are still recorded and typically marked **Processed** but do **not** run subscription activation. Select at least **`checkout.session.completed`** in the Dashboard endpoint (see **step 3.2** below).

**Tier mapping (checkout → SQL):** `BillingTierCode.FromCheckoutTier` maps **Team** and **Pro** to **`Standard`** in `dbo.Tenants.Tier`; **Enterprise** maps to **`Enterprise`**. `dbo.BillingSubscriptions.Tier` uses the string tier code from the same mapping (`StripeBillingProvider`).

**Last updated:** 2026-04-25

---

## 0. Gaps between STRIPE_CHECKOUT.md and the repo (as of 2026-04-25)

| Topic | STRIPE_CHECKOUT.md | Actual code / repo |
|--------|--------------------|--------------------|
| Checkout method | `POST /v1/tenant/billing/checkout` | **Correct** — but requires **JWT** and **`AdminAuthority`**, not anonymous. |
| `pricing.json` | “Set `teamStripeCheckoutUrl`” | The committed **`archlucid-ui/public/pricing.json`** is **packages-only**; **`teamStripeCheckoutUrl`** is **optional** in the TypeScript type and used by **`MarketingTierPricingSection`**. Add the property in **deployed** static JSON when you want a Payment Link CTA; not required for API-only checkout. |
| Webhook event list | “Align with `StripeBillingProvider`” | For **entitlement** updates, the provider only **activates** on **`checkout.session.completed`**. |
| Staging host examples | e.g. `staging.archlucid.com` | Use your real staging hostname everywhere below (`<staging-api-host>`). |

These gaps do **not** require source edits for verification; they inform how you configure Dashboard, secrets, and UI.

---

## 1. Prerequisites

| # | Prerequisite | Verify |
|---|----------------|--------|
| 1 | **Stripe** account; **Test mode** ON (Dashboard toggle). | Dashboard shows “Test mode”. |
| 2 | Staging **ArchLucid API** deployed over **HTTPS** (e.g. behind Front Door). | `curl -fsS -o /dev/null -w "%{http_code}\n" "https://<staging-api-host>/health/live"` → **200** |
| 3 | Staging app configuration can receive **`Billing:*`** (Key Vault reference or App Settings / Container Apps secrets). | Azure Portal / `az containerapp show` — settings present (redact in logs). |
| 4 | **SQL** reachable from a secure operator path (Microsoft Entra ID auth, private jump host, or read-only user as allowed). | `sqlcmd` or SSMS to `dbo` in staging database. |
| 5 | **Entra (or dev bypass)** and a **tenant admin** user to obtain a **Bearer** token for **`POST /v1/tenant/billing/checkout`**. | Sign in to UI as Admin; or use your org’s token procedure. |
| 6 | **No** this runbook does **not** create Stripe products in your name — you create them in the Dashboard (step **3.3**). | N/A |

---

## 2. Environment variables (illustrative names)

Set in Key Vault and/or App Settings using **double-underscore** or nested configuration as your host expects:

| Setting | Value (test) |
|---------|----------------|
| `Billing:Provider` | `Stripe` |
| `Billing:Stripe:SecretKey` | `sk_test_…` (Dashboard → Developers → API keys, **Test mode**) |
| `Billing:Stripe:WebhookSigningSecret` | `whsec_…` from the **test** webhook endpoint (or from **`stripe listen`**; see [STRIPE_CHECKOUT.md](../go-to-market/STRIPE_CHECKOUT.md) *Staging end-to-end — subsection 4*) |
| `Billing:Stripe:PriceIdTeam` | `price_…` for the Team (or first) test price (step **3.3** below) |
| `Billing:Stripe:PriceIdPro` | Optional `price_…` if you test Pro tier |
| `Billing:Stripe:PriceIdEnterprise` | Optional `price_…` for Enterprise |
| `ASPNETCORE_ENVIRONMENT` | `Staging` (recommended for hosted staging) |

**Verify in Azure (example — replace resource names):**

```bash
az containerapp show -g <resource-group> -n <api-container-app-name> --query "properties.template.containers[0].env" -o table
```

(Inspect `Billing__*` or secret references; do not paste keys into chat.)

Illustrative Key Vault names from [BILLING.md](../library/BILLING.md) include `billing-stripe-secret` and `billing-stripe-webhook-signing-secret` — map them in your platform to the `Billing:Stripe:*` configuration keys.

---

## 3. Step-by-step: configure API, webhook, product/price, UI, checkout, webhook, SQL

### 3.1 Load configuration (operator)

1. In Azure, apply the **Billing** settings from **section 2** to the **staging** API revision.
2. Restart or wait for the revision to become healthy.

**Verify:**

```bash
curl -fsS "https://<staging-api-host>/health/ready"
```

Expect top-level **Healthy** in JSON (database and billing are not always separate entries; use judgment if a billing misconfig fails startup — see API logs in Log Analytics if needed).

### 3.2 Register the Stripe **test** webhook

1. Stripe Dashboard → **Developers** → **Webhooks** → **Add endpoint** (confirm **Test mode**).
2. **Endpoint URL:** `https://<staging-api-host>/v1/billing/webhooks/stripe`  
   — Controller: `BillingStripeWebhookController` · route prefix `v{version}/billing/webhooks` · action **`stripe`**.
3. **Events to send:** at minimum select **`checkout.session.completed`**. (Optional extras do not change activation logic in `StripeBillingProvider` today but may be useful for Dashboard debugging.)
4. Save; copy the endpoint **Signing secret** (`whsec_…`) into `Billing:Stripe:WebhookSigningSecret` for this environment.
5. **Alternative for local/CLI testing:** [STRIPE_CHECKOUT.md](../go-to-market/STRIPE_CHECKOUT.md) documents **`stripe listen --forward-to ...`**; use the CLI’s `whsec_…` in the API config while forwarding.

**Verify (negative — expect non-success without valid signature):**

```bash
curl -sS -o /dev/null -w "%{http_code}\n" -X POST "https://<staging-api-host>/v1/billing/webhooks/stripe" \
  -H "Content-Type: application/json" \
  -H "Stripe-Signature: t=0,v1=invalid" \
  -d '{"id":"evt_test_placeholder","type":"checkout.session.completed","data":{"object":{}}}'
```

**Verify (positive):** use Stripe CLI **`stripe trigger checkout.session.completed`** against a forwarded URL with matching signing secret, or complete a real test Checkout in **3.5–3.6** so Stripe sends a signed event.

### 3.3 Create a test **Product** and **Price** (Stripe Test mode)

1. Dashboard → **Product catalog** → **Add product** (test mode).
2. Add a **recurring** price in **Subscription** mode (the API uses Checkout **`mode = subscription`** in `CreateCheckoutSessionAsync`).
3. Copy the **Price ID** (e.g. `price_xxxx`).
4. Assign that ID to `Billing:Stripe:PriceIdTeam` (or Pro/Enterprise) and redeploy/restart the API as needed.

**This runbook does not run Stripe APIs for you** — only Dashboard steps.

### 3.4 (Optional) Marketing UI link — `pricing.json`

1. In your **built** or **served** `public/pricing.json` (served as static file from the UI app), add or set **`teamStripeCheckoutUrl`** to a Stripe **Payment Link** or hosted Checkout URL **in test mode**, if you use the marketing CTA.
2. Confirm the browser loads: `https://<staging-ui-host>/pricing.json` and shows the property.
3. **Note:** The default committed **`archlucid-ui/public/pricing.json`** may omit `teamStripeCheckoutUrl`; the UI allows it to be empty.

### 3.5 Obtain an admin token and call checkout

`BillingCheckoutController` requires **`[Authorize(Policy = ArchLucidPolicies.AdminAuthority)]`**. The body is **`BillingCheckoutPostRequest`**: `targetTier` (`Team` / `Pro` / `Enterprise`), `returnUrl`, `cancelUrl`, optional `seats`, `workspaces`, `billingEmail`.

1. **Browser path:** In the operator UI, use **Convert to paid** (see [operator-shell.md](../library/operator-shell.md)) and capture the **`POST /v1/tenant/billing/checkout`** response in **DevTools** → **Network** if you need the JSON.
2. **curl path** (`export JWT='<your token>'` first; use `${JWT}` in the header — do not put an eight-character placeholder literal after `Bearer` in the same `curl` example, or `gitleaks`’s `curl-auth-header` rule may match it in CI). Scope URLs to your UI.

```bash
curl -sS -X POST "https://<staging-api-host>/v1/tenant/billing/checkout" \
  -H "Authorization: Bearer ${JWT}" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: <tenant-guid>" \
  -H "X-Workspace-Id: <workspace-guid>" \
  -H "X-Project-Id: <project-guid>" \
  -d "{\"targetTier\":\"Team\",\"returnUrl\":\"https://<staging-ui-host>/welcome\",\"cancelUrl\":\"https://<staging-ui-host>/pricing\",\"seats\":1,\"workspaces\":1}"
```

**If your API requires additional scope headers**, match your environment (same as the UI’s calls).

3. **Response (200):** JSON with **`checkoutUrl`**, **`providerSessionId`**, optional **`expiresUtc`**.  
4. **Open `checkoutUrl`** in the browser; pay with a **test card** (e.g. `4242 4242 4242 4242`).

**Conflict:** `409` if the tenant already has an **Active** subscription per `IBillingLedger.TenantHasActiveSubscriptionAsync` (see [rollback](#7-rollback--resetting-test-state)).

**CLI smoke (optional):** From [STRIPE_CHECKOUT.md](../go-to-market/STRIPE_CHECKOUT.md):

```bash
archlucid trial smoke --org "StripeStagingSmoke" --email "you+smoke@example.invalid" --api-base-url "https://<staging-api-host>"
```

(Use a disposable org/email policy your tenant allows.)

### 3.6 After Checkout completes

Stripe posts **`checkout.session.completed`** to `POST /v1/billing/webhooks/stripe`. The provider validates **`Stripe-Signature`**, idempotency-inserts `dbo.BillingWebhookEvents`, then activates subscription and trial conversion if metadata is present (see `HandleCheckoutSessionCompletedAsync`).

---

## 4. Controllers and routes (quick reference)

| Action | Method | Path | Auth |
|--------|--------|------|------|
| Create Checkout Session | `POST` | `/v1/tenant/billing/checkout` | **Bearer** + **AdminAuthority** + tenant scope |
| Stripe webhook | `POST` | `/v1/billing/webhooks/stripe` | **Anonymous** — **`Stripe-Signature`** + body |

**Handled Stripe `event.type` for activation (business logic in `HandleCheckoutSessionCompletedAsync`):** **`checkout.session.completed`**

---

## 5. SQL verification queries

Run against the **ArchLucid** staging database. Replace **`@TenantId`** with your test tenant `uniqueidentifier`. Use an account that can read RLS-protected `dbo.BillingSubscriptions` (often an operator role with `SESSION_CONTEXT` or an elevated `dbo` read, per your org).

### 5.1 Webhook idempotency and status

**Recent Stripe webhook events:**

```sql
SELECT TOP 30
    EventId,
    EventType,
    ResultStatus,
    ReceivedUtc,
    ProcessedUtc
FROM dbo.BillingWebhookEvents
WHERE Provider = N'Stripe'
ORDER BY ReceivedUtc DESC;
```

**Expected after success:** a row for the Stripe `evt_` id, `EventType = 'checkout.session.completed'`, `ResultStatus = 'Processed'`.

### 5.2 Subscription row

**Subscription for tenant:**

```sql
SELECT
    TenantId,
    WorkspaceId,
    ProjectId,
    Provider,
    ProviderSubscriptionId,
    Tier,
    Status,
    SeatsPurchased,
    WorkspacesPurchased,
    ActivatedUtc,
    CreatedUtc,
    UpdatedUtc
FROM dbo.BillingSubscriptions
WHERE TenantId = @TenantId;
```

**Expected after webhook:** `Status = 'Active'`, `Provider = 'Stripe'`, `ActivatedUtc` set, `Tier` consistent with `BillingTierCode` for the selected checkout tier.

### 5.3 Tenant tier (commercial)

```sql
SELECT Id, Tier
FROM dbo.Tenants
WHERE Id = @TenantId;
```

**Expected:** After conversion, **`Tier`** is **`Standard`** for Team/Pro or **`Enterprise`** for Enterprise (see [TenantTier enum](../../ArchLucid.Core/Tenancy/TenantTier.cs) and `BillingTierCode`).

### 5.4 Audit stream (optional)

```sql
SELECT TOP 20
    OccurredUtc,
    EventType,
    ActorUserId,
    DataJson
FROM dbo.AuditEvents
WHERE TenantId = @TenantId
  AND EventType IN (N'BillingCheckoutInitiated', N'BillingCheckoutCompleted', N'TenantTrialConverted')
ORDER BY OccurredUtc DESC;
```

---

## 6. Funnel and metrics (optional)

Prometheus: **`archlucid_billing_checkouts_total`**, trial conversion series — see [TRIAL_FUNNEL.md](TRIAL_FUNNEL.md) and [BILLING.md](../library/BILLING.md). Not required for a single E2E pass if SQL and Stripe Dashboard already agree.

---

## 7. Rollback / resetting test state

1. **Stripe (Test mode):** Cancel the test **subscription** or **customer** in the Dashboard, or issue refunds as needed for your policy — does not automatically revert ArchLucid SQL.
2. **ArchLucid — repeat checkout on same tenant:** The API **blocks** checkout when `dbo.BillingSubscriptions` has **`Status = 'Active'`** for the tenant. To re-test checkout you must **clear Active** (e.g. set to **`Canceled`** with an elevated, approved SQL change, or delete the row) **in staging only** and under your change process — `dbo.BillingSubscriptions` is normally mutated via **`dbo.sp_Billing_*`** (see [BILLING.md](../library/BILLING.md)). **Do not** improvise ad-hoc production deletes.
3. **Idempotent webhooks:** Replaying the same `EventId` hits **`TryInsertWebhookEventAsync`** dedupe; duplicates return **200** from the API without double-charging business logic if already **Processed** (`StripeBillingProvider` flow).
4. **Tenant trial state:** `MarkTrialConvertedAsync` updates the tenant; reversing **Tier** in SQL for another trial is a **data governance** action — out of scope for a generic runbook; prefer a **new test tenant** for a clean funnel repeat when possible.
5. **Secrets rotation:** If `whsec_…` is rotated, update `Billing:Stripe:WebhookSigningSecret` before the next event or signatures will **fail** (400).

---

## 8. Related

- [STRIPE_CHECKOUT.md](../go-to-market/STRIPE_CHECKOUT.md)
- [BILLING.md](../library/BILLING.md)
- [STAGING_DEPLOYMENT_CHECKLIST.md](../deployment/STAGING_DEPLOYMENT_CHECKLIST.md) (staging host checks)
