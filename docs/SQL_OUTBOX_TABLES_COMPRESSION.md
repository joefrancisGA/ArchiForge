# Outbox tables: rowstore compression and alternatives

## Objective

Decide how (or whether) to apply **data compression**, **schema shape**, or **retention** changes to the three **write-hot, short-lived** outbox tables so that production Azure SQL stays predictable under load without adopting **`PAGE`** compression blindly the way we did for append-mostly history tables (**084**–**090**).

**In scope objects:** `dbo.IntegrationEventOutbox`, `dbo.RetrievalIndexingOutbox`, `dbo.AuthorityPipelineWorkOutbox`.

## Assumptions

- Rows are **inserted**, **updated** (state transitions), and **deleted or archived** on a short horizon; sustained **churn** dominates over long-term scan-heavy analytics.
- The product may run on **DTU Basic** or small vCore SKUs where **compression is unsupported** or rebuild windows are unacceptable.
- **Terraform** remains the source of truth for server SKU and tuning flags; DDL changes are expressed as migrations + **`ArchLucid.sql`** parity when chosen.

## Constraints

- **Do not** apply **`PAGE`** compression to these tables without measurement: high insert/update rates can increase CPU on compression-aware code paths and encourage **page splits** if row size varies.
- **RLS** and **tenant scope** predicates already attach to these tables (see **`docs/security/MULTI_TENANT_RLS.md`**); any **memory-optimized** option must preserve the same security boundary (separate migration and explicit security review).
- **Historical migrations 001–028** stay immutable; any future outbox change is **new** migration numbers only.

## Architecture overview

| Approach | Role | Fit for outbox |
|----------|------|----------------|
| **Uncompressed + retention** | Default baseline | Lowest risk: aggressive delete/archive keeps leaf page count bounded; no rebuild surprise. |
| **ROW compression** | Lighter than PAGE on narrow rows | Often acceptable on **update-heavy** narrow payloads; less dictionary work than PAGE for volatile pages. |
| **PAGE compression** | Maximum density for scan-heavy, append-mostly | **Poor default** here: same drawbacks as forcing PAGE on a queue without proof. |
| **Memory-optimized OLTP** | Lock-free-ish queues on supported SKUs | Possible for **IntegrationEventOutbox**-class workloads **only** after SKU confirmation, HA/replica implications, and RLS redesign on natively compiled boundaries. |

## Component breakdown

- **`dbo.IntegrationEventOutbox`** — cross-cutting integration fan-out; highest write frequency of the three. Candidate for **ROW** compression or **partitioned** retention (if ever introduced), not PAGE-first.
- **`dbo.RetrievalIndexingOutbox`** — indexing pipeline handoff; moderate churn. Same decision tree as integration outbox.
- **`dbo.AuthorityPipelineWorkOutbox`** — authority pipeline steps; bounded row shape but still **state machine** updates.

## Data flow

1. **Producer** inserts a pending row (scoped by tenant/workspace/project per RLS).
2. **Worker** updates status and payload pointers; completes with delete or terminal state.
3. **Reader** (dispatcher) seeks by **status + time** or **PK**; scans are narrow and short.

Compression changes **inputs** (CPU during insert/update, log bytes during rebuild) and **outputs** (smaller buffers on the rare full scan). For queues, **inputs** usually dominate until the table is accidentally retained too long.

## Security model

- **No change** to RLS predicates from this document alone; any physical redesign (e.g. memory-optimized) must re-validate **filter** and **block** predicates for **`ArchLucidApp`** and session context.
- **Least privilege:** outbox writers remain constrained to app role; compression DDL is **admin** migration only.

## Operational considerations

1. **Measure first:** `sys.dm_db_index_operational_stats` (user seeks vs inserts vs updates), average row length, and **`sp_estimate_data_compression_savings`** for **ROW** and **PAGE** on a **restored copy** of production-like volume.
2. **Retention:** If rows linger > N days, prefer **scheduled purge** or **partition switch-out** over compression as the primary cost lever.
3. **Rebuild window:** If **ROW** or **PAGE** is chosen, use the same idempotent pattern as **084**–**090** (`ALTER INDEX ALL … REBUILD` gated on `sys.partitions.data_compression_desc`) and a paired **`Rollback/RNNN_*.sql`**.
4. **Automatic tuning:** If Azure **createIndex** / **dropIndex** is enabled, reconcile portal drift with repo DDL per **`docs/SQL_DDL_DISCIPLINE.md`**.

## Decision record (default until superseded)

| Table | Default stance | Next evidence to collect |
|-------|----------------|---------------------------|
| `IntegrationEventOutbox` | **No PAGE**; consider **ROW** after measurement | P95 insert/update latency with/without ROW on staging |
| `RetrievalIndexingOutbox` | Same | Queue depth time series vs. CPU |
| `AuthorityPipelineWorkOutbox` | Same | Correlation with authority commit latency |

When a migration is approved, add a row to **`docs/SQL_DDL_DISCIPLINE.md`** and a section to **`docs/SQL_INDEX_INVENTORY.md`** mirroring **084**/**090** style.
