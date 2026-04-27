> **Scope:** Security and accessibility operations owners provisioning and routing the `accessibility@archlucid.net` alias; not WCAG engineering guidance (see root [`ACCESSIBILITY.md`](../../ACCESSIBILITY.md) and marketing **`/accessibility`**).

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Accessibility mailbox (`accessibility@archlucid.net`)

## Decision (2026-04-22)

**`accessibility@archlucid.net`** exists as a **public-facing alias** for reporting **accessibility barriers** (WCAG / usability / assistive-technology friction). Mail routes to the **same operational custodian** as **`security@archlucid.net`** so one accountable owner triages inbound mail; the subject line and message body should make **accessibility vs coordinated security disclosure** obvious.

This document records **intent and provisioning steps**. Creating the alias in DNS / tenant admin is **owner-only** (same gate as the canonical `security@` mailbox).

## Custodian alignment

| Mailbox | Role |
| ------- | ---- |
| `security@archlucid.net` | Canonical coordinated disclosure + security inquiries (see [`SECURITY.md`](../../SECURITY.md)). |
| `accessibility@archlucid.net` | Accessibility barrier reports; **same custodian / same tenant** as `security@`, separate alias for clarity and filtering. |

## Public surfaces

- Marketing WCAG self-attestation: **`/accessibility`** (source `archlucid-ui/src/app/(marketing)/accessibility/page.tsx`).
- [`archlucid-ui/public/.well-known/security.txt`](../../archlucid-ui/public/.well-known/security.txt) lists **`Contact: mailto:accessibility@archlucid.net`** alongside the security contact.

<!-- TODO(owner): confirm provider — pick the checklist below that matches the live `security@archlucid.net` tenant (Microsoft 365 vs Google Workspace vs other). Remove this comment once confirmed. -->

## Provisioning checklist (Microsoft 365 / Exchange Online) — use if `security@` is an Exchange mailbox or shared mailbox in Entra ID

1. **Entra ID / Microsoft 365 admin center:** confirm the **`security@`** recipient type (user mailbox, shared mailbox, distribution list, or mail-enabled security group).
2. **Add alias / proxy address:** on the **same recipient object** that receives `security@`, add **`accessibility@archlucid.net`** as a secondary SMTP proxy (`Set-Mailbox -EmailAddresses` or Admin UI **Email aliases**).
3. **DNS:** ensure **MX** for `archlucid.net` points at the same Microsoft 365 endpoint already used for `security@` (no split routing unless intentionally designed).
4. **Inbound rules:** optional transport rule to **tag** or **route** subjects containing `accessibility` / `WCAG` for triage queues — keep default delivery to the same custodian inbox.
5. **Outbound / DKIM:** no change if the alias shares the same organizational domain configuration as `security@`.
6. **Documentation:** after creation, remove the HTML `TODO(owner): confirm provider` comment in this file and record the confirmed recipient type in the table above (one sentence).

## Provisioning checklist (Google Workspace) — use if `security@` is a Google Workspace user or group

1. **Admin console:** open the **user** or **group** that owns `security@` inbound delivery.
2. **Add nickname / alias:** add **`accessibility@`** as an alias on the same user or group (Workspace **Multiple email addresses**).
3. **DNS:** **MX** records for `archlucid.net` must continue to point at Google’s inbound servers (same as `security@`).
4. **Routing / filters:** optional Gmail filter to label **Accessibility** threads; keep delivery to the custodian primary inbox.
5. **Documentation:** same as step 6 in the Microsoft 365 list.

## Operational notes

- **Volume expectation:** low initially; still warrants the same **48-hour acknowledgment** target as security mail where feasible (not a legal SLA — see [`SECURITY.md`](../../SECURITY.md) for security-specific timelines).
- **Spam / phishing:** the alias is published on the public web; use the same anti-phishing posture as `security@` (SPF/DKIM/DMARC already aligned at domain level).
