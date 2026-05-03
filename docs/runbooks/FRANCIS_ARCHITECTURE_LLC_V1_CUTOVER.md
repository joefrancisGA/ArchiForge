> **Scope:** Runbook — phased migration of ArchLucid commercial, contractual, and outward vendor identity from the founder’s personal / sole-proprietorship posture to **Francis Architecture, LLC** during the V1 window (not legal advice).

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Runbook — Francis Architecture, LLC V1 commercial cutover

**Status:** Plan (2026-05-03). **Supersedes nothing automatically:** until this runbook is executed and recorded in [`docs/CHANGELOG.md`](../CHANGELOG.md), the repo’s resolved owner decisions for **Partner Center legal entity** (Joseph Francis, Sole Proprietorship) and **Stripe** operational ownership remain authoritative — see [`docs/PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md) items **8** and **9**.

**Disclaimer:** This document is operational guidance for the ArchLucid team. **Entity structure, contract assignment or novation, tax, and liability** are jurisdiction-specific. Engage **qualified legal counsel and a CPA** before binding the LLC or changing customer agreements.

---

## 1. Objective

Centralize **commercial, contractual, tax, banking, marketplace, card-presentment, and buyer-facing “vendor” identity** for ArchLucid under **Francis Architecture, LLC** for V1, without breaking pilots, payouts, webhooks, or Trust Center accuracy.

---

## 2. Assumptions

- **Francis Architecture, LLC** is (or will be) formed with **articles**, **EIN**, **operating agreement**, and **authorizing resolutions** sufficient to sign MSAs/order forms and complete KYB/KYC with Stripe and Microsoft Partner Center.
- The founder accepts **Stripe** and **Partner Center** reverification timelines (often multi-week).

---

## 3. Constraints

- Third-party rails (**Stripe business profile**, **Partner Center publisher/seller**, banking) gate the calendar; technical URL paths (`/v1/billing/webhooks/stripe`, marketplace webhook) stay the same unless product explicitly changes them — **credentials and legal metadata** behind them change with the LLC.
- Governing templates still use placeholders such as **`[ArchLucid vendor legal entity]`** in [`docs/go-to-market/MSA_TEMPLATE.md`](../go-to-market/MSA_TEMPLATE.md) and [`docs/go-to-market/DPA_TEMPLATE.md`](../go-to-market/DPA_TEMPLATE.md); the LLC name and registered address **must match** executed customer documents and Trust Center prose after cutover.

---

## 4. Architecture overview

Four parallel tracks converge on one **commercial cutover date**:

| Track | Purpose |
|--------|---------|
| **Legal / IP** | Signatory authority; founder → LLC **IP/domain/brand** assignment where applicable; pilot contract continuity (**novation** vs **replacement order form** — counsel decides). |
| **Money / tax** | LLC **bank account**; **Stripe** KYB/TIN; **Partner Center** tax + payout profiles; invoicing/W-9 (or equivalents). |
| **Identity / GTM** | Trust Center, privacy/DPA subprocessors sections, footer contacts, registrar/WHOIS if applicable — **consistent vendor party name**. |
| **Platform / infra** | Azure subscription **billing owner** and RBAC if still under a personal subscription; ensure **break-glass** and **secret rotation** runbooks name the **LLC role** (e.g. Managing Member) where “owner” is cited. |

---

## 5. Component breakdown (workstreams)

### 5.1 Inventory (first milestone)

Produce a single checklist of every surface tied to **personal** or **sole proprietorship** identity: Partner Center, Stripe, DNS/registrar, GitHub/org billing, insurance, accountant, executed pilot order forms, NDAs, domain SSL contact email.

### 5.2 Intellectual property and product

- Execute founder → **LLC** assignment for copyrights, domains, and **ArchLucid** marks in use, per counsel.
- Decide whether **GitHub organization** ownership eventually moves to an LLC-controlled org; if yes, plan migration **outside** the billing cutover weekend if possible.

### 5.3 Contracts and customers

- **New** orders: vendor block lists **Francis Architecture, LLC** (full legal name, state, signing officer).
- **In-flight pilots:** counsel-driven **novation** or **terminate + resign** with updated [`docs/go-to-market/ORDER_FORM_TEMPLATE.md`](../go-to-market/ORDER_FORM_TEMPLATE.md).

### 5.4 Stripe (direct billing)

- Confirm with Stripe whether the path is **entity update on existing account** vs **new LLC Standard account** + key rotation.
- Plan **controlled rotation** of `sk_live_*` / webhook signing secrets per [`STRIPE_WEBHOOK_INCIDENT.md`](STRIPE_WEBHOOK_INCIDENT.md) and Key Vault posture; **`ARCHLUCID PLATFORM`** statement descriptor can remain unless product changes branding rules.

### 5.5 Azure Marketplace

- Execute Partner Center steps in [`MARKETPLACE_PUBLISHER_IDENTITY.md`](MARKETPLACE_PUBLISHER_IDENTITY.md): **tax profile**, **payout bank**, **legal seller identity** aligned to the LLC when Microsoft policy allows (new publisher vs profile change — follow current Partner Center guidance).
- Keep listing **display name** **`ArchLucid`** unless marketing explicitly changes it; **legal/tax** may read **Francis Architecture, LLC** after cutover.

### 5.6 Docs and Trust Center (repo pass)

One bounded PR after legal confirm:

- Replace **`[ArchLucid vendor legal entity]`** and similar placeholders with the LLC’s exact legal string + address where templates require it.
- Align [`docs/go-to-market/TRUST_CENTER.md`](../go-to-market/TRUST_CENTER.md), [`docs/trust-center.md`](../trust-center.md), [`docs/go-to-market/SUBPROCESSORS.md`](../go-to-market/SUBPROCESSORS.md), and [`docs/go-to-market/PRIVACY_POLICY.md`](../go-to-market/PRIVACY_POLICY.md) controller/processor party names **only** when counsel confirms the LLC is the legal processor/seller of record.

### 5.7 Operational continuity

- Update runbook “owner” lines from a **named individual** to **LLC officer role** where appropriate (`STRIPE_WEBHOOK_INCIDENT.md`, marketplace runbooks).
- Carry **general / cyber** insurance under the LLC if counsel recommends.

---

## 6. Data flow (commercial)

**Before:** Customer → Stripe or Marketplace → **sole proprietorship** tax/payout plumbing.  
**After:** Same integration paths → **LLC bank** and **LLC tax identifiers** on platform tax documents. Application routes and webhook URLs are unchanged unless keys or Partner Center IDs are swapped per deployment runbook.

---

## 7. Security and compliance model

- **DPA/subprocessors:** Processor legal name **must match** the entity lawfully acting as processor; avoid claiming LLC controls flows still operated only as an individual without documentation.
- **Secrets:** Treat Stripe and any Partner Center webhook metadata changes as **rotation events** with `CHANGELOG.md` headings and configuration audit fields where the product defines them (`Billing:Stripe:WebhookSigningSecretRotatedUtc`, etc.).

---

## 8. Operational considerations

### 8.1 Phased sequencing (suggested)

1. **Foundation** — LLC bank, resolutions, CPA consult on effective date and flow-through/salary choices.
2. **Legal packet** — IP assignment; novation/template updates; pilot communication template (“successor vendor”).
3. **Rails** — Stripe + Partner Center KYB under LLC (long lead).
4. **Repo + site** — single documentation PR + marketing copy when counsel approves party strings.
5. **Cutover window** — freeze new personally signed deals; switch live payouts; smoke **Stripe** webhook delivery and **marketplace** subscription events per existing billing/trial runbooks.

### 8.2 Rollback

Payment rails rarely support **instant** rollback without dual accounts; plan **reconciliation windows** instead of implying same-day reversal.

### 8.3 Completion criteria

Cutover is **done** when: (a) counsel confirms customer-facing vendor entity is consistently the LLC, (b) Stripe and Partner Center show LLC as **seller of record** for new charges, (c) repo Trust Center and templates match, and (d) **`docs/CHANGELOG.md`** records the decision and effective date (and [`docs/PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md) item **8** sub-row (a) is updated to the LLC if the sole-prop resolution is fully superseded).

---

## Related

- [`MARKETPLACE_PUBLISHER_IDENTITY.md`](MARKETPLACE_PUBLISHER_IDENTITY.md)
- [`STRIPE_WEBHOOK_INCIDENT.md`](STRIPE_WEBHOOK_INCIDENT.md)
- [`STRIPE_OPERATOR_CHECKLIST.md`](STRIPE_OPERATOR_CHECKLIST.md)
- [`docs/go-to-market/MARKETPLACE_PUBLICATION.md`](../go-to-market/MARKETPLACE_PUBLICATION.md)
- [`docs/PENDING_QUESTIONS.md`](../PENDING_QUESTIONS.md) (items **8**, **9**)
