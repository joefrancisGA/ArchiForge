> **Scope:** How buyers and field teams obtain the ArchLucid procurement documentation ZIP.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.

# How to request the procurement pack

## Who this is for

- **Buyers / procurement** requesting a single documentation drop.
- **Sales engineering / customer success** assembling a diligence bundle without hand-picking Markdown paths.

## One command (recommended)

From a clone of the ArchLucid repository (with the .NET SDK and **Python 3** installed):

```bash
archlucid procurement-pack --out archlucid-procurement-pack.zip
```

```powershell
archlucid procurement-pack --out .\archlucid-procurement-pack.zip
```

The command runs `scripts/build_procurement_pack.py`, verifies **every canonical file** exists, writes `dist/procurement-pack/` (staging) and the ZIP. Inside the ZIP you will find:

- `README.md` — **start here** — pointers to artifact classification and provenance files
- `manifest.json` — each file’s **size**, **SHA-256**, and **`artifact_status`**
- `versions.txt` — **git commit**, build timestamp, and **CLI package version**
- `redaction_report.md` — repository paths **intentionally omitted** from the canonical checklist and why
- `artifact_status_index.json` — machine-readable **`artifact_status`** per packed path (mirrors `scripts/procurement_pack_canonical.json`)
- `ARTIFACT_STATUS_INDEX.md` — **Evidence** vs **Template** vs **Self-assessment** vs **Deferred** table for buyers

### Validate without writing a ZIP (CI / pre-commit)

```bash
python scripts/build_procurement_pack.py --dry-run
```

### Release / buyer drop — placeholder strictness

For a **release** or **procurement** drop, run the builder with **`--strict`** (or set environment variable **`PROCUREMENT_PACK_STRICT=1`**) so **Evidence** and **Self-assessment** text files are scanned for buyer-unsafe markers (`TODO`, `TBD`, `placeholder-replace-before-launch`). **`Template`** and **`Deferred`** pack rows are excluded from this scan by design.

```bash
python scripts/build_procurement_pack.py --strict
```

```powershell
$env:PROCUREMENT_PACK_STRICT = "1"
python scripts/build_procurement_pack.py
```

## Script-only (advanced)

```bash
./scripts/build_procurement_pack.sh
```

```powershell
./scripts/build_procurement_pack.ps1
```

Both wrappers invoke the same Python builder. **Default CI** should keep using **`--dry-run`** (assemblability only). Use **`--strict`** or **`PROCUREMENT_PACK_STRICT`** only on release/procurement jobs so merge-blocking gates do not depend on draft markers inside **Template**/**Deferred** pack rows.

### Deal-ready preflight (recommended before sending to buyer)

Run deal-ready mode for a stricter gate that includes canonical assurance coherence references, required buyer-contact checks, and **Last reviewed** freshness for required buyer-facing docs.

```bash
python scripts/build_procurement_pack.py --deal-ready
```

```powershell
$env:PROCUREMENT_PACK_DEAL_READY = "1"
python scripts/build_procurement_pack.py
```

By default, deal-ready mode fails when required buyer-facing docs are more than **120 days** past their `**Last reviewed:** YYYY-MM-DD` marker. Override only with an explicit owner decision:

```bash
python scripts/build_procurement_pack.py --deal-ready --max-review-age-days 180
```

## After generating the ZIP

1. Complete a buyer-specific cover letter using [`PROCUREMENT_PACK_COVER.md`](PROCUREMENT_PACK_COVER.md) **outside** the committed tree (see scaffold warnings there).
2. Send the ZIP **or** upload it to the buyer’s secure file exchange — do not email large binaries to unauthenticated mailboxes.

## Trust Center index

For narrative context and deep links, start at [`TRUST_CENTER.md`](TRUST_CENTER.md).
