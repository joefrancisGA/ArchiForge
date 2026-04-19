# ArchLucid Commercial Boundary Hardening Sequence

**Audience:** product, sales, architecture, and go-to-market stakeholders who need a practical sequence for turning ArchLucid’s current layer model into stronger commercial boundaries over time.

**Status:** Future-state commercialization guidance. This document does **not** implement licensing, billing, entitlement, or pricing enforcement. It explains **how boundary hardening should happen in sequence** so the product gains commercial discipline without damaging the Core Pilot wedge.

**Related:** [FUTURE_PACKAGING_ENFORCEMENT.md](FUTURE_PACKAGING_ENFORCEMENT.md) · [PRODUCT_PACKAGING.md](PRODUCT_PACKAGING.md) · [EXECUTIVE_SPONSOR_BRIEF.md](EXECUTIVE_SPONSOR_BRIEF.md) · [PILOT_ROI_MODEL.md](PILOT_ROI_MODEL.md) · [archlucid-ui/README.md](../archlucid-ui/README.md#role-aware-shaping-first-wave) (Stage 1 UI shipped)

---

## 1. Why sequencing matters

ArchLucid now has a strong three-layer model:

- **Core Pilot**
- **Advanced Analysis**
- **Enterprise Controls**

The next commercialization problem is not whether the model exists. The problem is **how to harden the right boundaries in the right order**.

If boundaries harden too early, the product risks weakening the first successful buying motion. If they never harden, the product risks remaining commercially soft even as it becomes more capable.

---

## 2. Guiding rule

Use this rule:

> **Harden only the boundaries that improve clarity, trust, or monetization without making the Core Pilot result harder to buy or prove.**

That means:

- keep the wedge simple,
- harden where operational accountability matters,
- commercialize only after value is legible,
- avoid gating anything essential to proving first success.

---

## 3. What should stay soft for now

The following should remain intentionally soft in the near term:

- the conceptual distinction between first-pilot work and deeper analysis,
- explanatory guidance about when to stay narrow versus expand,
- the basic operator path for creating a run, committing, and reviewing artifacts,
- the basic ROI proof path,
- the minimum evidence story required to trust the pilot.

These help adoption and should not be commercialized aggressively too early.

---

## 4. Stage 1 hardening — stronger role clarity and UI-native shaping

### Objective

Reduce ambiguity and dependency on documentation alone.

### Hardening moves

- reinforce layer usage directly in the UI,
- make “use this when” and “ignore this unless” guidance more visible in product surfaces,
- shape advanced surfaces more explicitly by context,
- clarify where operator/admin responsibility changes the expected experience.

**Shipped in `archlucid-ui` (first wave, not commercial gating):** tier + `requiredAuthority` nav shaping (`nav-config.ts`, `nav-authority.ts`, `nav-shell-visibility.ts`, `OperatorNavAuthorityProvider.tsx`, `current-principal.ts`), plus short Enterprise context copy (`enterprise-controls-context-copy.ts`, `EnterpriseControlsContextHints.tsx`, `layer-guidance.ts` `enterpriseFootnote`) and route-level **`LayerHeader.tsx`** (keys in `layer-guidance.ts`). **Execute-tier** in-page write affordances use `enterprise-mutation-capability.ts` / `useEnterpriseMutationCapability` so **button enablement** tracks the **same numeric rank** as **nav link visibility** (both derive from `/me` claims; API responses remain authoritative). **Core Pilot** remains the default path; **Enterprise Controls** are the primary place for stricter shaping. This does **not** implement billing, entitlements, or plan-based feature flags—see **Stage 2** for stronger role-enforced boundaries as a separate step.

**Read vs Execute (same threshold, two surfaces):** **`navLinkVisibleForCallerRank`** only controls **whether a link appears** after tier filtering. **`useEnterpriseMutationCapability()`** uses **`rank >= AUTHORITY_RANK.ExecuteAuthority`** for **soft-disabled controls** on mutation-heavy pages — not a second policy matrix; keep both aligned with **C#** `[Authorize(Policy = …)]` when routes move.

**Deep links:** hiding a destination in the shell does **not** remove the route — callers can still open URLs directly; **ArchLucid.Api** returns **401/403** when the token does not satisfy the controller policy.

**Maintenance map:** which TypeScript files correspond to which packaging layer is summarized in **PRODUCT_PACKAGING.md** §3 *Code seams (operator UI — maintenance map)*—update that table when you add groups or change the shaping pipeline.

**Regression anchors:** **docs/PRODUCT_PACKAGING.md** §3 *Contributor drift guard* lists **Vitest** files that guard rank thresholds, nav composition (default Enterprise **Reader** strip, **tier-before-authority**, **Core Pilot** extended **Execute** links such as **`/replay`**), JWT `/me` refetch conservative rank (`OperatorNavAuthorityProvider.test.tsx`), cross-module seams (`authority-seam-regression.test.ts` — includes Auditor vs Reader Enterprise filtering, **caller rank `0` vs `ReadAuthority`** nav, **`/alerts`** **`essential`** tier invariant, **stable ordering** after filters, **`LAYER_PAGE_GUIDANCE`** **Enterprise** vs **Advanced** **`enterpriseFootnote`** so **`LayerHeader`** packaging cannot drift from nav layers, **Enterprise** href **monotonicity** Read→Execute→Admin, **Advanced** default shell **`/ask`**-only, and **`/governance`** behind extended+advanced at **Execute** rank), plus **`authority-execute-floor-regression.test.ts`** (minimal **Execute floor**: synthetic **`ExecuteAuthority`** nav visibility **≡** **`enterpriseMutationCapabilityFromRank`**; real **`alerts-governance`** monotonicity and Reader **`/governance`** omission), **`current-principal`** vs mutation-capability and **`maxAuthority`** alignment, **LayerHeader** Enterprise rank cue + **`aside`** **`aria-label`** coverage, rank-gated Enterprise copy (**`EnterpriseControlsContextHints`** — alert tooling, governance resolution, audit log, **Alerts inbox**, **governance dashboard**), **`nav-config.structure.test.ts`** (href dedupe; **Core Pilot** essentials omit **`requiredAuthority`**; **ExecuteAuthority** not on **`essential`** tier), and **page-level** mutation affordances (**`archlucid-ui/src/app/(operator)/enterprise-authority-ui-shaping.test.tsx`** — policy packs, **alert rules**, alerts triage, **Governance** submit **`readOnly`**) — extend that list when you add a new shaping surface tied to `/me` or Enterprise POST/toggle UI.

### Why this comes first

This improves adoption and clarity without risking the Core Pilot wedge.

### Good Stage 1 candidates

- Advanced Analysis surfaces
- Governance dashboards and related Enterprise Controls entry points
- advanced alert and audit surfaces
- post-Core-Pilot expansion guidance

---

## 5. Stage 2 hardening — clearer role-enforced boundaries

### Objective

Make responsibility-based boundaries more real where operational accountability matters.

### Hardening moves

- strengthen operator/admin restrictions for governance and audit features,
- make policy, audit, and alert-management capabilities more explicitly role-bound,
- reduce ambiguity about who should use which Enterprise Controls surfaces.

### Why this comes second

These boundaries are easier to justify because they align with operational responsibility, not just commercial packaging.

### Good Stage 2 candidates

- governance approvals
- policy pack management
- audit log export
- alert tuning, routing, and simulation
- governance dashboard administration surfaces

---

## 6. Stage 3 hardening — selective plan-based boundaries

### Objective

Introduce commercial differentiation only where value is already understandable and non-essential to proving first success.

### Hardening moves

- identify advanced capabilities whose value is legible enough to support a higher plan,
- distinguish “useful for deeper maturity” from “required to prove the product,”
- preserve the Core Pilot wedge while differentiating more advanced commercial offers.

### Why this comes third

Plan-based gating is most dangerous when introduced too early. It should follow value proof, not precede it.

### Good Stage 3 candidates

- advanced replay and comparison depth,
- deeper graph / provenance analysis depth,
- advanced governance analytics,
- historical retention/export depth,
- premium advisory or recommendation-learning surfaces.

---

## 7. What not to hard-gate too early

Do **not** hard-gate these too early:

- creating a request,
- executing a run,
- committing a manifest,
- reviewing artifacts,
- basic export of a reviewable package,
- basic trust and evidence needed to prove the pilot,
- the minimal workflow a sponsor needs to judge the product.

These define the wedge. If they are weakened, the product becomes harder to buy.

---

## 8. Recommended hardening sequence

| Stage | Primary goal | Boundary type to strengthen first |
|---|---|---|
| **Stage 1** | Reduce ambiguity and cognitive load | UI-native shaping and contextual guidance |
| **Stage 2** | Improve operational accountability | Role-enforced boundaries |
| **Stage 3** | Introduce stronger commercial differentiation | Selective plan-based boundaries |

This sequence keeps commercialization disciplined without turning the product into a licensing exercise too early.

---

## 9. What future commercialization work would build on

The next layer of product work can build on:

- the current layer model,
- progressive disclosure in the UI,
- role-aware usage patterns,
- pricing and positioning artifacts,
- and later entitlement/billing controls if product-market evidence justifies them.

The key is to harden the boundaries in the same order that customer value becomes clear.

---

## 10. Summary

ArchLucid should not rush from good packaging narrative into hard commercial gating.

The right sequence is:

1. **clarify and shape,**
2. **then role-harden,**
3. **then selectively commercialize.**

That protects the Core Pilot wedge while giving the product room to mature into stronger commercial boundaries.
