> **Scope:** ArchLucid screenshot gallery — capture brief - full detail, tables, and links in the sections below.

# ArchLucid screenshot gallery — capture brief

**Audience:** Anyone producing screenshots for the marketing site, sales decks, product documentation, or demo recordings.

**Last reviewed:** 2026-04-15

---

## Purpose

This document is a **capture brief**: it describes exactly what to show on screen, what state the data should be in, what annotations to overlay, and what caption to use. Follow it with a running ArchLucid environment (demo seed data recommended) to produce a consistent, professional screenshot set.

---

## Prerequisites

- ArchLucid API running against SQL with **demo seed data** (`Demo:Enabled=true`, `Demo:SeedOnStartup=true`, or `POST /v1.0/demo/seed`). Fastest path: **`scripts/demo-start.ps1`** / **`scripts/demo-start.sh`** with **`docker-compose.demo.yml`** — see **[DEMO_QUICKSTART.md](DEMO_QUICKSTART.md)**. Save captures under **`docs/go-to-market/screenshots/`** when committing assets.
- Operator UI (`archlucid-ui`) running at `http://localhost:3000`.
- At least **two completed runs** with committed golden manifests (use `archlucid run --quick` twice or Swagger).
- At least **one comparison** between the two runs (use `/compare` in the UI or API).
- **One governance approval request** submitted and one approved (if governance is enabled).
- Browser at **1440×900** or **1920×1080** for consistent framing. Use **light mode** for primary set, capture **dark mode** variants for each.
- Disable browser extensions that modify page appearance.

---

## Screenshot 1: First-run wizard — Preset selection

| Attribute | Detail |
|-----------|--------|
| **Screen** | New run wizard — Step 1 |
| **URL** | `/runs/new` |
| **Data state** | Fresh page load, no preset selected yet. All three preset cards visible: Greenfield web app, Modernize legacy system, Blank (advanced). |
| **Annotation callouts** | (A) "Choose a starting template or start from scratch" on the preset card area. (B) "Seven guided steps from intent to pipeline" on the stepper indicator. |
| **Caption** | "ArchLucid's guided wizard walks you from a starting template through identity, requirements, constraints, review, and live pipeline tracking — in seven steps." |
| **Dark mode variant** | Yes |

---

## Screenshot 2: First-run wizard — Review step

| Attribute | Detail |
|-----------|--------|
| **Screen** | New run wizard — Step 6 (Review) |
| **URL** | `/runs/new` (navigate to step 6) |
| **Data state** | Populated from the "Greenfield web app" preset. System name, environment, description, constraints, and capabilities all filled. Validation messages clear (green). |
| **Annotation callouts** | (A) "Full request summary before submission" on the review panel. (B) "Inline validation catches errors before the run is created" near a validated field. (C) "Client-generated request ID for idempotency" on the request ID display. |
| **Caption** | "Review every field before creating the run. The wizard validates inputs inline so errors are caught at design time, not in production." |
| **Dark mode variant** | Yes |

---

## Screenshot 3: Run detail with pipeline stages

| Attribute | Detail |
|-----------|--------|
| **Screen** | Run detail page |
| **URL** | `/runs/{runId}` (use a completed run) |
| **Data state** | Run is committed. Pipeline timeline shows all stages completed (Context, Graph, Findings, Manifest — all with "Ready" badges). Manifest summary visible below. Artifacts table showing at least 3 rows. |
| **Annotation callouts** | (A) "Real-time pipeline tracking from context to manifest" on the pipeline timeline. (B) "Versioned golden manifest — the source of truth" on the manifest summary. (C) "Review or download individual artifacts" on the artifacts table. |
| **Caption** | "Every run shows its complete pipeline journey — from context ingestion through graph build, findings generation, and manifest synthesis — with artifacts available for review and download." |
| **Dark mode variant** | Yes |

---

## Screenshot 4: Provenance graph visualization

| Attribute | Detail |
|-----------|--------|
| **Screen** | Provenance graph |
| **URL** | `/runs/{runId}/provenance` |
| **Data state** | Graph loaded for a completed run. Full provenance view selected. Multiple node types visible (snapshots, findings, decisions, manifest, artifacts). Type/color legend visible. |
| **Annotation callouts** | (A) "Visual decision lineage from evidence to artifact" on the graph area. (B) "Color-coded node types: context, findings, decisions, manifest" on the legend. (C) "Click any node to see its detail" near a node. |
| **Caption** | "The provenance graph traces every architecture decision back to the evidence that drove it — context snapshots, findings, decision traces, manifest entries, and synthesized artifacts." |
| **Dark mode variant** | Yes |

---

## Screenshot 5: Run comparison — structured deltas

| Attribute | Detail |
|-----------|--------|
| **Screen** | Compare two runs |
| **URL** | `/compare?leftRunId={id1}&rightRunId={id2}` |
| **Data state** | Two committed runs compared. Structured manifest deltas visible with additions and changes highlighted. AI explanation section expanded (if available). |
| **Annotation callouts** | (A) "Structured architecture deltas between iterations" on the delta section. (B) "Detect drift before it reaches production" as a summary callout. |
| **Caption** | "Compare any two architecture runs to see exactly what changed — structured manifest deltas highlight additions, removals, and modifications with full context." |
| **Dark mode variant** | Yes |

---

## Screenshot 6: Governance dashboard

| Attribute | Detail |
|-----------|--------|
| **Screen** | Governance dashboard |
| **URL** | `/governance/dashboard` |
| **Data state** | At least one pending approval request visible. Compliance drift chart showing data for the last 30 days (even if sparse). Policy pack change count visible. |
| **Annotation callouts** | (A) "Pending approvals with SLA tracking" on the approval requests section. (B) "Compliance drift trend over time" on the drift chart. (C) "Policy pack activity at a glance" on the change count area. |
| **Caption** | "The governance dashboard gives operators a single view of pending approvals, compliance drift trends, and policy pack activity — so nothing falls through the cracks." |
| **Dark mode variant** | Yes |

---

## Screenshot 7: Audit event log

| Attribute | Detail |
|-----------|--------|
| **Screen** | Audit log |
| **URL** | `/audit` |
| **Data state** | Multiple audit events visible (at least 8–10 rows). Filters panel showing event type dropdown, date range, and correlation ID field. Summary line showing "Showing N events." Export CSV button visible. |
| **Annotation callouts** | (A) "78 typed audit event types — append-only, tamper-resistant" on the event list. (B) "Filter by event type, date, actor, run ID, or correlation ID" on the filter panel. (C) "Export to CSV for compliance evidence" on the Export CSV button. |
| **Caption** | "Every mutation is recorded in a durable, append-only audit store. Filter, search, and export events for compliance evidence — 78 typed event types with CI-enforced coverage." |
| **Dark mode variant** | Yes |

---

## Screenshot 8: Knowledge graph viewer

| Attribute | Detail |
|-----------|--------|
| **Screen** | Graph viewer |
| **URL** | `/graph` |
| **Data state** | Graph loaded for a completed run. Architecture view selected (not provenance). Multiple node types visible — infrastructure elements, requirements, policies as nodes with relationship edges. |
| **Annotation callouts** | (A) "Typed knowledge graph built from your architecture context" on the graph area. (B) "Nodes represent infrastructure, requirements, and policies" near distinct node types. |
| **Caption** | "The knowledge graph visualizes the architecture context as typed nodes and edges — infrastructure elements, requirements, policies, and their relationships — so you can see what the AI agents analyzed." |
| **Dark mode variant** | Yes |

---

## Screenshot 9: First-run wizard — Pipeline tracking (Step 7)

| Attribute | Detail |
|-----------|--------|
| **Screen** | New run wizard — Step 7 (Track) |
| **URL** | `/runs/new` (navigate to step 7 after creating a run) |
| **Data state** | Pipeline tracking in progress or completed. Progress bar at 75% or 100%. Stage badges showing Context Ready, Graph Ready, Findings Ready, Manifest Ready (or the last one still Pending for the "in progress" feel). |
| **Annotation callouts** | (A) "Live pipeline tracking — no page refresh needed" on the progress bar. (B) "Four stages from context to manifest" on the stage badges. |
| **Caption** | "After creating a run, the wizard tracks the AI pipeline in real time — context ingestion, graph build, findings generation, and manifest synthesis — so you know exactly when your results are ready." |
| **Dark mode variant** | Yes |

---

## Screenshot 10: Artifact review and DOCX export

| Attribute | Detail |
|-----------|--------|
| **Screen** | Run detail — artifacts section or artifact review page |
| **URL** | `/runs/{runId}` (artifacts table) or `/manifests/{manifestId}` |
| **Data state** | Artifacts table with at least 4–5 rows showing different artifact types (manifest JSON, architecture diagram, decision trace, DOCX report). One artifact row with "Review" and "Download" buttons visible. |
| **Annotation callouts** | (A) "Stakeholder-grade DOCX reports with embedded diagrams" near a DOCX artifact row. (B) "Review artifacts in-browser or download individually" on the Review/Download buttons. (C) "ZIP bundle for the complete evidence package" if bundle download link is visible. |
| **Caption** | "Every run produces reviewable artifacts — manifests, diagrams, decision traces, and consulting-grade DOCX reports — available for in-browser review, individual download, or as a complete ZIP bundle." |
| **Dark mode variant** | Yes |

---

## Capture checklist

| # | Screen | Light | Dark | Annotated |
|---|--------|-------|------|-----------|
| 1 | Wizard — Preset selection | [ ] | [ ] | [ ] |
| 2 | Wizard — Review step | [ ] | [ ] | [ ] |
| 3 | Run detail with pipeline | [ ] | [ ] | [ ] |
| 4 | Provenance graph | [ ] | [ ] | [ ] |
| 5 | Run comparison | [ ] | [ ] | [ ] |
| 6 | Governance dashboard | [ ] | [ ] | [ ] |
| 7 | Audit event log | [ ] | [ ] | [ ] |
| 8 | Knowledge graph | [ ] | [ ] | [ ] |
| 9 | Wizard — Pipeline tracking | [ ] | [ ] | [ ] |
| 10 | Artifact review / DOCX | [ ] | [ ] | [ ] |

---

## Output conventions

- **File format:** PNG at 2x resolution (Retina). JPEG acceptable for large screenshots.
- **Naming:** `screenshot-{number}-{slug}-{mode}.png` — e.g., `screenshot-01-wizard-preset-light.png`
- **Annotation style:** Semi-transparent dark overlay badges with white text. Pointer arrows from callout to UI element. Consistent font (system sans-serif or brand font once established).
- **Storage:** Place raw screenshots in `docs/go-to-market/screenshots/` and annotated versions in `docs/go-to-market/screenshots/annotated/`.

---

## Related documents

| Doc | Use |
|-----|-----|
| [PRODUCT_DATASHEET.md](PRODUCT_DATASHEET.md) | Product datasheet that will embed these screenshots |
| [POSITIONING.md](POSITIONING.md) | Value pillars and proof points for caption language |
| [BUYER_PERSONAS.md](BUYER_PERSONAS.md) | Which screenshots matter most to which persona |
| [../operator-shell.md](../operator-shell.md) | Operator UI workflow documentation |
| [../FIRST_RUN_WIZARD.md](../FIRST_RUN_WIZARD.md) | Wizard steps and field mappings |
