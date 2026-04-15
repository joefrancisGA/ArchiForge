#!/usr/bin/env python3
"""Ensure FullyQualifiedName~ tokens in docs/V1_REQUIREMENTS_TEST_TRACEABILITY.md resolve to test source files."""

from __future__ import annotations

import re
import sys
from pathlib import Path


def _repo_root() -> Path:
    return Path(__file__).resolve().parents[2]


def _parse_filter_column_rows(md: str) -> list[tuple[str, str]]:
    """Return (v1_ref, filter_cell) for matrix rows that have a filter column."""
    lines = md.splitlines()
    rows: list[tuple[str, str]] = []
    in_table = False

    for line in lines:
        stripped = line.strip()

        if stripped.startswith("| V1 reference"):
            in_table = True
            continue

        if not in_table:
            continue

        if not stripped.startswith("|"):
            break

        if re.match(r"^\|\s*-+", stripped):
            continue

        parts = [p.strip() for p in stripped.strip("|").split("|")]
        if len(parts) < 4:
            continue

        v1_ref = parts[0]
        filter_cell = parts[3]

        if v1_ref.startswith("**") and filter_cell:
            rows.append((v1_ref, filter_cell))

    return rows


def _fqdn_suffixes(filter_cell: str) -> list[str]:
    """Extract suffixes after FullyQualifiedName~ (split on OR)."""
    out: list[str] = []
    for segment in re.split(r"\s+OR\s+", filter_cell, flags=re.IGNORECASE):
        segment = segment.strip()
        m = re.search(r"FullyQualifiedName~(\w+)", segment)
        if m:
            out.append(m.group(1))
    return out


def _test_roots(root: Path) -> list[Path]:
    return sorted(root.glob("ArchLucid.*.Tests"))


def _suffix_resolves(root: Path, suffix: str) -> bool:
    if not suffix:
        return False

    boundary = re.compile(rf"\b{re.escape(suffix)}\b")

    for tests_dir in _test_roots(root):
        if not tests_dir.is_dir():
            continue

        for path in tests_dir.rglob("*.cs"):
            try:
                text = path.read_text(encoding="utf-8")
            except OSError:
                continue

            if boundary.search(path.stem):
                return True

            if suffix in path.stem and path.name.endswith("Tests.cs"):
                return True

            if boundary.search(text) and path.name.endswith("Tests.cs"):
                return True

            if re.search(rf"\bclass\s+{re.escape(suffix)}\b", text):
                return True

    ui_root = root / "archlucid-ui"
    if ui_root.is_dir():
        for pattern in ("*.test.ts", "*.test.tsx", "*.spec.ts", "*.spec.tsx"):
            for path in ui_root.rglob(pattern):
                try:
                    text = path.read_text(encoding="utf-8")
                except OSError:
                    continue

                if boundary.search(text) or boundary.search(path.stem):
                    return True

    return False


def main() -> int:
    root = _repo_root()
    doc_path = root / "docs" / "V1_REQUIREMENTS_TEST_TRACEABILITY.md"

    if not doc_path.is_file():
        print(f"::error::Missing {doc_path}")
        return 1

    text = doc_path.read_text(encoding="utf-8")
    rows = _parse_filter_column_rows(text)

    if not rows:
        print("::error::No V1 traceability filter rows parsed.")
        return 1

    failures: list[str] = []

    for v1_ref, filter_cell in rows:
        if "scripts" in filter_cell.lower() and "fullyqualifiedname" not in filter_cell.lower():
            continue

        if filter_cell.strip().lower().startswith("*(scripts"):
            continue

        suffixes = _fqdn_suffixes(filter_cell)

        if not suffixes:
            continue

        for suf in suffixes:
            if not _suffix_resolves(root, suf):
                failures.append(f"{v1_ref}: no test source match for FullyQualifiedName~{suf}")

    if failures:
        for f in failures:
            print(f"::error::{f}")
        print(f"::error::RTM filter resolve failed ({len(failures)} issue(s)).")
        return 1

    print(f"RTM filter resolve OK: checked {len(rows)} row(s).")
    return 0


if __name__ == "__main__":
    sys.exit(main())
