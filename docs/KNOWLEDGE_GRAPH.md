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

1. **Replace stringly-typed finding `GetByType("...")` calls** with a shared constants class (parallel to **`GraphNodeTypes`**).
2. **Richer topology containment** — drive **`CONTAINS_RESOURCE`** from declared parent/child in canonical `Properties` instead of label/category heuristics only.
3. **Policy / requirement targeting** — narrow **`APPLIES_TO`** / **`RELATES_TO`** using explicit references from ingestion when available.
4. **Dedicated `PolicySection` on `GoldenManifest`** + SQL column if policy data should be first-class in storage (today folded into assumptions/warnings/issues).
5. **`ArchiForge.KnowledgeGraph.Tests`** project — unit tests for inferrer, validator, and extensions without pulling Persistence.
