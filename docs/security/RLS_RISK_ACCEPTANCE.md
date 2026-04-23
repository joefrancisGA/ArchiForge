> **Scope:** Row-level security (RLS) — residual risk acceptance (template) - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Row-level security (RLS) — residual risk acceptance (template)

## 1. Objective

Record **explicit acceptance** of residual risks when **SQL Server RLS** is enabled, partially rolled out, or when **uncovered tables** remain application-scoped only. This is a **governance artifact**, not executable policy.

## 2. Assumptions

- RLS design is described in [MULTI_TENANT_RLS.md](MULTI_TENANT_RLS.md) (§9 covered / uncovered inventory).
- The API continues to enforce **tenant / workspace / project** authorization (`IScopeContextProvider`, policies).
- **Private connectivity** to SQL; no public SMB/file share exposure for tenant payloads at the API boundary.

## 3. Constraints

- RLS **cannot** fix application logic bugs that use the correct tenant but wrong business rules.
- **Uncovered** tables (see MULTI_TENANT_RLS §9) rely entirely on **repository `WHERE` clauses** and reviews.
- **Bypass** connections (migrations, emergency DBA) can see **all** rows — must be rare, audited, and network-isolated.

## 4. Architecture overview

**Nodes:** API tier, SQL with optional `SESSION_CONTEXT`, uncovered legacy tables, operator connections.

**Edges:** request scope → session context → filtered reads/writes; uncovered tables → app-only filtering.

## 5. Component breakdown

| Risk area | Control | Residual risk |
|-----------|---------|----------------|
| Missing `SESSION_CONTEXT` on pooled connection | Deny-by-default predicates when policy **ON** | Misconfigured host strand legitimate traffic or leak rows if bypass used |
| Uncovered child tables | Code review + parameterized queries | Higher blast radius on SQL injection or query omission |
| Predicate complexity | Simple `rls.archiforge_scope_predicate` plus tenant-only **`rls.archiforge_tenant_predicate`** (DbUp **096**) on additional tables | Future schema drift may delay RLS coverage on new tables; two predicate shapes must stay in sync with session context keys |

### Uncovered tables inventory (mirror of MULTI_TENANT_RLS §9)

The following remain **application-scoped** (no `TenantId` / `WorkspaceId` / `ProjectId` RLS predicate on the row). Sign-off assumes repositories and SQL continue to enforce scope explicitly:

- **Legacy architecture commit graph (string `RunId` model):** `dbo.ArchitectureRequests`, `dbo.ArchitectureRuns`, `dbo.AgentTasks`, `dbo.AgentResults`, and related rows without denormalized scope triples.
- **Child / graph tables keyed by foreign ids only:** e.g. `dbo.GraphSnapshots`, `dbo.FindingRecords`, `dbo.ArtifactBundleArtifacts`, `dbo.ConversationMessages`, `dbo.PolicyPackVersions`, `dbo.CompositeAlertRuleConditions`, `dbo.EvolutionSimulationRuns`, and product-learning bridge tables **without** scope columns.
- **Operational:** `dbo.BackgroundJobs`, `dbo.HostLeaderLeases`.

When denormalization migrations land (e.g. **046** pattern), update [MULTI_TENANT_RLS.md](MULTI_TENANT_RLS.md) §9 and trim this list in the same change set.

## 6. Data flow

1. Normal request sets scope triple in `SESSION_CONTEXT` (when enabled).
2. RLS policy filters rows on covered tables.
3. Queries against **uncovered** tables must still include explicit tenant predicates in SQL or use trusted views.

## 7. Security model

- **Defense in depth:** RLS backstops missing `WHERE TenantId = @tid` on **covered** tables.
- **Not a substitute** for API authZ, governance, or secure deployment practices.

## 8. Operational considerations

- **Scalability:** predicate simplicity preserves plan stability.
- **Reliability:** enable RLS only after **all** app entry points set context; integration tests cover cross-tenant negatives.
- **Cost:** low SQL overhead; higher engineering cost for migrations and runbooks.
- **Sign-off:** product owner + security + DBA (roles vary by org) should acknowledge this template before **STATE = ON** in production.

## 9. Evolution

- As child tables gain denormalized scope columns (e.g. DbUp **046** pattern), update MULTI_TENANT_RLS §9 and shrink the “uncovered” list.
- Link this document from release checklists when RLS state changes.

## 10. Related links

- [MULTI_TENANT_RLS.md](MULTI_TENANT_RLS.md) (§9 covered / uncovered tables)
- [SECURITY.md](../library/SECURITY.md)
