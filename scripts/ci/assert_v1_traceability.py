#!/usr/bin/env python3
"""Validate V1 traceability matrix filters discover at least one test (docs/V1_REQUIREMENTS_TEST_TRACEABILITY.md).

Run from repo root after a Release build:
  dotnet build ArchLucid.sln -c Release --nologo
  python scripts/ci/assert_v1_traceability.py

CI integration: optional follow-up job can invoke this script after the main .NET build.
"""

from __future__ import annotations

import re
import subprocess
import sys
from pathlib import Path


def _repo_root() -> Path:
    return Path(__file__).resolve().parents[2]


def _parse_traceability_filters(md_path: Path) -> list[tuple[str, str]]:
    """Return (v1_reference, filter_expression) from the main traceability matrix table."""
    text = md_path.read_text(encoding="utf-8")
    lines = text.splitlines()
    in_table = False
    rows: list[tuple[str, str]] = []

    for line in lines:
        if line.strip() == "## Traceability matrix":
            in_table = True
            continue

        if in_table and line.startswith("---") and "| V1 reference" not in line and rows:
            break

        if not in_table or not line.startswith("|"):
            continue

        if "**2." not in line and "**§" not in line:
            continue

        cells = [c.strip() for c in line.split("|")]

        if len(cells) < 6:
            continue

        ref = cells[1]
        filter_cell = cells[4]

        if "scripts" in filter_cell.lower() or "run `" in filter_cell.lower():
            continue

        if not filter_cell or "`" not in filter_cell:
            continue

        inner = filter_cell.replace("`", "").strip()

        if not inner:
            continue

        rows.append((ref, inner))

    return rows


def _ascii(text: str) -> str:
    return text.encode("ascii", errors="replace").decode("ascii")


def _list_test_count(root: Path, filter_expression: str) -> int:
    args = [
        "dotnet",
        "test",
        str(root / "ArchLucid.sln"),
        "-c",
        "Release",
        "--no-build",
        "--nologo",
        "--list-tests",
        "--filter",
        filter_expression,
    ]

    completed = subprocess.run(
        args,
        cwd=str(root),
        capture_output=True,
        text=True,
        check=False,
    )

    if completed.returncode != 0:
        print(completed.stdout, file=sys.stderr)
        print(completed.stderr, file=sys.stderr)
        raise RuntimeError(f"dotnet test --list-tests failed for filter: {filter_expression}")

    # Per-assembly output often includes "No test matches ..." for DLLs that do not
    # contain the filter; do not treat that as global zero — count matching FQN lines.
    output = completed.stdout + "\n" + completed.stderr

    count = 0

    for line in output.splitlines():
        stripped = line.strip()

        if stripped.startswith("ArchLucid.") and ".Tests." in stripped:
            count += 1

    return count


def main() -> int:
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except (AttributeError, OSError):
        pass

    root = _repo_root()
    md = root / "docs" / "V1_REQUIREMENTS_TEST_TRACEABILITY.md"

    if not md.is_file():
        print(f"ERROR: {md} not found", file=sys.stderr)
        return 1

    rows = _parse_traceability_filters(md)

    if not rows:
        print("ERROR: no traceability filter rows parsed", file=sys.stderr)
        return 1

    failures: list[str] = []

    print(f"Checking {len(rows)} V1 traceability filter row(s)...\n")

    for ref, combined in rows:
        branches = [b.strip() for b in re.split(r"\s+OR\s+", combined, flags=re.IGNORECASE)]
        best = 0
        best_branch = ""

        for branch in branches:
            if not branch:
                continue

            try:
                n = _list_test_count(root, branch)
            except RuntimeError as ex:
                failures.append(f"{ref}: {ex}")
                best = -1
                break

            if n > best:
                best = n
                best_branch = branch

        status = "OK" if best > 0 else "MISSING"

        safe_ref = _ascii(ref)
        safe_combined = _ascii(combined[:120])

        print(f"{status:8} {safe_ref:20} count={best:4}  filter={safe_combined}")

        if best <= 0:
            failures.append(f"{ref}: no tests discovered for any OR branch (tried: {branches})")

    if failures:
        print("\nTraceability validation failed:", file=sys.stderr)

        for line in failures:
            print(f"  - {_ascii(line)}", file=sys.stderr)

        return 1

    print("\nAll parsed V1 traceability filters discovered at least one test.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
