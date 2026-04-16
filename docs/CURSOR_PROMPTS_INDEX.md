# Cursor prompts index

Paste-ready Agent prompts for common improvement tracks. Prefer these slugs when bookmarking or cross-linking from checklists.

| Slug / topic | File | Summary |
|--------------|------|---------|
| Six weighted quality improvements (coverage, security, rename, traceability, explainability, usability) | [CURSOR_PROMPTS_SIX_QUALITY_IMPROVEMENTS.md](CURSOR_PROMPTS_SIX_QUALITY_IMPROVEMENTS.md) | Coverage gap, Stryker, dev-bypass guard, RBAC spot-check, rename artifacts, RFC 9457, etc. |
| Weighted improvement 3 (rename, archive quality docs, legacy sunset) | [CURSOR_PROMPTS_WEIGHTED_IMPROVEMENT_3.md](CURSOR_PROMPTS_WEIGHTED_IMPROVEMENT_3.md) | Single `.sln`, remove orphan `ArchiForge/` sources, `ArchLucid.Api.http`, config bridge sunset |
| Weighted improvements 3–6 (verification bundle) | [CURSOR_PROMPTS_WEIGHTED_IMPROVEMENTS_3_TO_6.md](CURSOR_PROMPTS_WEIGHTED_IMPROVEMENTS_3_TO_6.md) | Traceability matrix, orphan probe, finding narrative, first-run wizard parity, RFC 9457 sweep |
| Quality improvement 3 (alternate path) | [CURSOR_PROMPTS_QUALITY_IMPROVEMENT_3.md](CURSOR_PROMPTS_QUALITY_IMPROVEMENT_3.md) | Legacy pointer; prefer weighted doc set above when unsure |
| SaaS improvements 2–6 | [CURSOR_PROMPTS_SAAS_IMPROVEMENTS_2_TO_6.md](CURSOR_PROMPTS_SAAS_IMPROVEMENTS_2_TO_6.md) | Marketability / SaaS-oriented prompts |
| Canonical navigation (single entry) | [CURSOR_PROMPTS_CANONICAL.md](CURSOR_PROMPTS_CANONICAL.md) | If present, use as the authoritative prompt hub |

**Doc hygiene automation:** `python scripts/ci/check_doc_links.py` (merge-blocking in CI) and `python scripts/ci/check_doc_freshness.py` (informational; runbook **Last reviewed:** dates).

**Last reviewed:** 2026-04-16
