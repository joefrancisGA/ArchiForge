# Knowledge graph (typed architecture graph)

`ArchiForge.KnowledgeGraph` turns each persisted **`ContextSnapshot`** into a **`GraphSnapshot`**: typed **nodes**, typed **edges**, and optional **warnings**. Downstream **`ArchiForge.Decisioning`** finding engines and **`DefaultGoldenManifestBuilder`** consume this graph.

**Related:** `docs/CONTEXT_INGESTION.md` (upstream canonical objects) · `docs/DECISIONING_TYPED_FINDINGS.md` (findings + manifest) · `docs/DATA_MODEL.md` (SQL `GraphSnapshots`).

---

## Pipeline

1. **`IKnowledgeGraphService.BuildSnapshotAsync`** loads/builds via **`IGraphBuilder`**, wraps **`GraphBuildResult`** as **`GraphSnapshot`**, then **`IGraphValidator.Validate`** (throws on invalid node refs or missing types).
2. **`DefaultGraphBuilder`**:
   - Adds one **`ContextSnapshot`** root node (`nodeId` = `context-{SnapshotId:N}`).
   - Maps each **`CanonicalObject`** with **`IGraphNodeFactory`** → **`GraphNode`** (`NodeId` = `obj-{ObjectId}`, **`NodeType`** = `ObjectType`, **`Category`** from `properties["category"]` when present, **`SourceType`** / **`SourceId`**).
   - Calls **`IGraphEdgeInferer.InferEdges`**.
3. **`GraphSnapshot`** is persisted (SQL) as JSON columns for nodes, edges, and warnings (`ArchiForge.Persistence`).

---

## Project layout

| Area | Purpose |
|------|---------|
| **`Builders/`** | **`DefaultGraphBuilder`** implements **`IGraphBuilder`**. |
| **`Mapping/`** | **`IGraphNodeFactory`** / **`GraphNodeFactory`** — `CanonicalObject` → `GraphNode`. |
| **`Inference/`** | **`IGraphEdgeInferer`** / **`DefaultGraphEdgeInferer`** — edge inference from node sets. |
| **`Interfaces/`** | **`IGraphBuilder`**, **`IKnowledgeGraphService`**, **`IGraphSnapshotRepository`**, **`IGraphValidator`**. |
| **`Models/`** | **`GraphNode`**, **`GraphEdge`**, **`GraphSnapshot`**, **`GraphBuildResult`**, **`GraphSnapshotExtensions`**. |
| **`Services/`** | **`KnowledgeGraphService`**, **`GraphValidator`**. |
| **`Repositories/`** | In-memory graph snapshot store (tests / in-memory authority). |

Well-known **`NodeType`** / **`EdgeType`** string constants live in **`WellKnownGraph.cs`** (`GraphNodeTypes`, `GraphEdgeTypes`, `GraphTopologyCategories`).

---

## Node model (`GraphNode`)

| Field | Role |
|-------|------|
| **`NodeId`**, **`NodeType`**, **`Label`** | Identity and display. |
| **`Category`**, **`SourceType`**, **`SourceId`** | Provenance / topology enrichment. |
| **`Properties`** | Case-insensitive copy of canonical `Properties` (plus context node uses fixed keys: `snapshotId`, `runId`, `projectId`). |

---

## Inferred edge types (`DefaultGraphEdgeInferer`)

| `EdgeType` | Meaning (v1 heuristic) |
|------------|-------------------------|
| **`CONTAINS`** | Context root → every non-context node. |
| **`CONTAINS_RESOURCE`** | Topology `category=network` → topology nodes whose **label** contains `"subnet"` (case-insensitive). |
| **`PROTECTS`** | Each **`SecurityBaseline`** → each **`TopologyResource`**. |
| **`APPLIES_TO`** | Each **`PolicyControl`** → each **`TopologyResource`**. |
| **`RELATES_TO`** | **`Requirement`** → topology nodes when requirement **text** matches simple keyword/category heuristics (network/storage/compute/security/database). |

Edges are **deduplicated** by `(FromNodeId, ToNodeId, EdgeType)` (case-insensitive).

---

## Query helpers (`GraphSnapshotExtensions`)

- **`GetNodesByType(nodeType)`**
- **`GetEdgesByType(edgeType)`**
- **`GetOutgoingTargets(fromNodeId, edgeType)`**
- **`GetIncomingSources(toNodeId, edgeType)`**

Finding engines use these for **`RELATES_TO`**, **`PROTECTS`**, **`APPLIES_TO`**, etc.

---

## Dependency injection (API)

Registered in **`RegisterContextIngestionAndKnowledgeGraph`** (`ArchiForge.Api` startup):

- **`IGraphNodeFactory`** → **`GraphNodeFactory`**
- **`IGraphEdgeInferer`** → **`DefaultGraphEdgeInferer`**
- **`IGraphValidator`** → **`GraphValidator`** (singleton)
- **`IGraphBuilder`** → **`DefaultGraphBuilder`**
- **`IKnowledgeGraphService`** → **`KnowledgeGraphService`**

---

## Persistence JSON (legacy aliases)

`ArchiForge.Persistence.Serialization.JsonEntitySerializer` registers converters so **older or alternate** property names still deserialize:

- Nodes: `id` / `nodeId`, `type` / `nodeType`, `name` / `label`, etc.
- Edges: `from` / `fromNodeId`, `to` / `toNodeId`, `type` / `edgeType`, etc.

Round-trip serialization uses the **canonical** names. See **`JsonEntitySerializerGraphCompatibilityTests`** in **`ArchiForge.Decisioning.Tests`**.

---

## Golden manifest integration

**`DefaultGoldenManifestBuilder`** (Decisioning):

- **`PopulateTopologyFromGraph`** — **`TopologyResource`** labels → **`manifest.Topology.Resources`**.
- **`PopulatePolicyApplicability`** — **`PolicyApplicabilityFinding`** → assumptions (info) or warnings + **`UnresolvedIssues`** (policy applicability gap).

---

## Graph coverage analysis (Decisioning)

**`ArchiForge.Decisioning.Analysis`** provides **`IGraphCoverageAnalyzer`** / **`GraphCoverageAnalyzer`**, used by coverage finding engines (**`TopologyCoverageFindingEngine`**, **`SecurityCoverageFindingEngine`**, **`PolicyCoverageFindingEngine`**, **`RequirementCoverageFindingEngine`**) to reason over categories and typed edges. **`DefaultGoldenManifestBuilder.PopulateCoverageWarnings`** maps those findings into manifest gaps, security gaps, unresolved issues, and **`Requirements.Uncovered`**.

---

## Suggested next refactors

> Items 1–5 below have been implemented (batch j, March 2026). See `docs/NEXT_REFACTORINGS.md` §§88–94 and the j-batch entries for full changelog.

### Completed ✓

| # | Item | Status |
|---|------|--------|
| 1 | Replace stringly-typed `GetByType("…")` with `FindingTypes` constants class | ✓ Done — `ArchiForge.Decisioning/Findings/FindingTypes.cs` |
| 2 | Richer topology containment — `CONTAINS_RESOURCE` from explicit `parentNodeId` property | ✓ Done — `DefaultGraphEdgeInferer.InferExplicitParentChildContainment` |
| 3 | Policy / requirement targeting — narrow `APPLIES_TO`/`RELATES_TO` using explicit references | Open — heuristic-only until ingestion emits explicit refs |
| 4 | `PolicySection` first-class on `GoldenManifest`, included in `ManifestHashService` + validated by `GoldenManifestValidator` | ✓ Done |
| 5 | `ArchiForge.KnowledgeGraph.Tests` — inferrer, validator, graph builder, and extensions unit tests | ✓ Done |

### Open items

1. **Policy / requirement targeting (item 3 above)** — ingestion side must emit explicit `APPLIES_TO` references before this can be narrowed; currently every policy applies to every topology resource.
2. **Weighted edge scoring** — add a numeric `Weight` field to `GraphEdge` so traversals can prioritise strong relationships (e.g. explicit parent/child) over inferred ones.
3. **Graph persistence indexes** — the `GraphSnapshots` table stores JSON; add a computed or shadow table for edge-level queries to avoid full-document deserialization at read time.
4. **Incremental graph rebuild** — today the full snapshot is rebuilt on every run. Add a delta path that reuses the previous `GraphSnapshot` and only replaces changed nodes/edges.
