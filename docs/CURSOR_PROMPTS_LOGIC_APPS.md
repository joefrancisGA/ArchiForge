# Cursor prompts — Azure Logic Apps (Standard) for ArchLucid

**Index entry:** linked from [`CURSOR_PROMPTS_CANONICAL.md`](CURSOR_PROMPTS_CANONICAL.md). **Architecture decision:** [ADR 0019 — Logic Apps Standard edge orchestration](adr/0019-logic-apps-standard-edge-orchestration.md). **Terraform root:** [`infra/terraform-logicapps/`](../infra/terraform-logicapps/README.md). **Operator runbook:** [`LOGIC_APPS_STANDARD.md`](runbooks/LOGIC_APPS_STANDARD.md).

Logic Apps are a **narrow, high-value** fit: cross-system orchestration, human-in-the-loop, and SaaS connectors you would otherwise hand-roll. ArchLucid remains the **system of record**; Logic Apps are **subscribers** and **callers** (Service Bus → Teams / Outlook → callback to API), not replacements for JWT verification, SQL idempotency, or governance transitions.

---

## Cross-cutting objective / assumptions / constraints

| | |
|--|--|
| **Objective** | Deploy **Azure Logic Apps (Standard, single-tenant)** for visual run history and connector breadth while keeping domain rules in `ArchLucid.*`. |
| **Assumptions** | Integration topic from `infra/terraform-servicebus/`; canonical event strings in `ArchLucid.Core.Integration.IntegrationEventTypes`; APIM + managed identity for API callbacks. |
| **Constraints** | **Managed identity** for connectors; **VNet / private endpoints** per landing zone; **no public SMB (port 445)** for file shares; Marketplace JWT stays in **`AzureMarketplaceBillingProvider`** (ADR 0016). |

---

## Prompt `logic-app-governance-approval-routing`

1. Add Logic App **Standard** workflow `governance-approval-routing` (Terraform-managed host per `infra/terraform-logicapps/` pattern).
2. **Trigger:** Service Bus subscription filtered to `com.archlucid.governance.approval.submitted` (`IntegrationEventTypes.GovernanceApprovalSubmittedV1`).
3. **Fan-out:** Teams adaptive card + optional Outlook approval + optional ServiceNow/Jira for `targetEnvironment=prod`.
4. **Callbacks:** POST to existing governance approve/reject routes; propagate **human** identity from Teams so `GovernanceWorkflowService` segregation-of-duties rules still apply.
5. **Idempotency:** `clientTrackingId = approvalRequestId`; rely on API `TryTransitionFromReviewableAsync` for safe retries.
6. **Observability:** Application Insights; document in `docs/OBSERVABILITY.md` when the workflow is enabled.

**Repo scaffolding (implemented):**

| Step | Location |
|------|----------|
| Dedicated topic subscription + SQL filter on **`event_type`** | `infra/terraform-servicebus/` — `enable_logic_app_governance_approval_subscription`, `azurerm_servicebus_subscription_rule` **`$Default`**, optional **`governance_logic_app_managed_identity_principal_id`** → **Data Receiver** |
| Second Logic App (Standard) host | `infra/terraform-logicapps/` — `enable_governance_approval_logic_app`, outputs `governance_logic_app_principal_id` for Service Bus IAM |
| Workflow / callback notes | `infra/terraform-logicapps/workflows/governance-approval-routing/README.md` |
| Observability | `docs/OBSERVABILITY.md` § *Azure Logic Apps (optional)* |

**Still in Portal / export:** `workflow.json`, Teams/Outlook connectors, and adaptive-card action URLs.

---

## Prompt `logic-app-trial-lifecycle-email`

1. Workflow `trial-lifecycle-email` with trigger on `com.archlucid.notifications.trial-lifecycle-email.v1` (`IntegrationEventTypes.TrialLifecycleEmailV1`) and optional daily recurrence calling a read-only internal “due envelopes” API if you retire `TrialLifecycleEmailScanHostedService`.
2. **Templating:** ACS Email primary, Outlook fallback; per-`TrialLifecycleEmailTrigger` templates.
3. **Retry:** Connector exponential backoff + dead-letter path to an internal admin endpoint (mirror `docs/adr/0009-digest-delivery-failure-semantics.md` spirit).
4. **Flag:** `Notifications:TrialLifecycle:Owner` ∈ `{ Hosted, LogicApp }` for safe cutover.

---

## Prompt `logic-app-marketplace-fulfillment-handoff`

1. **Do not** terminate Marketplace HTTPS at Logic Apps. Keep `POST /v1/billing/webhooks/marketplace` → `AzureMarketplaceBillingProvider`.
2. **Trigger:** Service Bus on `com.archlucid.billing.marketplace.webhook.received.v1` (`IntegrationEventTypes.BillingMarketplaceWebhookReceivedV1`) — **implemented:** published from `BillingMarketplaceWebhookController` after successful processing via `MarketplaceWebhookIntegrationEventPublisher`.
3. **Branches:** Teams to sales channel; optional CRM connector; optional follow-up activation API calls **only** through authenticated, idempotent routes already owned by billing code.
4. **Schema:** `schemas/integration-events/billing-marketplace-webhook-received.v1.schema.json`.

---

## Prompt `logic-app-incident-chatops`

1. Trigger on `com.archlucid.alert.fired` (`IntegrationEventTypes.AlertFiredV1`); payload schema `schemas/integration-events/alert-fired.v1.schema.json`.
2. Severity-based routing to Teams / PagerDuty; adaptive card actions **Acknowledge** / **Mute** posting to **existing** alert APIs (add routes if missing; Logic App must not publish new alert events to avoid loops).
3. Companion workflow on `com.archlucid.alert.resolved` to mark Teams cards resolved using stable correlation keys (`deduplicationKey` per `docs/adr/0008-alert-dedupe-scopes.md`).

---

## Prompt `logic-app-promotion-activated-customer-notifications`

1. Trigger on `com.archlucid.governance.promotion.activated` (`IntegrationEventTypes.GovernancePromotionActivatedV1`); filter `environment = prod`.
2. Resolve per-tenant channels via a secured internal preferences API; fan-out email + Teams + signed outbound webhooks in **parallel branches** so one channel failure does not block others.
3. **Secrets:** Key Vault references only; enable `secureInput` / `secureOutput` on HMAC signing steps.

---

## Repo execution log (2026-04-19)

What landed in this repository for the **marketplace / ADR 0016 hand-off** slice and shared **IaC + governance** scaffolding:

| Area | Change |
|------|--------|
| **Integration contract** | `IntegrationEventTypes.BillingMarketplaceWebhookReceivedV1`, JSON Schema + `catalog.json`, AsyncAPI channel, catalog doc row. |
| **API** | `BillingMarketplaceWebhookController` publishes after non-duplicate success using `MarketplaceWebhookIntegrationEventPublisher`. |
| **Domain** | `BillingWebhookHandleResult` carries optional `MarketplaceWebhookReceivedIntegrationPayload`; `AzureMarketplaceBillingProvider` populates it on success. |
| **Tests** | `IntegrationEventPayloadContractTests`, `MarketplaceWebhookIntegrationEventPublisherTests`. |
| **IaC** | `infra/terraform-logicapps/` (disabled by default) + CI matrix validate + `.terraform.lock.hcl`. |
| **Docs / ADR** | ADR **0019**, runbook, `BILLING.md` / `DEPLOYMENT_TERRAFORM.md` / `REFERENCE_SAAS_STACK_ORDER.md` updates. |

**2026-04-19 (governance approval routing prompt):** Service Bus optional subscription + **`$Default`** SQL rule on **`event_type`**; governance Logic App host variables/outputs; `workflows/governance-approval-routing/README.md`; `OBSERVABILITY.md`, `LOGIC_APPS_STANDARD.md`, `INTEGRATION_EVENTS_AND_WEBHOOKS.md` updates.

**Still intentionally out of repo:** concrete `workflow.json` assets and in-app connection bundles — design in Azure Portal or your CD pipeline, then freeze per change control.

---

## Related

- `docs/INTEGRATION_EVENTS_AND_WEBHOOKS.md`
- `docs/adr/0016-billing-provider-abstraction.md`
- `docs/adr/0018-background-workloads-container-apps-jobs.md`
