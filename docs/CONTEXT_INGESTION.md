# Context ingestion pipeline

`ArchiForge.ContextIngestion` turns heterogeneous inputs (description, inline requirements, pasted documents, policy references, topology/security hints) into **`CanonicalObject`** instances, **deduplicates** them, and stores a **`ContextSnapshot`** used by the knowledge graph and downstream authority chain.

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

`RunId` is assigned by **`AuthorityRunOrchestrator`** immediately before **`IContextIngestionService.IngestAsync`**.

---

## Connector pipeline (fixed order)

Connectors implement **`IContextConnector`**. Registration order is explicit (see **`RegisterContextIngestionAndKnowledgeGraph`** in `ArchiForge.Api` startup):

1. **`StaticRequestContextConnector`** — primary description → one `Requirement` (“Primary Request”) with `SourceType=StaticRequest`.
2. **`InlineRequirementsConnector`** — each inline string → `Requirement` (`SourceType=InlineRequirement`).
3. **`DocumentConnector`** — parses each **`ContextDocumentReference`** with a matching **`IContextDocumentParser`**.
4. **`PolicyReferenceConnector`**
5. **`TopologyHintsConnector`**
6. **`SecurityBaselineHintsConnector`**

Summaries from each connector’s **`DeltaAsync`** are concatenated into **`ContextSnapshot.DeltaSummary`**. **`DeltaAsync`** receives the **latest persisted `ContextSnapshot` for `ProjectId`** (if any), so messaging can distinguish first ingest vs update for that project.

---

## Document parsers

### `PlainTextContextDocumentParser`

Supports **`text/plain`** and **`text/markdown`**. Non-empty lines may start with:

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

## Deduplication

**`CanonicalDeduplicator`** collapses duplicates before the snapshot is saved. Grouping key:

`ObjectType | Name | fingerprint`

**Fingerprint** precedence:

1. `Properties["text"]` if non-empty  
2. else `Properties["reference"]` if non-empty  
3. else empty string  

So policy objects that only set **`reference`** still dedupe correctly when the same reference appears from multiple connectors.

---

## Further reading

- **API body and validation:** `docs/API_CONTRACTS.md` (create run / `ArchitectureRequest`).
- **Persisted snapshots:** `docs/DATA_MODEL.md` (`ContextSnapshots`).
- **Architecture overview:** `docs/ARCHITECTURE_CONTEXT.md`.
