> **Scope:** PII boundary for transactional email - full detail, tables, and links in the sections below.

# PII boundary for transactional email

## Objective

Define what **personally identifiable information (PII)** may appear in **trial lifecycle transactional email** bodies, how that relates to **audit-derived mailbox resolution**, and how future **tenant policy** could widen content safely.

## Assumptions

- Trial lifecycle templates intentionally avoid **architecture artefacts** (manifest JSON, finding text, run narratives).
- The **To** address is sourced from **durable audit actor ids** (`TrialProvisioned` / `TenantSelfRegistered`) and is therefore already classified as **identity contact data** in the audit store.

## Constraints

- **Default posture:** emails contain **product metadata** (tenant display name, counts, dates, tier labels) — not **customer workload content**.
- **Regulatory / tenant policy** may require stricter redaction; enforce via template + dispatcher review, not ad-hoc controller logic.

## Architecture overview

**Inputs:** `TenantRecord`, `TrialLifecycleEmailIntegrationEnvelope`, `EmailNotificationOptions`.

**Outputs:** HTML + plain text email bodies rendered via RazorLight.

**Boundary:** dispatcher + templates must not pull **run repositories** or **finding stores** unless a future `TenantEmailContentPolicy` explicitly allows it.

## Component breakdown

| Area | PII relevance |
|------|----------------|
| `ITenantTrialEmailContactLookup` | Returns **email address** (high sensitivity). |
| `TrialWelcomeEmailModel.OrganizationHint` | Uses **tenant display name** (often non-personal; may still identify an org). |
| `TrialApproachingRunLimitEmailModel` | Numeric **usage** only — not contents of runs. |
| Links (`OperatorBaseUrl`) | URLs may be considered **behavioral metadata** if they embed tokens — keep links **opaque HTTPS paths** without JWTs. |

## Data flow

1. Dispatcher resolves **To** via lookup (email string).
2. Dispatcher builds **view models** with bounded fields.
3. Renderer emits HTML/text **without** embedding large JSON blobs.

## Security model

- **Storage:** `dbo.SentEmails` stores **idempotency keys**, not bodies — reduces breach blast radius.
- **Transport:** Prefer **ACS with private networking** and **MI**; SMTP is **dev-oriented** only.
- **Logging:** never log full HTML bodies at `Information` in production; use **sizes / template ids** only.

## Operational considerations

### Opt-in detail per tenant policy (future)

Introduce a nullable **`TenantEmailContentMode`** (example values: `MetadataOnly`, `IncludeRunTitles`) persisted on `dbo.Tenants` **only after legal review**. Dispatcher gates model expansion:

| Mode | Allowed extras |
|------|----------------|
| `MetadataOnly` (default) | Current templates. |
| `IncludeRunTitles` | Optional non-sensitive titles only — still **no** finding bodies. |

Until that column exists, **all tenants** remain on `MetadataOnly`.

### Incident response

If a template accidentally embeds sensitive content, rotate template ids + idempotency namespace (new migration) only after product approval — historical rows remain for forensics.
