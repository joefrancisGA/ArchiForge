## ArchiForge documentation index

### Orientation

- **Context** – high-level purpose and boundary  
  - `docs/ARCHITECTURE_CONTEXT.md`
- **Containers** – projects and their responsibilities  
  - `docs/ARCHITECTURE_CONTAINERS.md`
- **Components** – key modules inside each container  
  - `docs/ARCHITECTURE_COMPONENTS.md`
- **Key flows** – run, export, comparison, replay sequences  
  - `docs/ARCHITECTURE_FLOWS.md`
- **Data model** – core tables/records and relationships  
  - `docs/DATA_MODEL.md`
- **Context ingestion** – connectors, parsers, deduplication, create-run fields  
  - `docs/CONTEXT_INGESTION.md`

### How-to guides

- **Comparison replay** – formats, modes, headers, examples  
  - `docs/COMPARISON_REPLAY.md`
- **(Planned) Add a new comparison type**  
  - `docs/HOWTO_ADD_COMPARISON_TYPE.md` (see below)

---

### Typical questions and where to read

- **“How does a run become a manifest?”**  
  → `ARCHITECTURE_FLOWS.md` (Flow A) + `ARCHITECTURE_COMPONENTS.md` (DecisionEngineService)

- **“How do I replay a comparison and re-export it?”**  
  → `COMPARISON_REPLAY.md` + `ARCHITECTURE_FLOWS.md` (Flow C)

- **“Where do comparison records live and how do I query them?”**  
  → `DATA_MODEL.md` (ComparisonRecords) + `ARCHITECTURE_COMPONENTS.md` (ComparisonRecordRepository, ComparisonReplayService)

- **“Where should I add a new feature?”**  
  → `ARCHITECTURE_CONTAINERS.md` first, then the relevant section of `ARCHITECTURE_COMPONENTS.md`.

