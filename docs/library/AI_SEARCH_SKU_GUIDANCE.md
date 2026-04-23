> **Scope:** Azure AI Search — SKU notes for ArchLucid retrieval - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Azure AI Search — SKU notes for ArchLucid retrieval

**Objective:** Pick a search tier that matches environment (dev vs prod), network boundaries, and cost.

**Assumptions:** Retrieval uses `Retrieval:VectorIndex=AzureSearch` with private connectivity preferred in production.

**Constraints:** Do not expose SMB (port 445) for file-based alternatives; use Azure-native private endpoints where policy requires.

## Dev / test

- **Free** or lowest **Basic** tier is acceptable when vector volume is tiny and latency spikes are tolerable.
- Run without private endpoints on isolated subscriptions only; treat indexes as non-production data.
- Pair with **Azurite** or emulator-backed storage for local compose; AI Search itself has no official local emulator — use a small Azure resource or **InMemory** vector mode (`Retrieval:VectorIndex=InMemory`) for laptop-only work.

## Production

- **Standard** tiers (S1+) when SLA, replica/partition scale, or compliance requires it.
- **Private endpoint** + **private DNS** (see `infra/terraform-private/` patterns): deny public network access on the search resource after cutover.
- **Cost:** replicas × partitions dominate bill; embedding and query volume drive RU-like pressure — cap upstream embedding batching (`Retrieval:EmbeddingCaps` in `appsettings.json`) before scaling search replicas.

## Operational notes

- Reindex jobs after schema changes; monitor **throttling** responses and backoff in callers.
- Align **Terraform** variables for SKU, capacity, and `public_network_access_enabled` with the above posture.
