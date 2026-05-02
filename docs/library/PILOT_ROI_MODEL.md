> **Scope:** ArchLucid Pilot ROI Model - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# ArchLucid Pilot ROI Model

**Audience:** executive sponsors, chief architects, architecture review leads, pilot operators, and sales engineers who need a credible way to judge whether an ArchLucid pilot created business value.

**Status:** Practical V1 pilot-evaluation guidance. This document explains **how to measure pilot success using capabilities ArchLucid supports today**. It is not a pricing model and it is not a guaranteed ROI calculator.

**Narrative of record for sponsors:** **[EXECUTIVE_SPONSOR_BRIEF.md](../EXECUTIVE_SPONSOR_BRIEF.md)**. This ROI model is the measurement companion; keep headline buyer claims in the brief.

**Related:** [README.md](../../README.md) · [CORE_PILOT.md](../CORE_PILOT.md) · [PMF_VALIDATION_TRACKER.md](../go-to-market/PMF_VALIDATION_TRACKER.md) · [PILOT_BUYER_SAFE_EVIDENCE_TEMPLATE.md](../go-to-market/PILOT_BUYER_SAFE_EVIDENCE_TEMPLATE.md) · [REAL_LLM_RUN_EVIDENCE_TEMPLATE.md](../quality/REAL_LLM_RUN_EVIDENCE_TEMPLATE.md) · [PRODUCT_PACKAGING.md](PRODUCT_PACKAGING.md) (§3 *Code seams*, *Four UI shaping surfaces*, *Contributor drift guard* when a measured capability crosses UI layers or Enterprise mutation affordances — shell metrics are **shaped**, **API** responses remain **authoritative**; Vitest **`archlucid-ui/src/lib/authority-seam-regression.test.ts`** for cross-module seam locks; **`archlucid-ui/src/lib/authority-execute-floor-regression.test.ts`** for the **Execute** nav vs mutation floor; **`archlucid-ui/src/lib/authority-shaped-ui-regression.test.ts`** for catalog **`ExecuteAuthority`** nav rows vs rank; **`archlucid-ui/src/app/(operator)/authority-shaped-layout-regression.test.tsx`** for read-tier Enterprise page layout) · [OPERATOR_DECISION_GUIDE.md](OPERATOR_DECISION_GUIDE.md) · [EXECUTIVE_SPONSOR_BRIEF.md](../EXECUTIVE_SPONSOR_BRIEF.md) · [V1_SCOPE.md](V1_SCOPE.md) · [PILOT_GUIDE.md](PILOT_GUIDE.md)

---

## 1. What this model is for

An ArchLucid pilot should answer one business question clearly:

> **Does ArchLucid reduce the time, ambiguity, and manual effort required to move from an architecture request to a reviewable, defensible architecture package?**

For most pilots, the goal is **not** to prove enterprise-wide transformation. The goal is to prove that a real team can:

- produce a committed manifest more quickly,
- produce reviewable artifacts with less manual assembly,
- improve traceability and governance evidence,
- and shorten the path to architecture discussion or approval.

For the **canonical buyer narrative** for this value story, see **[EXECUTIVE_SPONSOR_BRIEF.md](../EXECUTIVE_SPONSOR_BRIEF.md)**.

---

## 2. The simplest sponsor-level value story

A successful pilot should let a sponsor say:

- **We got from request to reviewable architecture output faster.**
- **We reduced manual preparation of architecture artifacts.**
- **We improved visibility into what was decided, why, and what changed.**
- **We created a stronger evidence trail for governance and review.**

That is a credible V1 value story.

---

## 3. What to measure before the pilot

Capture a small baseline before using ArchLucid.

### 3.1 Baseline questions

For one representative architecture workflow, record:

1. **How long does it currently take** to go from architecture request / brief to a reviewable package?
2. **How much manual effort is required** to assemble the architecture narrative, manifest-like content, diagrams, and supporting evidence?
3. **How hard is it to explain what changed** between two versions of a design?
4. **How much governance evidence is missing or manually reconstructed** during review?
5. **How much architect time is spent on packaging and review preparation rather than on design quality?**

ArchLucid now optionally captures the **median hours from architecture request to reviewable package** (question 1) electronically at **self-service trial signup** (`POST /v1/register` — optional `baselineReviewCycleHours` / `baselineReviewCycleSource`), persists it on the tenant row, and surfaces **before vs measured** review-cycle deltas automatically in the tenant **value-report DOCX** and the **first-value Markdown** report (same copy via `ValueReportReviewCycleSectionFormatter` / `DocxValueReportRenderer`), so sponsors see a consistent narrative without operator post-editing when the prospect supplied a number or when the conservative ROI-model default applies.

### 3.2 Keep the baseline light

Do not create a giant measurement program. For most pilots, a simple baseline is enough:

- One representative architecture use case
- One team or one architect group
- One or two current-state cycle-time estimates
- One rough estimate of manual prep effort
- One qualitative assessment of governance friction

---

## 4. What to measure during the pilot

Use the Core Pilot path as the default evaluation lane:

**Create request → Execute run → Commit → Review artifacts**

### 4.1 Primary pilot metrics

These are the most useful V1 measures.

| Metric | Why it matters | How to judge | Computed by ArchLucid? |
|--------|----------------|--------------|------------------------|
| **Time to committed manifest** | Measures speed from request to durable architecture output | Faster than current-state workflow, or meaningfully more predictable | **Yes — `RunRecord.CreatedUtc` → `GoldenManifest.CommittedUtc`** rendered in the *Computed deltas* section of the first-value report (Markdown + PDF). |
| **Findings (total + by severity)** | Measures how much risk the agents surface that a human would otherwise miss | Severity mix should be defensible to a reviewer | **Yes — aggregated from `ArchitectureRunDetail.Results[*].Findings`.** |
| **LLM calls for the run** | Measures cost-shape and behavioural footprint of one run | Should fit the cost envelope agreed during pilot kickoff | **Yes — counted from per-run `AgentExecutionTrace` rows (sibling of the `archlucid_llm_calls_per_run` histogram).** |
| **Audit rows for the run** | Measures how thoroughly the run is observable / forensically reviewable | Higher is generally better, with caveats below | **Yes — `IAuditRepository.GetFilteredAsync(RunId)` (capped to 500 rows; if the cap is hit the value is shown as a lower bound).** |
| **Top-severity finding evidence chain** | Lets a reviewer trace one finding back to the manifest version, snapshot ids, and graph nodes used to produce it | A sponsor can hand a reviewer the chain ids and they resolve | **Yes — pulled from `IFindingEvidenceChainService` for the highest-severity finding on the run.** |
| **Time to reviewable artifact package** | Measures how quickly stakeholders can review something concrete | Faster package preparation with less manual assembly | No — operator-filled (qualitative). |
| **Manual preparation effort reduced** | Measures architect/admin time saved | Fewer hand-built documents, fewer manual stitching steps | No — operator-filled (qualitative). |
| **Decision traceability completeness** | Measures whether decisions and evidence are easier to explain | More complete, easier-to-follow review narrative | No — operator-filled (qualitative). |
| **Change visibility between runs** | Measures whether review of revisions is clearer | Stakeholders can see what changed and why more quickly | No — operator-filled (qualitative). |
| **Governance evidence readiness** | Measures whether approvals/reviews have better support material | Less reconstruction during review or approval prep | No — operator-filled (qualitative). |

### 4.1.1 How to read the demo numbers

The first-value report (`GET /v1/pilots/runs/{runId}/first-value-report` and the `…/first-value-report.pdf` companion) and the sponsor one-pager PDF compute the five "Computed by ArchLucid" rows above straight from persisted run state. **Baseline confidence appendix:** Markdown and PDF append **`## ROI evidence completeness`** (from `RoiEvidenceCompletenessMarkdownFormatter`) so sponsors see whether dollar narratives rely on tenant-captured baselines (**Strong / Partial**) or illustrative defaults (**Low confidence**).

Every first-value report also includes a **Buyer-safe proof package contract**. Treat that table as the send/no-send checklist before a sponsor email: architecture review identity, support run id, time to committed manifest, findings by severity, top finding evidence-chain pointer, audit-row count or lower bound, LLM-call count, ROI evidence confidence, and demo-data warning when applicable. Do not hand-edit missing fields into the report; either rerun the check, explain the gap, or mark the proof package incomplete.

When the report is generated **for one of the canonical Contoso Retail demo runs** (or any run whose `RequestId` carries the `req-contoso-demo-` prefix that `ContosoRetailDemoIds.ForTenant(...)` mints for non-default tenants), every report renders the banner:

> _demo tenant — replace before publishing._

Treat that banner as a **non-negotiable redaction marker**:

- **Do not screenshot** the computed-deltas table from a demo run for an external deck without removing the numbers or replacing them with figures from a live tenant.
- **Do not quote** "ArchLucid produced N findings in T minutes for our pilot" using a demo number — the seed is deterministic and was tuned for clarity, not to represent any specific customer's environment.
- **Do** use the demo numbers to walk a sponsor through *what the report will look like* once they run it against their own tenant — this is the entire point of having the seed render the same shape as a live pilot.

If you need to verify the matcher logic, the unit tests in `ArchLucid.Application.Tests/Pilots/PilotRunDeltaComputerTests.cs` and `ArchLucid.Application.Tests/Bootstrap/ContosoRetailDemoIdentifiersMatcherTests.cs` lock in both the canonical-RunId match and the multi-tenant `req-contoso-demo-*` prefix match.

### 4.2 Secondary pilot metrics

These matter, but should not dominate a first pilot.

| Metric | Why it matters |
|--------|----------------|
| **Operator onboarding time** | Shows whether first use is practical |
| **Support incidents / blockers** | Shows whether self-sufficiency is real enough |
| **Export usefulness** | Shows whether artifacts are usable outside the tool |
| **Reviewer confidence** | Shows whether the outputs are trusted, not just produced |

---

## 5. What a successful pilot should demonstrate

A successful pilot does **not** require every layer of the product.

For V1, success usually looks like this:

### 5.1 Minimum success bar

- A real architecture request was created and executed.
- The run produced a committed manifest.
- Stakeholders reviewed artifacts generated from that run.
- The team judged the output materially easier to review or package than the current-state approach.

### 5.2 Strong success bar

- The pilot reduced time from request to reviewable package.
- The pilot reduced manual artifact-preparation effort.
- Reviewers had clearer visibility into decisions, evidence, and changes.
- The team could explain why ArchLucid should be used again for similar architecture work.

### 5.3 Exceptional success bar

- The pilot created visible sponsor confidence.
- The team wants to expand into Operate (analysis workloads) or Operate (governance and trust).
- Governance, audit, or architecture review stakeholders actively prefer the ArchLucid flow.

---

## 6. Suggested pilot scorecard

Use a simple 1–5 rating for each item.

| Area | Question | Score 1–5 |
|------|----------|-----------|
| **Speed** | Did we get to a committed manifest faster or more predictably? | |
| **Artifact readiness** | Did we get to a reviewable package with less manual assembly? | |
| **Traceability** | Were decisions and evidence easier to explain? | |
| **Change clarity** | Was it easier to understand what changed between runs? | |
| **Governance readiness** | Did the pilot improve review or approval readiness? | |
| **Operator usability** | Could operators complete the Core Pilot path without excessive friction? | |
| **Stakeholder confidence** | Did reviewers trust the outputs enough to use them seriously? | |
| **Repeatability** | Would we use this again for a similar architecture request? | |

### Reading the scorecard

- **32–40** = strong pilot result
- **24–31** = promising, but more hardening or scope narrowing may be needed
- **Below 24** = pilot likely proved interest but not enough operational or business value yet

---

## 7. How sponsors can describe the result internally

Here is the kind of internal summary a sponsor should be able to use after a good pilot:

> ArchLucid shortened the path from architecture request to reviewable output, reduced manual packaging effort, and gave us a clearer evidence trail for review and governance. The pilot suggests that the product can improve architecture throughput and decision defensibility without requiring us to jump immediately into the full advanced feature set.

That is a credible V1 outcome statement.

---

## 8. What not to over-claim

Do **not** over-claim these from an early pilot unless you have direct evidence:

- enterprise-wide cost savings,
- broad productivity transformation,
- full governance automation,
- universal architecture standardization,
- reduced infrastructure spend,
- reduced headcount.

A strong V1 pilot should prove **workflow improvement and decision support**, not magic.

---

## 9. Best practice for pilot scope

For the cleanest ROI story:

- Use **one clear architecture use case**.
- Stay on the **Core Pilot** path first.
- Measure speed, packaging effort, and evidence quality.
- Only then expand into **Operate (analysis workloads)** or **Operate (governance and trust)**.

This keeps the pilot honest and makes sponsor judgment easier.

---

## 10. Summary

The most defensible ArchLucid pilot ROI story is simple:

- faster movement from request to committed manifest,
- less manual effort assembling reviewable architecture outputs,
- clearer visibility into decisions and changes,
- and better evidence for governance or architecture review.

If a pilot proves those four things, it is commercially meaningful.
