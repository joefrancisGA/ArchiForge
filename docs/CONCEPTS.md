> **Scope:** ArchLucid concept vocabulary - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# ArchLucid concept vocabulary

**Last reviewed:** 2026-04-20

This file is the canonical "**should I write X or Y?**" reference for ArchLucid documentation, code comments, runbooks, and operator-facing copy. It is **distinct from [`GLOSSARY.md`](GLOSSARY.md)**:

- `GLOSSARY.md` answers **"what does X mean?"** — short definitions for domain terms.
- `CONCEPTS.md` (this file) answers **"if X and Y are both possible, which one is canonical and why?"** — adjudication between competing terms. New ambiguities should land here as a row first; only then propagate to `GLOSSARY.md` for definition.

> **CI guard.** [`scripts/ci/check_concept_vocabulary.py`](../scripts/ci/check_concept_vocabulary.py) enforces a **minimal, conservative** subset of these rules at build time. Each rule is opt-in: the script only flags terms whose canonical-vs-rejected mapping appears in its `RULES` list, and only inside `docs/` (excluding `docs/archive/`). Adding a new rule to the script requires a corresponding row in this file with worked rationale.

## How to use this file

- **Adding new prose?** Skim § 1 first. If you have a choice between two phrasings, use the canonical one. If your case isn't listed, prefer the one that already appears most often in `docs/ARCHITECTURE_*.md` and the architecture ADRs.
- **Reviewing a PR?** If a doc change introduces a *rejected* term from § 1, ask the author to use the canonical form. The CI guard catches the rules in § 1.1; the others are reviewer-enforced until they prove safe to automate.
- **Proposing a new vocabulary rule?** Add a row to § 1 with the rationale. Only promote it to the CI guard's `RULES` list (§ 1.1) once you have ripgrep-confirmed that the rejected form does not appear in `docs/` (or that every existing occurrence is fixable in the same PR).

---

## 1 Canonical vocabulary

### 1.1 CI-enforced rules

The following rules are enforced by `scripts/ci/check_concept_vocabulary.py`. A PR that introduces a forbidden form will fail the `doc-markdown-links` job until corrected.

| # | Use this (canonical) | Don't use (rejected) | Rationale | First introduced |
|---|----------------------|----------------------|-----------|------------------|
| 1 | **Microsoft Entra ID** | "Azure Active Directory" or "Azure AD" | Microsoft renamed the service in 2023. Continued use of the legacy name confuses customers reading our security docs and contradicts our own [`SECURITY.md`](../SECURITY.md) and the Azure-native default in our `.cursor/rules/`. The rejected forms remain valid in **historical archive docs** (`docs/archive/**`) which the CI guard excludes. | 2026-04-20 (Improvement 6) |

### 1.2 Reviewer-enforced rules (not yet automated)

The following are encouraged but not blocking. Promote them to § 1.1 when ripgrep confirms zero false-positive risk.

| # | Use this (canonical) | Don't use (rejected) | Rationale |
|---|----------------------|----------------------|-----------|
| 2 | **Coordinator pipeline** / **Authority pipeline** (capitalized when naming the architecture concept) | lowercase "coordinator pipeline" / "authority pipeline" mid-sentence | The two pipelines are distinct architectural concepts, named in [`archive/dual-pipeline-navigator-superseded.md`](archive/dual-pipeline-navigator-superseded.md) and ADR 0010. Capitalizing them when used as concept names (not when used as common-noun adjectives) keeps reading scannable. Not yet automated because legitimate adjective uses ("the coordinator-side repository") would false-positive. |
| 3 | **`RunEventTrace`** (Coordinator) and **`RuleAuditTrace`** (Authority); **`DecisionTrace`** for the shared JSON envelope only | bare "decision trace" in prose when the meaning is one of the two specific subtypes | The 2026-04-06 namespace flatten gave the two subtypes distinct CLR types; bare "decision trace" in prose is now ambiguous. See `docs/archive/dual-pipeline-navigator-superseded.md` § *Architecture overview* row "Trace CLR type". Not yet automated because `DecisionTrace` is also a legitimate JSON wire-shape reference. |
| 4 | **golden manifest** (lowercase, two words) when used as a concept; **`GoldenManifest`** when referring to the CLR type | "Golden Manifest" (title-cased mid-sentence) | The CLR type is `GoldenManifest`; the prose noun is "the golden manifest". Title-casing mid-sentence implies a proper noun that doesn't exist. Not yet automated because heading text legitimately title-cases the term. |
| 5 | **operator** (the human role using the UI) | "user" when the meaning is "the architect / reviewer / governance reviewer using ArchLucid" | The UI is operator-shell-shaped; "user" is too generic and conflicts with the API auth principal model. See [`archlucid-ui/README.md`](../archlucid-ui/README.md). Not yet automated because "user" still has many legitimate uses (end customer, SQL user, etc.). |
| 6 | **trial** (free 14-day self-service) and **pilot** (paid guided 4-8 week engagement) — never interchangeable | "trial" used to mean "pilot", or vice versa | Pricing freeze of 2026-04-17 (locked the meanings); see [`docs/go-to-market/PRICING_PHILOSOPHY.md`](go-to-market/PRICING_PHILOSOPHY.md) §§ 5–6. Conflating them confuses the buyer narrative and the funnel metrics in `OBSERVABILITY.md`. Not yet automated because reviewer judgment is needed for borderline phrases. |

---

## 2 Carve-outs and "do not flag" cases

Some concepts intentionally have multiple acceptable spellings. Reviewers should not flag the following:

- **`ArchLucid` (PascalCase product name) vs `archlucid-` (lowercase, used in package names, container image names, env var prefixes, metric prefixes, dashboard file names, and CSS / Tailwind class names).** Both are correct; the lowercase form is required wherever a hostname / NPM package / OpenTelemetry meter name / Prometheus metric must obey a casing convention. Files like `infra/grafana/dashboard-archlucid-*.json` are correct as written.
- **Legacy `ArchiForge*` identifiers** still present in code (configuration keys, connection-string keys, Prometheus alert names, OTel metric prefixes for which a renamed metric would break customer dashboards). Governed by [`.cursor/rules/ArchLucid-Rename.mdc`](../.cursor/rules/ArchLucid-Rename.mdc); the `Guard — unexpected ArchiForge string literals in C#` CI step is the enforcement seam, not this concept guard.
- **Quotation of legacy or third-party terminology** in archived docs and historical change-set summaries (`docs/archive/**`). These read as receipts of past decisions and must not be retroactively rewritten.

---

## 3 Adding a new vocabulary rule

When two phrasings show up in PR review more than twice in a quarter, add a row here:

1. **Pick a canonical form.** Default to the form that already appears most often in `docs/ARCHITECTURE_*.md`, the architecture ADRs, and `docs/EXECUTIVE_SPONSOR_BRIEF.md` (in that priority order).
2. **List rejected forms.** Be explicit about word-boundary requirements (e.g. "Azure AD\b" not "AzureAD" — see § 1.1).
3. **Run the candidate ripgrep.** If `docs/` (excluding `docs/archive/**`) has more than ten existing occurrences of the rejected form, **fix them in the same PR** before adding the rule. Otherwise the next PR after yours will fail CI for unrelated reasons.
4. **Land the rule in § 1.2 first** (reviewer-enforced). Promote to § 1.1 (CI-enforced) only after one full release cycle with no false-positive PR comments.
5. **Update `scripts/ci/check_concept_vocabulary.py` `RULES` list.** Each entry must cite this file and the row number.

---

## 4 Related

- [`GLOSSARY.md`](GLOSSARY.md) — definitions of canonical terms.
- [`CANONICAL_PIPELINE.md`](CANONICAL_PIPELINE.md) — operator pipeline overview.
- [`archive/dual-pipeline-navigator-superseded.md`](archive/dual-pipeline-navigator-superseded.md) — archived contributor decision tree (engineering).
- [`docs/adr/README.md`](adr/README.md) — ADR index (ADR titles set the canonical naming for cross-cutting concerns).
- [`.cursor/rules/ArchLucid-Rename.mdc`](../.cursor/rules/ArchLucid-Rename.mdc) — separate, narrower enforcement of the ArchiForge → ArchLucid rename.
- [`scripts/ci/check_concept_vocabulary.py`](../scripts/ci/check_concept_vocabulary.py) — the CI guard implementing § 1.1.
