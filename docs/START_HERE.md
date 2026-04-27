> **Scope:** Canonical buyer + evaluator entry — Day-0 narrative, audience split, five-document contributor spine, and where to go next without opening 200 root files.

> **Read this first (forced tree):** **[`READ_THIS_FIRST.md`](READ_THIS_FIRST.md)** — single Y/N entry surface; this file is the deeper hub after you pick a lane.

# Start here — ArchLucid

## Objective

Give **buyers, evaluators, sponsors, operators, and engineers** one place to understand **what to open first**, **how long each step takes**, and **where depth lives** without competing "first doc" hubs.

## Audience split (read this first)

ArchLucid is a **SaaS** product. Pick the column that matches you — they share **almost no documents**.

| You are… | What you ever touch | Start here | Never asked of you |
|---|---|---|---|
| **Buyer / evaluator / sponsor / customer** | The public site (`archlucid.net`), the in-product **operator UI** after sign-in, and the **Azure portal** only for your own tenant identity / billing artefacts. | **[`BUYER_FIRST_30_MINUTES.md`](BUYER_FIRST_30_MINUTES.md)** (the canonical evaluator entry — five steps, no install) → **[`EXECUTIVE_SPONSOR_BRIEF.md`](EXECUTIVE_SPONSOR_BRIEF.md)** → **[`ARCHITECTURE_ON_ONE_PAGE.md`](ARCHITECTURE_ON_ONE_PAGE.md)**. The same five steps with screenshots live at the marketing route `archlucid.net/get-started`. | **No Docker. No SQL. No .NET / Node SDKs. No Terraform. No CLI.** If any doc tells you to install one of those, you are reading a **contributor** doc by mistake. |
| **ArchLucid contributor / engineer / internal operator** | The repo, your local toolchain (Docker / SQL container / .NET / Node), the GitHub workflows, and (operator only) the production Azure subscription via OIDC. | The **five-document contributor spine** below. | None — this column is the one with the toolchain. |

> **What about the buyer's first 30 minutes inside the product?** The buyer-facing equivalent of [`engineering/FIRST_30_MINUTES.md`](engineering/FIRST_30_MINUTES.md) ships in two places: the repo stub at [`BUYER_FIRST_30_MINUTES.md`](BUYER_FIRST_30_MINUTES.md) (consultative scaffold, q35 placeholders on owner-blocked sentences) and the marketing route `archlucid.net/get-started` (same five steps with placeholder screenshot slots until owner names the real-tenant `tenantId` / `runId` for capture). The cloud trial funnel itself (`archlucid.net/signup → /demo/preview → first sample run`) is wired in code but **not yet live in production** — see Improvement 2 in [`QUALITY_ASSESSMENT_2026_04_23_INDEPENDENT_73_20.md`](QUALITY_ASSESSMENT_2026_04_23_INDEPENDENT_73_20.md) §3 and [`runbooks/TRIAL_FUNNEL_END_TO_END.md`](runbooks/TRIAL_FUNNEL_END_TO_END.md).

## Assumptions

- **Quick lookup:** [Doc Navigator](NAVIGATOR.md) — one table, 15 common tasks, each row links to a single best document.
- You self-identified above. The **buyer column never installs anything**; the **contributor column** uses the spine.
- Incomplete requirements and imperfect teams are normal — this layout keeps the **default path narrow** and pushes depth into [`docs/library/`](library/) and topic folders.

## Constraints

- **Architectural decision records** stay under [`docs/adr/`](adr/) (do not treat ADRs as onboarding fiction).
- **Historical receipts** stay under [`docs/archive/`](archive/) — never silently rewritten.
- **SMB / port 445** never belongs on the public internet; storage stays on private endpoints (see [`SECURITY.md`](../SECURITY.md) at repo root and [`docs/TROUBLESHOOTING.md`](TROUBLESHOOTING.md)).

## Architecture overview (where ArchLucid sits)

ArchLucid coordinates **architecture requests → authority pipeline → committed manifests + artifacts + evidence**. The **C4-style poster** is **[`ARCHITECTURE_ON_ONE_PAGE.md`](ARCHITECTURE_ON_ONE_PAGE.md)** — read it once you have run something (even a demo run).

```text
[Evaluator / Sponsor] --> START_HERE (this file)
       |
       v
[Five-document spine] --> depth on demand --> docs/library + adr + runbooks
```

## Component breakdown

| Layer | You touch it when… |
|-------|---------------------|
| **Buyer / sponsor narrative** | You need procurement-safe language before touching the repo — **[`EXECUTIVE_SPONSOR_BRIEF.md`](EXECUTIVE_SPONSOR_BRIEF.md)** |
| **Five-document spine** | You will implement, operate, or govern ArchLucid — table below |
| **Operator UI wizard** | You want `/runs/new` semantics without screenshots — **[`library/FIRST_RUN_WIZARD.md`](library/FIRST_RUN_WIZARD.md)** + checklist **[`library/FIRST_RUN_WALKTHROUGH.md`](library/FIRST_RUN_WALKTHROUGH.md)** |
| **Deeper engineering index** | You already ran the spine and need maps — **[`ARCHITECTURE_INDEX.md`](ARCHITECTURE_INDEX.md)** |
| **Everything else** | Search or browse **[`docs/library/`](library/)** (~150+ reference markdown files moved 2026-04-23 to keep `/docs` root small) |

## Data flow — canonical **buyer / evaluator** journey (no install)

1. **Open the canonical first-30-minutes path** — **[`BUYER_FIRST_30_MINUTES.md`](BUYER_FIRST_30_MINUTES.md)** (5 min read; the same five steps render at `archlucid.net/get-started` with screenshots).
2. **Believe the problem is real** — read **[`EXECUTIVE_SPONSOR_BRIEF.md`](EXECUTIVE_SPONSOR_BRIEF.md)** (10–15 min).
3. **See the system shape** — skim **[`ARCHITECTURE_ON_ONE_PAGE.md`](ARCHITECTURE_ON_ONE_PAGE.md)** (15 min; diagrams first; no install required, just look at the poster).
4. **Run something — in the cloud, not locally** — sign up at **`archlucid.net/signup`** (cloud trial; status see Improvement 2 in [`QUALITY_ASSESSMENT_2026_04_23_INDEPENDENT_73_20.md`](QUALITY_ASSESSMENT_2026_04_23_INDEPENDENT_73_20.md)). Until that path is live, request a guided demo.
5. **Run a serious pilot** — read **[`CORE_PILOT.md`](CORE_PILOT.md)** for the operator motion and review surfaces (you operate the in-product UI; ArchLucid hosts the stack).
6. **Track open decisions** — **[`PENDING_QUESTIONS.md`](PENDING_QUESTIONS.md)** (owner gates, cadence reminders).

### Five-document **contributor / internal-engineer** spine (Day-1 reading order)

> **Audience.** ArchLucid contributors and internal engineers only. **Customers never read this spine.** It is the toolchain path for people building or operating ArchLucid itself.

| # | Document | Role | Time |
|---|----------|------|------|
| 1 | **[`engineering/INSTALL_ORDER.md`](engineering/INSTALL_ORDER.md)** | Contributor toolchain + install order (Docker, .NET, Node) | ~10 min |
| 2 | **[`engineering/FIRST_30_MINUTES.md`](engineering/FIRST_30_MINUTES.md)** | First committed manifest + finding on a contributor laptop (Docker) | ~30 min |
| 3 | **[`CORE_PILOT.md`](CORE_PILOT.md)** | First pilot / operator motion (read for context; safe for buyers too) | ~20 min |
| 4 | **[`ARCHITECTURE_ON_ONE_PAGE.md`](ARCHITECTURE_ON_ONE_PAGE.md)** | Poster + ownership (safe for buyers too) | ~15 min |
| 5 | **[`PENDING_QUESTIONS.md`](PENDING_QUESTIONS.md)** | Owner decisions + gates | ~10 min |

**Filename redirects:** [`FIRST_5_DOCS.md`](FIRST_5_DOCS.md), [`FIRST_FIVE_DOCS.md`](FIRST_FIVE_DOCS.md), [`FIRST_RUN_WIZARD.md`](FIRST_RUN_WIZARD.md), [`FIRST_RUN_WALKTHROUGH.md`](FIRST_RUN_WALKTHROUGH.md) are **thin stubs** pointing at [`READ_THIS_FIRST.md`](READ_THIS_FIRST.md) so bookmarks stay stable; spine detail stays in this file.

## Security model (read once)

- **Authentication modes** and fail-closed defaults are summarized in **[`library/SECURITY.md`](library/SECURITY.md)** and repo-root **[`SECURITY.md`](../SECURITY.md)**.
- **Tenant isolation / RLS** deep dive: [`security/MULTI_TENANT_RLS.md`](security/MULTI_TENANT_RLS.md).

## Operational considerations

- **Break / fix loop:** [`TROUBLESHOOTING.md`](TROUBLESHOOTING.md).
- **Hosted stack order:** [`library/REFERENCE_SAAS_STACK_ORDER.md`](library/REFERENCE_SAAS_STACK_ORDER.md).
- **Change log (user-visible):** [`CHANGELOG.md`](CHANGELOG.md) · **breaking-only:** [`../BREAKING_CHANGES.md`](../BREAKING_CHANGES.md).

## Where the rest of the docs went

On **2026-04-23** the repository **compressed `/docs` root** so evaluators see ~20 active entry files instead of ~200. Most former root markdown files now live under **[`docs/library/`](library/)** with **relative links rewritten** across markdown. Superseded **quality / Cursor prompt packs** (except the current **68.60** pair) moved under **[`archive/quality/2026-04-23-doc-depth-reorg/`](archive/quality/2026-04-23-doc-depth-reorg/)**.

**Inventory:** [`library/DOC_INVENTORY_2026_04_23.md`](library/DOC_INVENTORY_2026_04_23.md) lists every active markdown file (excluding `docs/archive/`) with last-modified metadata and audience tags.

## Related (optional depth)

- Historical onboarding write-ups: [`archive/ONBOARDING_START_HERE_2026_04_17.md`](archive/ONBOARDING_START_HERE_2026_04_17.md)
- Full archive index: [`archive/README.md`](archive/README.md)
