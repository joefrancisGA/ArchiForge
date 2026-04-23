> **Scope:** Product learning (pilot feedback) — operator & product owner guide (58R) - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Product learning (pilot feedback) — operator & product owner guide (58R)

**Audience:** Operators and product / architecture owners reviewing how ArchLucid outputs are received in a pilot.

**Not the same as** **Learning** in the operator shell ([operator-shell.md](operator-shell.md)): that page is **recommendation learning** (advisory acceptance weights). **Pilot feedback** (this doc) is **cross-cutting judgment** on runs, manifests, and artifacts, stored per tenant/workspace/project.

---

## 1. How data is captured

- Each **signal** is a **human judgment**: trust, reject, revise, follow-up, etc., plus **subject** (what was rated), optional **pattern key**, optional short **comment**, and optional link to an **architecture run**.
- Rows are stored in **`ProductLearningPilotSignals`** (SQL when `ArchLucid:StorageProvider` is **`Sql`**). Scope is always **tenant + workspace + project** (same headers/claims as other operator APIs: `x-tenant-id`, `x-workspace-id`, `x-project-id`, or defaults in Development).
- **Nothing in 58R auto-changes** prompts, rule packs, or agents from this data.
- **Insert path today** is through **application integration** (code that uses the product-learning repository). The shipped API focuses on **read**, dashboard, and **export**; a first-party **HTTP POST** for pilots may arrive in a later change. Until then, empty dashboards simply mean **no rows in scope**.

**Conventions:** Do not put secrets or credentials in free-text fields.

---

## 2. View the learning dashboard (UI)

1. Run the API and [operator UI](OPERATOR_QUICKSTART.md#operator-ui) (`archlucid-ui`, `.env.local` → `ARCHIFORGE_API_BASE_URL`).
2. Open **http://localhost:3000** → nav **Q&A & advisory** → **Pilot feedback** (`/product-learning`).
3. Choose **Time range** (all time, 30 days, 7 days) and **Refresh** if needed.

Each full load issues **four** read requests to the API (summary, opportunities, trends, triage) with the same `since` filter so panels stay aligned.

You will see:

| Section | Purpose |
|--------|---------|
| **Summary** | Signal and run counts, rollup/trend/opportunity/triage counts, expandable API notes. |
| **Trusted vs rejected / revised** | Table of **artifact**-level counts (trusted, revised, rejected, follow-up, runs). |
| **Top improvement opportunities** | Ranked **candidates** for product review (not auto-filed tickets). |
| **Triage queue** | Merged **next steps** (opportunities plus repeated-comment themes that crossed thresholds). |

If counts are zero, scope has no signals yet or the time window filters everything out.

---

## 3. Review top improvement opportunities

- In the UI, read **title**, **severity**, **area**, and **summary**; use **Repeated theme** when present as a hint, not as NLP truth.
- **Ordering is deterministic** on the server (same inputs → same order). Use **Priority rank** as a **review order**, not a promise of engineering priority.
- **API (same scope headers):** `GET /v1/product-learning/improvement-opportunities` — optional query `since`, `maxOpportunities` (see Swagger).

---

## 4. Export triage summaries

**In the UI (same page):** under **Export for triage**, use **Download Markdown**, **Download JSON**, or **Open JSON in new tab**. Exports respect the **same time range** as the dashboard.

**API:**

| Goal | Call |
|------|------|
| Markdown body inside JSON | `GET /v1/product-learning/report?format=markdown` |
| Structured JSON | `GET /v1/product-learning/report?format=json` |
| Download file | `GET /v1/product-learning/report/file?format=markdown` or `format=json` |

Optional: `since` (ISO 8601), `maxReportArtifacts`, `maxReportImprovements`, `maxReportTriage` (bounds enforced — see Swagger).

Exports are **short, human-readable rollups**: totals, artifact outcome table, problem-area bullets, improvement lines, triage preview. **Raw comments are not dumped** into exports by design.

---

## 5. How product / architecture owners should use this

| Do | Avoid |
|----|--------|
| Use the dashboard and export in **triage meetings** to agree on themes (quality, clarity, repeat rejects). | Treat ranks as **automatic backlog priority** or model-driven scores. |
| Tie themes to **concrete artifacts or workflows** (diagrams, manifest sections, export formats). | Over-interpret **short comment prefixes** (they are deterministic string rollups, not semantic clustering). |
| Feed conclusions into your normal **planning / RFC / bug** process with human judgment. | Expect the system to **mutate** production config from pilot feedback. |

---

## 6. Related docs

| Doc | Notes |
|-----|--------|
| [archive/CHANGE_SET_58R.md](../archive/CHANGE_SET_58R.md) | Objectives, constraints, component list, full prompt log (historical). |
| [DATA_MODEL.md](DATA_MODEL.md) | `ProductLearningPilotSignals` table overview. |
| [API_CONTRACTS.md](API_CONTRACTS.md) | General HTTP behavior, auth, correlation ID. |
| [operator-shell.md](operator-shell.md) | Overall UI navigation patterns. |

**Tests:** Filter `ChangeSet=58R` or `FullyQualifiedName~ProductLearning` — see [TEST_STRUCTURE.md](TEST_STRUCTURE.md).
