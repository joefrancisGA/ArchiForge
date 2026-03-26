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
3. **`GraphSnapshot`** is persisted (SQL) as JSON columns for nodes, edges, and warnings (`ArchiForge.Persistence`). **`SqlGraphSnapshotRepository`** also inserts denormalized rows into **`GraphSnapshotEdges`** (same transaction) for **`IGraphSnapshotRepository.ListIndexedEdgesAsync`** without loading **`EdgesJson`**.
4. When the latest committed **`ContextSnapshot`** for the project matches the new snapshot’s canonical fingerprint (**`GraphSnapshotCanonicalFingerprint`**), **`AuthorityRunOrchestrator`** may **`GraphSnapshotCloner.CloneForNewRun`** from **`GetLatestByContextSnapshotIdAsync`** instead of rebuilding the graph.

---

## Project layout

| Area | Purpose |
|------|---------|
| **`Builders/`** | **`DefaultGraphBuilder`** implements **`IGraphBuilder`**. |
| **`Mapping/`** | **`IGraphNodeFactory`** / **`GraphNodeFactory`** — `CanonicalObject` → `GraphNode`. |
| **`Inference/`** | **`IGraphEdgeInferer`** / **`DefaultGraphEdgeInferer`** — edge inference from node sets. |
| **`Interfaces/`** | **`IGraphBuilder`**, **`IKnowledgeGraphService`**, **`IGraphSnapshotRepository`**, **`IGraphValidator`**. |
| **`Models/`** | **`GraphNode`**, **`GraphEdge`**, **`GraphSnapshot`**, **`GraphBuildResult`**, **`GraphSnapshotExtensions`**. |
| **`Services/`** | **`KnowledgeGraphService`**, **`GraphValidator`**, **`GraphSnapshotCanonicalFingerprint`**, **`GraphSnapshotCloner`**. Root **`CanonicalGraphPropertyKeys`** type documents optional **`CanonicalObject.Properties`** keys (`applicableTopologyNodeIds`, `relatedTopologyNodeIds`) for **`DefaultGraphEdgeInferer`**. |
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
| **`APPLIES_TO`** | Each **`PolicyControl`** → **`TopologyResource`** nodes listed in **`CanonicalGraphPropertyKeys.ApplicableTopologyNodeIds`** when set; otherwise every topology node (legacy heuristic). |
| **`RELATES_TO`** | **`Requirement`** → topology nodes listed in **`CanonicalGraphPropertyKeys.RelatedTopologyNodeIds`** when set; otherwise keyword/category heuristics on requirement text. |

Edges are **deduplicated** by `(FromNodeId, ToNodeId, EdgeType)` (case-insensitive); when duplicates exist, the higher **`GraphEdge.Weight`** wins.

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
- **`PopulatePolicySection`** — **`PolicyApplicabilityFinding`** / **`PolicyCoverageFinding`** → **`manifest.Policy`** (**`SatisfiedControls`** / **`Violations`**).

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
| 3 | Policy / requirement targeting — narrow `APPLIES_TO`/`RELATES_TO` using explicit references | ✓ Done — `CanonicalGraphPropertyKeys` + `DefaultGraphEdgeInferer` |
| 4 | `PolicySection` first-class on `GoldenManifest`, included in `ManifestHashService` + validated by `GoldenManifestValidator` | ✓ Done |
| 5 | `ArchiForge.KnowledgeGraph.Tests` — inferrer, validator, graph builder, and extensions unit tests | ✓ Done |
| 6 | `GraphEdge.Weight`, `GraphSnapshotEdges` index table, `ListIndexedEdgesAsync`, fingerprint + clone reuse in `AuthorityRunOrchestrator` | ✓ Done |

### Open items

1. **Traversal consumers** — use **`Weight`** and **`ListIndexedEdgesAsync`** in read paths that today deserialize full **`EdgesJson`** (when those APIs are added).
2. **Cross-run graph diff** — optional delta persistence when fingerprint differs but overlap is high (not implemented).
