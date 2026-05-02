> **Scope:** Runnable contract tests only — asserts CloudEvent JSON shape maps to downstream ITSM payloads without outbound calls.

Contract and catalog alignment (repo root paths):

- `schemas/integration-events/catalog.json` — integration event type catalog used by bridge recipes.
- `docs/library/INTEGRATION_EVENTS_AND_WEBHOOKS.md` — integration events, webhooks, and operator guidance.

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
