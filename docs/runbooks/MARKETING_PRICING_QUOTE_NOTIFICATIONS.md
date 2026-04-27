> **Scope:** Operations for sales inbox mail when a visitor submits the marketing **pricing quote** form (`POST /v1/marketing/pricing/quote-request` → `dbo.MarketingPricingQuoteRequests`).

# Marketing pricing quote → sales notification

## Behaviour

- After a **successful persist** (SQL path), the API sends a **transactional email** to the configured inbox with request id, timestamp (UTC), and **non-secret** fields from the submission. Message body HTML-encodes free text. **Secrets must not** appear in email.
- **Provider `Noop`:** no SMTP or ACS send; the notifier logs at **Information** that it **would** notify sales (same pattern as other outbound mail when mail is not wired).

## Configuration (`Email` section)

| Key | Purpose |
|-----|---------|
| `Email:Provider` | `Noop` (default, dev-safe), `Smtp`, or `AzureCommunicationServices`. |
| `Email:PricingQuoteSalesInbox` | Recipient for quote-request notifications (default **`sales@archlucid.net`**). |
| `Email:FromAddress` / `Email:FromDisplayName` | Envelope from when provider sends real mail. |
| Smtp or ACS sub-keys | See `EmailNotificationOptions` and hosted secrets / Key Vault layout for your environment. |

**Staging / production:** Set provider + credentials so mail reaches **`sales@archlucid.net`** (or override inbox via config). **Tenant safety:** handler stays scoped to the anonymous marketing endpoint; rate limits for the route are unchanged.

## Verification

- Submit a quote from the marketing UI or call the API; confirm a row in **`dbo.MarketingPricingQuoteRequests`** and an inbox message (or `Noop` **would notify** line in logs).
- Idempotency key on the message: `marketing-pricing-quote:{request-id}`.

## Related

- Product / pricing context: [`docs/go-to-market/PRICING_PHILOSOPHY.md`](../go-to-market/PRICING_PHILOSOPHY.md)
- Open product questions: [`docs/PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md) (item 13 — public price list vs quote-on-request)
