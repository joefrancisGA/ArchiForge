# Workflow placeholder — `governance-approval-routing`

This directory documents the **Logic App (Standard)** workflow that should live on the governance host (`azurerm_logic_app_standard.governance_approval` when `enable_governance_approval_logic_app` is true).

## Trigger

- **Azure Service Bus** subscription: output `logic_app_governance_approval_subscription_name` from `infra/terraform-servicebus` (SQL filter on user property **`event_type`** = `com.archlucid.governance.approval.submitted`, matching `AzureServiceBusIntegrationEventPublisher`).

## Payload

- JSON body per `schemas/integration-events/governance-approval-submitted.v1.schema.json`.

## Callbacks (ArchLucid API)

Wire HTTP actions to existing routes (authenticated with the **reviewer’s** Entra token from Teams / OAuth connector, not a shared app-only identity, so `GovernanceWorkflowService` self-approval blocking stays meaningful):

- Approve: `POST /v1/governance/approval-requests/{approvalRequestId}/approve`
- Reject: `POST /v1/governance/approval-requests/{approvalRequestId}/reject`

See `ArchLucid.Api/Controllers/Governance/GovernanceController.cs`.

## Idempotency

Set Logic App **`clientTrackingId`** to `approvalRequestId` from the message body.

## Ship checklist

1. Export `workflow.json` from the designer after review.
2. Store connection references in Key Vault / managed settings (no secrets in git).
3. Link Application Insights to the Logic App resource (see `docs/OBSERVABILITY.md` § Azure Logic Apps).
