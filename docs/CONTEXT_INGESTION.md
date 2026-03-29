# Context ingestion pipeline

`ArchiForge.ContextIngestion` turns heterogeneous inputs (description, inline requirements, pasted documents, policy references, topology/security hints, **structured infrastructure declarations**) into **`CanonicalObject`** instances, **enriches** topology/security metadata, **deduplicates** them, and stores a **`ContextSnapshot`** used by the knowledge graph and downstream authority chain.

---

## Request model

HTTP clients send **`ArchitectureRequest`** (see `ArchiForge.Contracts.Requests`). The coordinator maps it to **`ContextIngestionRequest`** via **`ContextIngestionRequestMapper.FromArchitectureRequest`**:

| ArchitectureRequest field | ContextIngestionRequest field | Notes |
|---------------------------|-------------------------------|--------|
| `SystemName` | `ProjectId` | Same logical key used for “latest snapshot” lookups. |
| `Description` | `Description` | Primary free-text description. |
| `InlineRequirements` | `InlineRequirements` | Each line becomes a `Requirement` canonical object. |
| `Documents` | `Documents` | Inline documents (`name`, `contentType`, `content`) — not multipart upload. |
| `PolicyReferences` | `PolicyReferences` | Short strings → `PolicyControl` objects (`reference` + `status=referenced`). |
| `TopologyHints` | `TopologyHints` | → `TopologyResource` objects. |
| `SecurityBaselineHints` | `SecurityBaselineHints` | → `SecurityBaseline` objects. |
| `InfrastructureDeclarations` | `InfrastructureDeclarations` | Structured IaC snippets (`json` or `simple-terraform`) → **`InfrastructureDeclarationConnector`**. |

`RunId` is assigned by **`AuthorityRunOrchestrator`** immediately before **`IContextIngestionService.IngestAsync`**.

---

## File-backed connectors and SMB (port 445)

**Enterprise default:** Do not expose **SMB (TCP 445)** to the public internet. File-backed ingestion should use **private endpoints** (VPN, ExpressRoute, private VNet integration, or managed file shares reachable only from the workload network). Align Terraform/network design with deny-by-default NSGs and private DNS.

When documenting connector deployments, treat **on-prem file shares** as **data-plane** dependencies with the same classification as database connection strings.

## Connector pipeline (fixed order)

Connectors implement **`IContextConnector`**. **Code source of truth:** **`ContextConnectorPipeline.ResolveOrdered`** (`ArchiForge.ContextIngestion.Infrastructure`) — the API host registers **`IEnumerable<IContextConnector>`** only from that method, so execution order is never dependent on implicit multi-registration ordering. **`RegisterContextIngestionAndKnowledgeGraph`** (`ArchiForge.Api` startup) wires DI to that resolver; concrete connector types are listed below in pipeline order:

1. **`StaticRequestContextConnector`** — primary description → one `Requirement` (“Primary Request”) with `SourceType=StaticRequest`.
2. **`InlineRequirementsConnector`** — each inline string → `Requirement` (`SourceType=InlineRequirement`).
3. **`DocumentConnector`** — parses each **`ContextDocumentReference`** with a matching **`IContextDocumentParser`**.
4. **`PolicyReferenceConnector`**
5. **`TopologyHintsConnector`**
6. **`SecurityBaselineHintsConnector`**
7. **`InfrastructureDeclarationConnector`** — **`InfrastructureDeclarationReference`** items parsed by **`IInfrastructureDeclarationParser`** implementations (`json`, `simple-terraform`).

Each connector’s **`DeltaAsync`** returns a short base summary; **`IContextDeltaSummaryBuilder`** (default: **`DefaultContextDeltaSummaryBuilder`**) enriches it with normalized object counts, a per-type breakdown (e.g. `Requirement×2`), and a one-time baseline clause against the **latest persisted `ContextSnapshot` for `ProjectId`** (if any). The enriched segments are joined into **`ContextSnapshot.DeltaSummary`**.

### Optional properties for knowledge-graph targeting

Connectors or parsers may set comma-separated graph **`NodeId`** values on **`CanonicalObject.Properties`** using keys from **`ArchiForge.KnowledgeGraph.CanonicalGraphPropertyKeys`** (`applicableTopologyNodeIds` on **`PolicyControl`**, `relatedTopologyNodeIds` on **`Requirement`**) so **`DefaultGraphEdgeInferer`** emits narrow **`APPLIES_TO`** / **`RELATES_TO`** edges instead of broad heuristics. See **`docs/KNOWLEDGE_GRAPH.md`**.

---

## Supported document content types (single source of truth)

The canonical MIME list for inline documents is **`ArchiForge.ContextIngestion.SupportedContextDocumentContentTypes.All`**. The API FluentValidation rule (**`ContextDocumentRequestValidator`**) and **`PlainTextContextDocumentParser.CanParse`** both use **`SupportedContextDocumentContentTypes.IsSupported`**. When adding a new parser for another type, extend **`All`** and implement **`IContextDocumentParser`**.

---

## Document parsers

### `PlainTextContextDocumentParser`

Supports the MIME types listed in **`SupportedContextDocumentContentTypes`**. Non-empty lines may start with:

| Prefix | Canonical `ObjectType` | `Properties` |
|--------|-------------------------|--------------|
| `REQ:` | `Requirement` | `text` |
| `POL:` | `PolicyControl` | `text` |
| `TOP:` | `TopologyResource` | `text` |
| `SEC:` | `SecurityBaseline` | `text`, `status=declared` |

Prefix matching is case-insensitive. Lines without a recognized prefix are ignored.

### Unsupported content types

- **API:** `ArchitectureRequest` documents are validated with **`ContextDocumentRequestValidator`** (supported types only → **400** if invalid).
- **Ingestion:** If a document reaches **`DocumentConnector`** with no matching parser, a **warning** is appended to **`NormalizedContextBatch.Warnings`** and surfaced on **`ContextSnapshot.Warnings`** (skipped document).

---

## Infrastructure declarations (IaC seam)

DTO: **`InfrastructureDeclarationReference`** (`Name`, **`Format`**, `Content`). Supported v1 **`Format`** values: **`json`**, **`simple-terraform`**.

### `json`

Body deserializes to **`ResourceDeclarationDocument`** with a **`resources`** array of **`ResourceDeclarationItem`** (`type`, `name`, optional `subtype`, `region`, `properties` as string dictionary). Declared **`type`** maps to canonical **`ObjectType`** (e.g. `vnet`/`subnet`/`storage`/`appservice` → **`TopologyResource`**; `keyvault`/`firewall`/`nsg` → **`SecurityBaseline`**; `policy` → **`PolicyControl`**). Each object uses **`SourceType=InfrastructureDeclaration`** and **`SourceId=DeclarationId`**.

### `simple-terraform`

Lightweight regex over lines like **`resource "azurerm_virtual_network" "core"`** (not a full HCL parser). **`terraformType`** is stored on the canonical object; **`ResolveObjectType`** maps vault / firewall / NSG → **`SecurityBaseline`**, `policy` → **`PolicyControl`**, else **`TopologyResource`**.

### Enrichment

After all connectors run, **`ICanonicalEnricher`** (**`CanonicalInfrastructureEnricher`**) runs before deduplication: **`TopologyResource`** objects get inferred **`category`** (`network`, `storage`, `compute`, `data`, `identity`, `general`) from **`resourceType`** or **`terraformType`**. **`SecurityBaseline`** objects get **`status=declared`** when missing.

---

## Deduplication

**`CanonicalDeduplicator`** collapses duplicates before the snapshot is saved. Grouping key:

`ObjectType | Name | fingerprint`

**Fingerprint** precedence:

1. `Properties["text"]` if non-empty  
2. else `Properties["reference"]` if non-empty  
3. else `Properties["terraformType"]` if non-empty (Terraform-derived infra)  
4. else `Properties["resourceType"]` if non-empty (JSON-derived infra)  
5. else empty string  

So policy objects that only set **`reference`** still dedupe correctly when the same reference appears from multiple connectors; infrastructure objects can dedupe on provider/resource kind when text is absent.

---

## Downstream: knowledge graph

After **`ContextSnapshot`** is saved, **`ArchiForge.KnowledgeGraph`** builds a typed **`GraphSnapshot`** (nodes, inferred edges, validation). Canonical **`ObjectType`** values (e.g. `Requirement`, `TopologyResource`, `PolicyControl`, `SecurityBaseline`) become **`GraphNode.NodeType`**; enrichment such as **`category`** on topology objects feeds node **`Category`** and edge inference.

See **`docs/KNOWLEDGE_GRAPH.md`** for pipeline, **`EdgeType`** semantics, DI registration, persistence JSON aliases, and manifest integration.

---

## Further reading

- **Typed knowledge graph:** `docs/KNOWLEDGE_GRAPH.md`.
- **API body and validation:** `docs/API_CONTRACTS.md` (create run / `ArchitectureRequest`).
- **Persisted snapshots:** `docs/DATA_MODEL.md` (`ContextSnapshots`).
- **Architecture overview:** `docs/ARCHITECTURE_CONTEXT.md`.
