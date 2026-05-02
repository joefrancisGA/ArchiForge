#!/usr/bin/env python3
"""
Assemble dist/procurement-pack/ + dist/procurement-pack.zip for buyer / procurement teams.

Canonical file list: scripts/procurement_pack_canonical.json (shared with CI:
scripts/ci/assert_procurement_pack_buildable.py).

Emits:
  - README.md — buyer-facing entry (artifact index pointers; assessment 76_76 §9)
  - manifest.json — each packed file: pack_path, source_repo_path, bytes, sha256, artifact_status
  - versions.txt — git commit, build UTC, ArchLucid CLI version (from ArchLucid.Cli.csproj)
  - redaction_report.md — files intentionally not included and why
  - artifact_status_index.json — machine-readable artifact_status per packed path
  - ARTIFACT_STATUS_INDEX.md — buyer-facing table of evidence vs template vs deferred rows
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import subprocess
import sys
import shutil
import zipfile
from datetime import datetime, timezone
from pathlib import Path

TEXT_PACK_SUFFIXES = frozenset({".md", ".txt", ".json", ".yaml", ".yml", ".html", ".xml", ".csv"})

# Buyer-unsafe placeholder tokens (release / `--strict` builds only; skipped for Template/Deferred entries).
_PLACEHOLDER_PATTERNS: tuple[re.Pattern[str], ...] = (
    re.compile(r"\bTBD\b", re.IGNORECASE),
    re.compile(r"\bTODO\b", re.IGNORECASE),
    re.compile(r"placeholder-replace-before-launch", re.IGNORECASE),
)

_DEAL_READY_PATTERNS: tuple[re.Pattern[str], ...] = (
    *_PLACEHOLDER_PATTERNS,
    re.compile(r"\[Legal\s*[-—]\s*describe\]", re.IGNORECASE),
)


def entry_should_scan_for_placeholders(entry: dict) -> bool:
    status = entry.get("artifact_status", "Evidence")
    if status in ("Template", "Deferred"):
        return False

    path = Path(entry["pack_path"])
    suf = path.suffix.lower()

    return suf in TEXT_PACK_SUFFIXES


def scan_packed_files_for_markers(
    stage: Path,
    entries: list[dict],
    patterns: tuple[re.Pattern[str], ...],
    allowed_statuses: tuple[str, ...] | None = None,
) -> list[str]:
    violations: list[str] = []
    for e in entries:
        if not entry_should_scan_for_placeholders(e):
            continue
        if allowed_statuses is not None and e.get("artifact_status", "Evidence") not in allowed_statuses:
            continue

        pack_path = e["pack_path"]
        target = stage / pack_path
        text = target.read_text(encoding="utf-8", errors="replace")

        for pat in patterns:
            if pat.search(text) is not None:
                violations.append(f"{pack_path}: matched /{pat.pattern}/")
                break

    return violations


def write_artifact_status_index(stage: Path, entries: list[dict]) -> None:
    rows: list[dict] = []
    for e in entries:
        rows.append(
            {
                "pack_path": e["pack_path"],
                "artifact_status": e.get("artifact_status", "Evidence"),
                "description": e.get("description", ""),
            }
        )

    (stage / "artifact_status_index.json").write_text(
        json.dumps({"generated_utc": datetime.now(timezone.utc).isoformat(), "files": rows}, indent=2) + "\n",
        encoding="utf-8",
    )

    lines = [
        "# Artifact status index",
        "",
        "Each row reflects `artifact_status` from the canonical procurement list (`scripts/procurement_pack_canonical.json`).",
        "",
        "| Pack file | Status | Description |",
        "| --- | --- | --- |",
    ]
    for r in rows:
        desc = str(r.get("description", "")).replace("|", "\\|")
        lines.append(f"| `{r['pack_path']}` | **{r['artifact_status']}** | {desc} |")

    lines.append("")
    (stage / "ARTIFACT_STATUS_INDEX.md").write_text("\n".join(lines), encoding="utf-8")


def write_pack_readme(stage: Path) -> None:
    """Buyer-facing entrypoint inside the ZIP — points at artifact classification (assessment 76_76 §9 item 4)."""
    body = """# ArchLucid procurement pack

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
"""
    (stage / "README.md").write_text(body, encoding="utf-8")


def repo_root() -> Path:
    return Path(__file__).resolve().parents[1]


def load_canonical(root: Path) -> tuple[list[dict], list[dict]]:
    path = root / "scripts" / "procurement_pack_canonical.json"
    data = json.loads(path.read_text(encoding="utf-8"))
    return data["canonical_entries"], data["excluded_from_canonical_pack"]


def read_cli_version(root: Path) -> str:
    csproj = root / "ArchLucid.Cli" / "ArchLucid.Cli.csproj"
    text = csproj.read_text(encoding="utf-8")
    m = re.search(r"<Version>([^<]+)</Version>", text)
    if m:
        return m.group(1).strip()
    return "unknown"


def git_head(root: Path) -> str:
    try:
        r = subprocess.run(
            ["git", "-C", str(root), "rev-parse", "HEAD"],
            capture_output=True,
            text=True,
            check=True,
            timeout=30,
        )
        return r.stdout.strip()
    except (subprocess.CalledProcessError, FileNotFoundError, subprocess.TimeoutExpired):
        return os.environ.get("GITHUB_SHA", "unknown").strip() or "unknown"


def build_manifest_rows(stage: Path, entries: list[dict]) -> list[dict]:
    rows: list[dict] = []
    for e in entries:
        pack = e["pack_path"]
        p = stage / pack
        raw = p.read_bytes()
        digest = hashlib.sha256(raw).hexdigest()
        rows.append(
            {
                "pack_path": pack,
                "source_repo_path": e["source_repo_path"],
                "bytes": len(raw),
                "sha256": digest,
                "artifact_status": e.get("artifact_status", "Evidence"),
            }
        )
    return rows


def write_versions_txt(stage: Path, root: Path) -> None:
    sha = git_head(root)
    ver = read_cli_version(root)
    now = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")
    lines = [
        f"git_commit_sha={sha}",
        f"built_utc={now}",
        f"archlucid_cli_version={ver}",
        "",
        "Built by scripts/build_procurement_pack.py — see docs/go-to-market/HOW_TO_REQUEST_PROCUREMENT_PACK.md.",
    ]
    (stage / "versions.txt").write_text("\n".join(lines) + "\n", encoding="utf-8")


def write_redaction_report(stage: Path, excluded: list[dict]) -> None:
    lines = [
        "# Redaction / omission report",
        "",
        "The canonical procurement ZIP (see `scripts/procurement_pack_canonical.json`) **includes only** the reviewer checklist. "
        "The following repository paths are **not** copied into this pack and are listed here so owners can audit gaps.",
        "",
        "| Repository path | Reason |",
        "|-----------------|--------|",
    ]
    for row in excluded:
        path = row.get("path", "")
        reason = row.get("reason", "").replace("|", "\\|")
        lines.append(f"| `{path}` | {reason} |")

    lines.append("")
    lines.append("**Do not** add unredacted customer names or deal-specific cover letter text without owner sign-off.")
    (stage / "redaction_report.md").write_text("\n".join(lines) + "\n", encoding="utf-8")


def validate_sources(root: Path, entries: list[dict]) -> list[str]:
    missing: list[str] = []
    for e in entries:
        src = root / e["source_repo_path"]
        if not src.is_file():
            missing.append(e["source_repo_path"])
    return missing


def deal_ready_repo_checks(root: Path, entries: list[dict]) -> list[str]:
    violations: list[str] = []
    required_docs = (
        root / "docs" / "go-to-market" / "ASSURANCE_STATUS_CANONICAL.md",
        root / "docs" / "go-to-market" / "TRUST_CENTER.md",
        root / "docs" / "go-to-market" / "SOC2_STATUS_PROCUREMENT.md",
        root / "docs" / "go-to-market" / "CURRENT_ASSURANCE_POSTURE.md",
        root / "docs" / "go-to-market" / "INCIDENT_COMMUNICATIONS_POLICY.md",
    )

    for p in required_docs:
        if not p.is_file():
            violations.append(f"missing required deal-ready doc: {p.relative_to(root).as_posix()}")
            continue

        text = p.read_text(encoding="utf-8", errors="replace")
        if "ASSURANCE_STATUS_CANONICAL.md" not in text and p.name != "ASSURANCE_STATUS_CANONICAL.md":
            violations.append(f"{p.relative_to(root).as_posix()}: missing canonical assurance status reference")

    trust = root / "docs" / "go-to-market" / "TRUST_CENTER.md"
    incident = root / "docs" / "go-to-market" / "INCIDENT_COMMUNICATIONS_POLICY.md"
    if trust.is_file() and "security@archlucid.net" not in trust.read_text(encoding="utf-8", errors="replace"):
        violations.append("TRUST_CENTER.md: missing security contact mailbox")
    if incident.is_file() and "security@archlucid.net" not in incident.read_text(encoding="utf-8", errors="replace"):
        violations.append("INCIDENT_COMMUNICATIONS_POLICY.md: missing fallback security contact mailbox")

    missing_status = [
        e.get("pack_path", "")
        for e in entries
        if not e.get("artifact_status")
    ]
    if missing_status:
        violations.append("canonical entries missing artifact_status: " + ", ".join(missing_status))

    return violations


def main() -> int:
    parser = argparse.ArgumentParser(description="Build ArchLucid procurement pack ZIP.")
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Verify all canonical sources exist; do not write dist/ output.",
    )
    parser.add_argument(
        "--out",
        type=Path,
        default=None,
        help="Output ZIP path (default: dist/procurement-pack.zip under repo root).",
    )
    parser.add_argument(
        "--strict",
        action="store_true",
        help="After staging, fail if Evidence/Self-assessment text files contain buyer-unsafe placeholders (TBD/TODO/...).",
    )
    parser.add_argument(
        "--deal-ready",
        action="store_true",
        help="Run stricter release/procurement checks (implies --strict) for buyer-facing packs.",
    )
    args = parser.parse_args()

    root = repo_root()
    entries, excluded = load_canonical(root)

    missing = validate_sources(root, entries)
    if missing:
        print("error: canonical procurement pack sources missing:", file=sys.stderr)
        for m in missing:
            print(f"  - {m}", file=sys.stderr)
        return 1

    strict_env = os.environ.get("PROCUREMENT_PACK_STRICT", "").strip().lower() in ("1", "true", "yes")
    deal_ready_env = os.environ.get("PROCUREMENT_PACK_DEAL_READY", "").strip().lower() in ("1", "true", "yes")
    deal_ready = args.deal_ready or deal_ready_env
    strict = args.strict or strict_env or deal_ready

    if args.dry_run:
        print("procurement pack dry-run: OK (all canonical sources present)")
        return 0

    stage = root / "dist" / "procurement-pack"
    if stage.exists():
        shutil.rmtree(stage)

    stage.mkdir(parents=True)

    for e in entries:
        src = root / e["source_repo_path"]
        dst = stage / e["pack_path"]
        dst.parent.mkdir(parents=True, exist_ok=True)
        dst.write_bytes(src.read_bytes())

    if strict:
        violations = scan_packed_files_for_markers(
            stage,
            entries,
            _PLACEHOLDER_PATTERNS,
            allowed_statuses=("Evidence", "Self-assessment"),
        )
        if violations:
            print("error: procurement pack strict mode found placeholders in buyer-facing files:", file=sys.stderr)
            for v in violations:
                print(f"  - {v}", file=sys.stderr)
            return 1

    if deal_ready:
        deal_violations = scan_packed_files_for_markers(
            stage,
            entries,
            _DEAL_READY_PATTERNS,
            allowed_statuses=("Evidence", "Self-assessment"),
        )
        deal_violations.extend(deal_ready_repo_checks(root, entries))
        if deal_violations:
            print("error: procurement pack deal-ready checks failed:", file=sys.stderr)
            for v in deal_violations:
                print(f"  - {v}", file=sys.stderr)
            return 1

    manifest_rows = build_manifest_rows(stage, entries)
    (stage / "manifest.json").write_text(
        json.dumps({"generated_utc": datetime.now(timezone.utc).isoformat(), "files": manifest_rows}, indent=2)
        + "\n",
        encoding="utf-8",
    )
    write_versions_txt(stage, root)
    write_redaction_report(stage, excluded)
    write_artifact_status_index(stage, entries)
    write_pack_readme(stage)

    out_zip = args.out if args.out is not None else root / "dist" / "procurement-pack.zip"
    out_zip.parent.mkdir(parents=True, exist_ok=True)
    if out_zip.exists():
        out_zip.unlink()

    with zipfile.ZipFile(out_zip, "w", compression=zipfile.ZIP_DEFLATED) as zf:
        for path in sorted(stage.rglob("*")):
            if path.is_file():
                arc = path.relative_to(stage).as_posix()
                zf.write(path, arcname=arc)

    print(f"Wrote {out_zip}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
