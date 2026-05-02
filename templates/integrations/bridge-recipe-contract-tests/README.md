> **Scope:** Runnable contract tests only — asserts CloudEvent JSON shape maps to downstream ITSM payloads without outbound calls.

Contract and catalog alignment (repo root paths):

- `schemas/integration-events/catalog.json` — integration event type catalog used by bridge recipes.
- `docs/library/INTEGRATION_EVENTS_AND_WEBHOOKS.md` — integration events, webhooks, and operator guidance.

## What this harness proves

- The recipe consumes the canonical CloudEvents envelope (`specversion`, `type`, `id`, `source`, `data`) instead of a per-tool finding schema.
- The downstream payload carries the CloudEvent `id` as the idempotency / correlation key.
- The downstream payload includes an ArchLucid review backlink based on `data.runId`.
- The tests run without Jira, ServiceNow, Power Automate, or Logic Apps credentials.

## Security checklist for bridge authors

- Validate `X-ArchLucid-Webhook-Signature` or receive through a trusted Service Bus subscription.
- Store Jira / ServiceNow / Confluence credentials in Key Vault or the target platform secret store.
- Reject duplicate CloudEvent `id` values before creating a second downstream ticket.
- Keep the ArchLucid `runId` backlink in every downstream issue for support correlation.
- Do not transform ArchLucid events into a new long-lived schema unless the canonical event catalog changes.

## Run locally

From this directory:

```bash
node --test mapping-contract.test.mjs
```

From repository root:

```bash
node --test templates/integrations/bridge-recipe-contract-tests/mapping-contract.test.mjs
```

Parity helpers for PowerShell-only validation:

- **`../validate-jira-bridge.ps1`**
- **`../validate-servicenow-bridge.ps1`**
