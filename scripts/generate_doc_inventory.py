#!/usr/bin/env python3
"""Emit ``docs/library/DOC_INVENTORY_<date>.md`` — markdown table of every ``docs/**/*.md`` except ``docs/archive/``."""

from __future__ import annotations

import datetime as dt
import subprocess
import sys
from pathlib import Path


def repo_root() -> Path:
    return Path(__file__).resolve().parents[1]


def git_last_modified(repo: Path, rel: str) -> str:
    try:
        out = subprocess.check_output(
            ["git", "log", "-1", "--format=%cs", "--", rel],
            cwd=repo,
            stderr=subprocess.DEVNULL,
            text=True,
        ).strip()
    except (subprocess.CalledProcessError, FileNotFoundError, OSError):
        return "(unknown)"
    return out or "(unknown)"


def classify(rel_posix: str) -> tuple[str, str, str]:
    low = rel_posix.lower()
    if low.endswith("concepts.md") or "v1_requirements_test_traceability" in low:
        return "N", "Y", ""
    if "archlucid_rename_checklist" in low or "azure_marketplace_saas_offer" in low:
        return "N", "Y", ""
    if "coverage_gap_analysis" in low or "quality_assessment" in low or "cursor_prompts" in low:
        return "Y", "Y", ""
    buyer = "Y" if any(
        x in low
        for x in (
            "start_here",
            "executive_sponsor",
            "first_30",
            "core_pilot",
            "go-to-market/",
            "trust_center",
            "demo",
            "whitepapers/",
            "sponsor",
            "pilot_roi",
            "install_order",
            "changelog",
            "pending_questions",
            "troubleshooting",
            "onboarding/",
        )
    ) else "N"
    engineer = "Y" if any(
        x in low
        for x in (
            "adr/",
            "api_",
            "/build",
            "build.md",
            "sql_",
            "test_",
            "architecture",
            "persistence",
            "runbooks/",
            "terraform",
            "operator_quick",
            "library/",
            "evidence/",
            "contracts/",
            "security/",
            "deployment/",
        )
    ) else "N"
    redundant = ""
    return buyer, engineer, redundant


def main() -> int:
    root = repo_root()
    docs = root / "docs"
    today = dt.date.today().isoformat()
    library = docs / "library"
    library.mkdir(parents=True, exist_ok=True)
    out_path = library / f"DOC_INVENTORY_{today.replace('-', '_')}.md"
    rows: list[tuple[str, str, str, str, str, str]] = []
    for path in sorted(docs.rglob("*.md")):
        try:
            rel = path.relative_to(docs)
        except ValueError:
            continue
        if rel.parts and rel.parts[0] == "archive":
            continue
        rel_posix = rel.as_posix()
        buyer, eng, red = classify(rel_posix)
        git_d = git_last_modified(root, str(Path("docs") / rel))
        rows.append((rel_posix, git_d, buyer, eng, red, ""))

    lines: list[str] = [
        f"> **Scope:** Machine-generated inventory ({today}) — paths under ``docs/`` excluding ``docs/archive/``; used for doc-compression audits.",
        "",
        f"**Generated:** {today} via ``python scripts/generate_doc_inventory.py``.",
        "",
        "**Columns:** `path` · `last_modified` (git commit date touching file) · `buyer_facing` · `engineer_facing` · `redundant_with` (heuristic; empty when none).",
        "",
        "**Note:** Rows with **N/N** are candidates for archival when they are not CI-pinned — see ``docs/START_HERE.md`` for the active root layout policy.",
        "",
        "| path | last_modified | buyer_facing | engineer_facing | redundant_with |",
        "|------|---------------|--------------|-----------------|----------------|",
    ]
    for r in rows:
        p, d, b, e, red, _ = r
        red_e = red.replace("|", "\\|")
        lines.append(f"| `{p}` | {d} | {b} | {e} | {red_e} |")

    lines.append("")
    lines.append(f"**Row count:** {len(rows)}")
    out_path.write_text("\n".join(lines) + "\n", encoding="utf-8", newline="\n")
    print(f"Wrote {out_path.relative_to(root)} ({len(rows)} rows)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
