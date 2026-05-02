> **Scope:** Independent Decision Velocity solution-quality assessment (2026-05-02); buyer + operator decision-cycle view; excludes V1.1/V2 deferrals in [`V1_DEFERRED.md`](V1_DEFERRED.md) from **readiness penalty**; not a prior assessment update.

# Decision Velocity — Solution Quality 66 / 100 · Weighted readiness contribution **1.29%** (weight **2**, denominator **102**)

## 1. Objective

Assess how quickly **real organizations** move from curiosity to a defensible **pilot / purchase decision**, for ArchLucid V1 only.  
**Decision Velocity** here means **calendar-time and meeting-cycle friction**, not internal pipeline microseconds.

## 2. Assumptions

- V1 commercial motion is **sales-led** where self-serve live commerce is deferred; that deferral is **not** treated as a V1 defect for this score ([`V1_SCOPE.md`](V1_SCOPE.md) §3, [`V1_DEFERRED.md`](V1_DEFERRED.md) §6b).
- First-party **Jira / ServiceNow / Confluence / Slack / MCP** gaps are **V1.1/V2**; bridge docs/recipes may still affect *workflow* velocity but are not scored as “missing product promises.”
- **Third-party pen-test publication** and **public reference customers** are **out of V1** per deferral register; this write-up does **not** use their absence to drag the numeric score (it *does* note residual *practical* calendar drag where buyers ignore your scope boundaries).

## 3. Constraints

- Honest trust posture (no SOC 2 Type II opinion, owner-conducted pen posture) still consumes **legal/security calendar** in large enterprises—regardless of what V1 “should” exclude on paper.
- AI-assisted outputs remain **probabilistic** in live mode; evaluators often add **one extra proof loop** vs deterministic tools.
- Doc and UI surface area is large; **progressive disclosure** helps power users but can slow **first-time sponsors**.

## 4. Score

**66 / 100.**

**Weighted contribution to headline readiness** (same convention as [`ARCHLUCID_ASSESSMENT_WEIGHTED_READINESS_2026_05_02.md`](ARCHLUCID_ASSESSMENT_WEIGHTED_READINESS_2026_05_02.md)):  
\((66 \times 2) / 102 \approx\) **1.29%** of the Σ(weight)=**102** model.

## 5. Scoring justification (blunt)

| Sub-dimension | Approx. weight in this score | Rationale |
|----------------|------------------------------|-----------|
| **Operator / pilot path velocity** | 30% | Core Pilot is a tight **four-step** contract ([`CORE_PILOT.md`](../CORE_PILOT.md)); **`SECOND_RUN`** lowers friction for a real second architecture review; simulator defaults shorten time-to-output but can trigger **extra sponsor questions** in live evaluations. |
| **Buyer self-serve proof (no sales call)** | 25% | **Strong:** public demo commit-page preview and marketing paths ([`DEMO_PREVIEW.md`](DEMO_PREVIEW.md)), snapshot fallback, sponsor narrative ([`EXECUTIVE_SPONSOR_BRIEF.md`](../EXECUTIVE_SPONSOR_BRIEF.md)). **Weak:** first-touch visitors still must reconcile **run ID** vs **architecture review** hybrid vocabulary ([`PRODUCT_PACKAGING.md`](PRODUCT_PACKAGING.md)). |
| **Sponsor / economic decision** | 20% | **Strong:** pilot ROI model, computed deltas, proof-package contract ([`PILOT_ROI_MODEL.md`](PILOT_ROI_MODEL.md)); sponsor PDF path from run detail (per executive brief). **Weak:** baseline capture is optional—many deals still debate “is the numerator real?” |
| **Procurement / security questionnaire velocity** | 15% | **Strong:** SIG-themed accelerator, objection playbook, deterministic pack build story ([`PROCUREMENT_RESPONSE_ACCELERATOR.md`](../go-to-market/PROCUREMENT_RESPONSE_ACCELERATOR.md), [`PROCUREMENT_OBJECTION_PLAYBOOK.md`](../go-to-market/PROCUREMENT_OBJECTION_PLAYBOOK.md)). **Weak:** volume of documents remains high; buyers still parallelize reviews across **risk, legal, procurement**. |
| **Committee confidence in “decisions”** | 10% | **Strong:** decisioning golden corpus as a **merge-blocking** deterministic contract ([`DECISIONING_GOLDEN_CORPUS.md`](DECISIONING_GOLDEN_CORPUS.md)). **Weak:** explains *internal correctness*, not *external* acceptance—sponsors may still demand pilot evidence on *their* data. |

**Why not higher than ~70:** most enterprise “decisions” are **gated meetings**, not UX clicks. You removed several self-serve blockers by policy (deferred commerce, deferred ITSM), but you cannot remove **third-party diligence psychology** inside the quarter.

**Why not lower than ~60:** you ship uncommon **buyer-speed artifacts**—public demo preview, structured procurement responses, sponsor-facing measurement, and a bounded pilot contract—that many competitors still fake with slides.

## 6. Tradeoffs (commercially realistic)

- **Sales-led vs PLG:** Control and narrative consistency **up**; baseline “click-to-pilot” velocity **down**—accepted for V1 per deferral posture.
- **Thin demo vs deep demo:** Cached demo preview **scales** pre-sales; staleness up to TTL after re-seed ([`DEMO_PREVIEW.md`](DEMO_PREVIEW.md)) can create a **one-meeting correction** if a buyer hits an unlucky window—manageable but real.
- **Honest trust stance vs velocity:** Short answers in the objection playbook **accelerate** informed buyers; **noise-sensitive** committees still push deals to “come back with more paper.”
- **Operate surface richness:** Great for expansion; **risk** is evaluators who wander out of Core Pilot before the sponsor sees an outcome—[`OPERATOR_DECISION_GUIDE.md`](OPERATOR_DECISION_GUIDE.md) mitigates but does not eliminate misfocused pilots.

## 7. Findings — most improvement needed first (Decision Velocity lens)

Weighted roughly by **expected calendar impact on “go / no-go.”**

1. **Trust / diligence queue (practical, not scoring deferrals)** — Buyers will still run parallel security reviews; your docs are good, the **organizational process** is slow. *Mitigate with fast-lane framing, not more PDFs.*
2. **First-session cognitive load** — Authority tiers, progressive disclosure, hybrid vocabulary: competent buyers adapt; **busy sponsors** stall. *Default to Core Pilot ruthlessly in marketing + in-product.*
3. **Proof transfer from demo to tenant data** — Demo is credible; the decision flips when sponsors believe **their** review package. *Force one “non-demo” run story in every qualified opportunity.*
4. **LLM vs simulator narrative friction** — Simulator-first pilots are fast and cheap; some champions lose the room on “was this real AI?” *Pre-baked talking points and UI badges for execution mode.*
5. **Procurement volume** — Accelerator helps answer SIG rows; **some teams still outsource diligence** to vendors who want bespoke forms. *A one-page “how to ingest our pack” + escalation matrix.*
6. **Workflow embedding** — V1 relies on webhooks/recipes vs ITSM seats; **tech-savvy** buyers move; **low-maturity** buyers wait for “native Jira.” *Make the bridge recipe the default demo follow-on (deferred native connector is not a scored gap, but education still affects velocity).*
7. **Quote / order-form clarity** — [`ORDER_FORM_TEMPLATE.md`](../go-to-market/ORDER_FORM_TEMPLATE.md) and pricing philosophy reduce negotiation surface; placeholders and legal review **still** insert a week or two in real companies.
8. **Executive attention budget** — You have sponsor PDFs and briefs; overloaded execs **ghost** slow pilots unless the champion owns a dated decision milestone.

## 8. Improvement recommendations (high leverage)

- Ship a **procurement + pilot “fast lane” one-pager** (signed vs template vs unavailable; expected calendar; who escalates).
- **Instrument Core Pilot checklist** completion for sales engineers (server-derived counters are already described in [`CORE_PILOT.md`](../CORE_PILOT.md)) and **review dropout** before sponsor touch.
- **Execution-mode transparency** on run detail and exports (“simulator / real / echo”) in buyer-safe language.
- **Champion kit**: one Markdown + one DOCX, “first non-demo architecture review in 48 hours”—ties `SECOND_RUN` to sponsor metrics in [`PILOT_ROI_MODEL.md`](PILOT_ROI_MODEL.md).
- **Trust Center → pilot bridge**: three-link path from `/trust` to “what we do in pilot week 1” without reading [`V1_SCOPE.md`](V1_SCOPE.md).

---

## 9. Eight best improvements + one replacement (Cursor prompts)

### Improvement 1 — Procurement + pilot “fast lane” one-pager (single source of truth)

**Cursor prompt:**  
Create `docs/go-to-market/DECISION_FAST_LANE.md` with blockquote scope header. Content: one page max—**Pilot path** (4 steps + link [`CORE_PILOT.md`](../CORE_PILOT.md)), **Procurement path** (Trust Center → accelerator → pack request), **What is signed vs template** (DPA/MSA/order form posture per [`PROCUREMENT_OBJECTION_PLAYBOOK.md`](../go-to-market/PROCUREMENT_OBJECTION_PLAYBOOK.md)), **Typical calendar** (honest ranges), **Escalation triggers** (SOC2 attestation demand, third-party pen, custom DP terms). Add links from [`../trust-center.md`](../trust-center.md) and [`EXECUTIVE_ONE_EMAIL_KIT.md`](../go-to-market/EXECUTIVE_ONE_EMAIL_KIT.md). Run `python scripts/ci/check_doc_scope_header.py`.

### Improvement 2 — Core Pilot dropout telemetry for SE-led motions

**Cursor prompt:**  
Trace `POST /v1/diagnostics/core-pilot-rail-step` and `archlucid_core_pilot_rail_checklist_step_total` usage. Add a short operator-visible “resume checklist” on Home when steps are incomplete (no PII; use existing counters only). Document the sales-engineer workflow in `docs/runbooks/CORE_PILOT_SE_WORKFLOW.md` with scope header. Add or extend Vitest only if UI changes require it.

### Improvement 3 — Execution mode transparency (buyer-safe strings)

**Cursor prompt:**  
Audit run detail + first-value report surfaces for where `AgentExecution:Mode` (or equivalent persisted flags) is available. Add compact UI copy: **Simulator** (deterministic, no LLM cost), **Real** (live model), **Echo/other** if applicable—without leaking internal config keys. Ensure [`PILOT_ROI_MODEL.md`](PILOT_ROI_MODEL.md) §demo banners remain consistent. Add/update [`API_CONTRACTS.md`](API_CONTRACTS.md) snippet if response DTOs gain a stable `executionMode` field; keep backward compatibility.

### Improvement 4 — Champion kit: “48-hour second architecture review”

**Cursor prompt:**  
Add `docs/library/CHAMPION_48H_KIT.md` (scope header) linking [`SECOND_RUN.md`](SECOND_RUN.md), [`PILOT_ROI_MODEL.md`](PILOT_ROI_MODEL.md), and sponsor PDF endpoint from [`EXECUTIVE_SPONSOR_BRIEF.md`](../EXECUTIVE_SPONSOR_BRIEF.md). Include copy-paste email skeleton, success criteria, and explicit “no demo numbers in external slides” warning. Cross-link from [`DOGFOOD_PILOT_KIT.md`](DOGFOOD_PILOT_KIT.md) if present.

### Improvement 5 — Demo preview staleness operator note (reduce one-meeting surprises)

**Cursor prompt:**  
In `archlucid-ui` marketing preview pages, when API returns preview successfully, add a subtle footer line: “Demo data may be cached up to ~5 minutes after re-seed” (align [`DEMO_PREVIEW.md`](DEMO_PREVIEW.md)). Keep `noindex`. Add a11y-safe text only; no new network calls.

### Improvement 6 — Trust Center → Core Pilot bridge (three links) + pricing honesty

**Cursor prompt:**  
Update `docs/trust-center.md` (respect docs-root budget if applicable—if at cap, add section via `docs/library/` and link) with a **“Start pilot”** strip: Core Pilot, Pilot Guide, Trust artifacts index. Goal: security reader can route to pilot motion without scanning [`V1_SCOPE.md`](V1_SCOPE.md). In the same pass, review `archlucid-ui` `/pricing` and quote-request UX against [`PRICING_PHILOSOPHY.md`](../go-to-market/PRICING_PHILOSOPHY.md) §quote path: primary CTA must state **inbox / sales follow-up**, not silent tenant provisioning; if `teamStripeCheckoutUrl` remains a placeholder, ensure UI copy does not promise live self-serve where motion is sales-led (copy-only if code already guards).

### Improvement 7 — SIG accelerator “deal desk” row (ingestion hygiene)

**Cursor prompt:**  
Append a compact table to [`PROCUREMENT_RESPONSE_ACCELERATOR.md`](../go-to-market/PROCUREMENT_RESPONSE_ACCELERATOR.md): **When buyer sends bespoke questionnaire**, map to **Evidence** column + **Deferral** pointer to [`V1_DEFERRED.md`](V1_DEFERRED.md) where relevant. Keep statuses honest. Run procurement CI scripts if paths change.

### Improvement 8 — **DEFERRED** — Binding signature on customer Order Form / MSA / DPA

Only customer legal / business signatories can execute contracts. No automation substitute. **No Cursor prompt.**  
**Partial work** is still possible via template hygiene and fast-lane docs (Improvements 1, 7).

### Improvement 9 — Decision-record template for steering committees *(replacement for Improvement 8)*

**Cursor prompt:**  
Add `docs/go-to-market/STEERING_DECISION_MEMO_TEMPLATE.md` with scope header: one-page decision memo aligned to [`PILOT_ROI_MODEL.md`](PILOT_ROI_MODEL.md) scorecard rows—**Decision**, **Alternatives**, **Pilot scope**, **Success measures**, **Date / owners**. Link from [`EXECUTIVE_SPONSOR_BRIEF.md`](../EXECUTIVE_SPONSOR_BRIEF.md) §Related.

---

## 10. Pending questions for the owner (answer when you have time)

These **do not block** engineering partial work above:

1. **Target buyer motion for H1:** design-partner only vs broader mid-market outbound (affects how hard to optimize self-serve demo vs SE-led).
2. **CRM routing for quote requests:** pricing philosophy still notes owner CRM routing beyond inbox mail—who owns SLA?
3. **Pilot success gate:** is “committed manifest on customer data” the firm internal bar for a paid proposal conversation?

---

## 11. Uncertainty

- **Hosted funnel conversion rates** and **real wall-clock** pilot lengths are not derivable from repo evidence alone.
- **Buyer committee behavior** varies by industry; financial services and public sector may ignore fast-lane docs unless portals match their workflow.

---

## 12. Security · scalability · reliability · cost (Decision Velocity only)

| Dimension | Impact on Decision Velocity |
|-----------|-----------------------------|
| **Security** | Faster decisions when buyers believe controls; slower when Assurance gaps trigger committee review—playbook reduces surprise, not anthropology. |
| **Scalability** | Demo preview caching supports **many parallel evaluators** without burning SQL ([`DEMO_PREVIEW.md`](DEMO_PREVIEW.md))—reduces scheduling drag. |
| **Reliability** | Hosted health and staging URLs in packaging docs—**downtime kills** evaluation momentum harder than feature gaps. |
| **Cost** | Simulator-first pilots **accelerate** decisions by removing token budget fear; real-mode budget caps need a crisp SE story ([`PRICING_PHILOSOPHY.md`](../go-to-market/PRICING_PHILOSOPHY.md) hosted AOAI guardrails). |
