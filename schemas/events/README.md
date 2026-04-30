> **Scope:** JSON Schema stubs for CloudEvents-shaped integration payloads — illustrative; validate against live emitters.

# Integration event schema stubs (draft-07)

**Purpose:** Minimal JSON Schema documents for outbound **CloudEvents**-shaped payloads published via integration events (`com.archlucid.*`).

**Status:** Contracts live in **`ArchLucid.Contracts`** (including **`ArchLucid.Contracts.Abstractions.*`** namespaces under `Contracts/Abstractions/`); schemas here are reviewer-facing mirrors and **should be validated against production payloads** before customer hand-offs.

---

## Files

| File | Description |
|------|-------------|
| `finding-created.sample.schema.json` | Sample envelope + `data` block for finding lifecycle events |
| `governance-promotion-activated.sample.schema.json` | Governance promotion activation mirror for reviewer workshops |

**Note:** Extend this folder when new **`com.archlucid.*`** event types stabilize; keep names aligned with **`docs/library/INTEGRATION_EVENTS_AND_WEBHOOKS.md`**.
