#!/usr/bin/env python3
"""
Assemble dist/procurement-pack/ + dist/procurement-pack.zip for buyer / procurement teams.

Canonical file list: scripts/procurement_pack_canonical.json (shared with CI:
scripts/ci/assert_procurement_pack_buildable.py).

Emits:
  - manifest.json — each packed file: pack_path, source_repo_path, bytes, sha256
  - versions.txt — git commit, build UTC, ArchLucid CLI version (from ArchLucid.Cli.csproj)
  - redaction_report.md — files intentionally not included and why
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
    args = parser.parse_args()

    root = repo_root()
    entries, excluded = load_canonical(root)

    missing = validate_sources(root, entries)
    if missing:
        print("error: canonical procurement pack sources missing:", file=sys.stderr)
        for m in missing:
            print(f"  - {m}", file=sys.stderr)
        return 1

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

    manifest_rows = build_manifest_rows(stage, entries)
    (stage / "manifest.json").write_text(
        json.dumps({"generated_utc": datetime.now(timezone.utc).isoformat(), "files": manifest_rows}, indent=2)
        + "\n",
        encoding="utf-8",
    )
    write_versions_txt(stage, root)
    write_redaction_report(stage, excluded)

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
