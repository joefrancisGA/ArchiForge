> **Scope:** Transactional email (trial lifecycle) - full detail, tables, and links in the sections below.

# Transactional email (trial lifecycle)

## Objective

Deliver **operator-facing trial lifecycle email** (welcome, first successful run, mid-trial, approaching run limit, expiring, expired, converted) without coupling HTTP controllers to SMTP or Azure SDKs. Dispatch is **durable-audit + integration-event driven** for audit-triggered mail, and **scheduled + integration-event driven** for time-based triggers, with **SQL idempotency** (`dbo.SentEmails`, migration **076**) so retries never double-send.

## Assumptions

- The **trial admin mailbox** is resolved from durable audit (`TrialProvisioned` / `TenantSelfRegistered` actor id), not from a dedicated column on `dbo.Tenants`.
- **Azure Communication Services (Email)** is the default production transport when `Email:Provider=AzureCommunicationServices`.
- **Managed identity** can reach the ACS resource over **private networking** where required (align with private endpoint posture in Terraform).

## Constraints

- **No HTTP controller call sites** for outbound mail; only **worker / background** dispatch after integration consumption (plus audit append side-effects publishing integration JSON).
- **Fail-closed idempotency**: if `dbo.SentEmails` already contains the `IdempotencyKey`, the dispatcher returns without sending.
- **Production safety**: `Email:Provider=AzureCommunicationServices` requires `Email:AzureCommunicationServicesEndpoint` (validated in `ProductionSafetyRules.CollectTransactionalEmailAcs`).

## Architecture Overview

**Nodes:** API/Worker hosts → `IAuditService` decorator → `IntegrationEventOutbox` / Service Bus → `TrialLifecycleEmailIntegrationEventHandler` → `ITrialLifecycleEmailDispatcher` → `IEmailProvider` → ACS / SMTP / Noop.

**Edges:** JSON payload `com.archlucid.notifications.trial-lifecycle-email.v1` (see `schemas/integration-events/trial-lifecycle-email.v1.schema.json`).

**Flows:**

1. **Audit path:** `TrialLifecycleEmailPublishingAuditDecorator` wraps `AuditService` and publishes integration events for `TrialProvisioned`, `CoordinatorRunCommitCompleted`, `TenantTrialConverted`.
2. **Schedule path:** `TrialLifecycleEmailScanHostedService` (leader lease `hosted:trial-lifecycle-email-polling`) runs `TrialScheduledLifecycleEmailScanner` daily and enqueues the same integration event type for day-7 / 80% / expiring / expired conditions.

## Component Breakdown

| Component | Responsibility |
|-----------|------------------|
| `IEmailProvider` | Transport abstraction (`Noop`, `Smtp`, `AzureCommunicationServices`). |
| `IEmailTemplateRenderer` | RazorLight embedded `.cshtml` rendering (`ArchLucid.Application/Notifications/Email/Templates/`). |
| `ITrialLifecycleEmailDispatcher` | Gate triggers, resolve mailbox, reserve idempotency row, render, send. |
| `ISentEmailLedger` | SQL / in-memory insert-only idempotency. |
| `ITenantTrialEmailContactLookup` | SQL lookup of latest trial registration actor email. |
| `IAzureCommunicationEmailApi` | Stub-friendly ACS transport wrapper. |

## Data Flow

1. **Publish** `TrialLifecycleEmailIntegrationEnvelope` (`schemaVersion`, `trigger`, tenant/workspace/project ids, optional `runId`, optional `targetTier`).
2. **Consume** in worker → `DispatchAsync`.
3. **Reserve** `dbo.SentEmails` row (`INSERT … WHERE NOT EXISTS`).
4. **Render** HTML + text from template id.
5. **Send** via configured provider.

## Security Model

- **Secrets:** ACS uses **managed identity** (`DefaultAzureCredential`); SMTP may use `Email:SmtpPassword` (prefer **Key Vault references** in App Service / Container Apps, not plain appsettings in production).
- **PII:** see `docs/security/PII_EMAIL.md` — templates are **metadata-only** by default (no manifest text, no finding bodies).

## Operational Considerations

### Provider matrix

| `Email:Provider` | When to use | Notes |
|------------------|-------------|-------|
| `Noop` | Dev / tests / air-gapped | Default. |
| `Smtp` | Local SMTP sink | Requires `Email:SmtpHost`, `Email:FromAddress`. |
| `AzureCommunicationServices` | Production | Requires `Email:AzureCommunicationServicesEndpoint` + verified sender domain in ACS. |

### Environment variables / configuration

| Key | Purpose |
|-----|---------|
| `Email:Provider` | `Noop` / `Smtp` / `AzureCommunicationServices`. |
| `Email:AzureCommunicationServicesEndpoint` | ACS endpoint URL (`https://{resource}.communication.azure.com/`). |
| `Email:AzureManagedIdentityClientId` | Optional user-assigned MI client id. |
| `Email:FromAddress` / `Email:FromDisplayName` | Envelope sender. |
| `Email:OperatorBaseUrl` | HTTPS base for template links. |
| `Email:ProductDisplayName` | Overrides default **ArchLucid** label in templates. |
| SMTP subset | `Email:SmtpHost`, `Email:SmtpPort`, `Email:SmtpUser`, `Email:SmtpPassword`. |

### Key Vault (recommended shape)

Store **SMTP password** and any **future third-party API keys** as secrets; bind into configuration via Key Vault provider or Container Apps secret refs. ACS should rely on **MI + RBAC** (`Azure Communication Email` data plane role) rather than connection strings.

### Template authoring

- Add `.cshtml` under `ArchLucid.Application/Notifications/Email/Templates/`.
- Register logical id in `EmailTemplateIds` (must match file stem).
- Extend `TrialLifecycleEmailDispatcher.TryBuildPlan` with subject + model wiring.
- Add idempotency helper in `TrialEmailIdempotencyKeys` when introducing a new **once-per-tenant** email class.

### Suppression / complaint handling

ACS and most ESPs maintain **suppression lists** (bounce, complaint, unsubscribe). Operational policy:

1. Treat hard bounces as **stop sending** for that mailbox until re-verified via product support.
2. Never auto-delete idempotency rows; if a send failed **before** reservation, retry is safe; if **after** reservation, operations must investigate (row blocks automated retry by design).

## Related documents

- `docs/security/PII_EMAIL.md`
- `docs/INTEGRATION_EVENTS_AND_WEBHOOKS.md`
- `schemas/integration-events/catalog.json`
