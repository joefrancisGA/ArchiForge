# ArchLucid procurement pack

This bundle was produced by **`scripts/build_procurement_pack.py`** (or `archlucid procurement-pack`). **Start here**, then open the indexes below.

## What to open next

| File | Purpose |
| --- | --- |
| **`ARTIFACT_STATUS_INDEX.md`** | Table of **Evidence** vs **Template** vs **Self-assessment** vs **Deferred** — use this so templates are not mistaken for attestations. |
| **`artifact_status_index.json`** | Same classification in JSON (automation / SIEM). |
| **`manifest.json`** | Per-file **SHA-256**, size, and **`artifact_status`**. |
| **`versions.txt`** | Git commit, build UTC, CLI package version. |
| **`redaction_report.md`** | Repository paths **intentionally omitted** from the canonical pack and why. |

## Operator reference

- **How to regenerate:** in a full clone, see **`docs/go-to-market/HOW_TO_REQUEST_PROCUREMENT_PACK.md`**.
- **Strict placeholder scan:** **`--strict`** or **`PROCUREMENT_PACK_STRICT=1`** on release drops (same doc).

**Do not** treat **Template** or **Self-assessment** artifacts as executed legal agreements or third-party certifications.
