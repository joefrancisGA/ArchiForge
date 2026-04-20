> **Scope:** ADR 0019 — Azure Logic Apps (Standard) for edge orchestration - full detail, tables, and links in the sections below.

# ADR 0019 — Azure Logic Apps (Standard) for edge orchestration

## Status

Accepted (2026-04-19)

## Context

ArchLucid already publishes domain integration events to Azure Service Bus (`IntegrationEventTypes`, transactional outbox per ADR 0004). Several workflows are **cross-system orchestration** with **human-in-the-loop** steps (governance approvals, incident ChatOps, marketplace fulfillment fan-out) where hand-rolled C# competes poorly with first-party connectors (Teams adaptive cards, Outlook approvals, ServiceNow/Jira).

Azure **Functions** and **Container Apps Jobs** (ADR 0018) remain the default for compute-heavy or CLI-shaped batch work. Logic Apps fill a different niche: **visual run history**, **connector breadth**, and **retry policies** on outbound SaaS calls without growing the API surface.

## Decision

- Use **Azure Logic Apps (Standard, single-tenant)** only for **edge orchestration** that consumes Service Bus integration events or schedules, then calls back into ArchLucid via **APIM + managed identity** (or posts human actions to existing REST routes).
- Keep **JWT verification**, **SQL idempotency**, and **billing ledger** authority inside .NET (ADR 0016). Logic Apps **must not** sit in front of anonymous Marketplace webhooks; they consume **`com.archlucid.billing.marketplace.webhook.received.v1`** published **after** successful provider handling.
- Provision hosts with Terraform under **`infra/terraform-logicapps/`** (disabled by default via `enable_logic_apps`) with **system-assigned managed identity**, **ZRS** storage, and **WS1** plans until workload isolation requires splitting. Optional **per-workload** sites mirror **governance** and **Marketplace**: **trial lifecycle email**, **incident ChatOps**, **promotion customer notify**. Pair **`infra/terraform-servicebus/`** optional filtered topic subscriptions so each trigger receives a minimal event slice without sharing the worker subscription.
- Operational prompts and workflow intent live in **`docs/CURSOR_PROMPTS_LOGIC_APPS.md`**; operators extend workflows via designer or CI-deployed `workflow.json` on the file share (not checked into this repo until a workflow is frozen).

## Consequences

- **Positive:** Auditors and L2 support gain **replayable** run histories for billing and governance side-effects without deploying new API controllers for every SaaS fan-out.
- **Positive:** Teams / email / ITSM integrations reuse Microsoft-maintained connectors instead of bespoke HTTP clients in `ArchLucid.Api`.
- **Trade-off:** Another Azure billable surface (Standard plan + storage); must tag with FinOps metadata like other `infra/terraform-*` roots.
- **Trade-off:** Workflow definitions are **not** strongly typed in C# — contract discipline stays on JSON Schemas under `schemas/integration-events/` and catalog sync tests.
- **Security:** VNet integration and private endpoints follow the same landing-zone rules as Container Apps (`infra/terraform-private`); **no public SMB (port 445)** exposure for file shares.

## Compliance / security notes

- Logic Apps identities need **least-privilege** RBAC on Service Bus **subscriptions** (read) and on Key Vault **get secret** only where adaptive cards require tenant-specific webhook signing secrets.
- Human-in-the-loop actions that mutate governance or alerts must continue to enforce **Entra-signed** caller identity on the API; Logic Apps should forward **user** claims, not substitute a shared service principal for approval decisions that require segregation of duties.

## Related

- ADR 0016 — billing provider abstraction (Marketplace trust boundary).
- ADR 0018 — Container Apps Jobs for batch/offload workloads.
- `docs/CURSOR_PROMPTS_LOGIC_APPS.md`, `docs/runbooks/LOGIC_APPS_STANDARD.md`, `infra/terraform-logicapps/README.md`.
