#!/usr/bin/env python3
"""Validate docs/V1_REQUIREMENTS_TEST_TRACEABILITY.md maps each V1 row to existing test evidence paths."""

from __future__ import annotations

import re
import sys
from pathlib import Path


def _repo_root() -> Path:
    return Path(__file__).resolve().parents[2]


def _parse_matrix_rows(md: str) -> list[tuple[str, str]]:
    """Return (v1_ref, tests_cell) for data rows under the traceability matrix header."""
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
        if len(parts) < 3:
            continue

        v1_ref = parts[0]
        tests_cell = parts[2]

        if v1_ref.startswith("**") and tests_cell:
            rows.append((v1_ref, tests_cell))

    return rows


def _tokens_from_tests_cell(cell: str) -> list[str]:
    """Extract backtick-enclosed tokens (project or file paths)."""
    return re.findall(r"`([^`]+)`", cell)


def _is_evidence_token(token: str) -> bool:
    """Only filesystem-backed references are validated (skip type names like JobsController)."""
    t = token.strip()

    if not t or " " in t:
        return False

    if t.startswith("ArchLucid.") or t.startswith("archlucid-ui"):
        return True

    if t.startswith(".github/") or t.startswith("scripts/") or t.startswith("infra/"):
        return True

    if re.search(r"\.(ps1|cmd|sh|yml|yaml|md)$", t, re.IGNORECASE):
        return True

    return False


def _token_exists(root: Path, token: str) -> bool:
    t = token.strip()
    if not t:
        return False

    normalized = t.replace("\\", "/").strip()
    if normalized.startswith("./"):
        normalized = normalized[2:]

    candidate = root / normalized
    if candidate.exists():
        return True

    if (root / f"{normalized}.csproj").exists():
        return True

    # Markdown lives under docs/
    if normalized.endswith(".md") and (root / "docs" / Path(normalized).name).exists():
        return True

    if normalized.endswith(".md") and (root / "docs" / normalized).exists():
        return True

    return False


def main() -> int:
    root = _repo_root()
    doc_path = root / "docs" / "V1_REQUIREMENTS_TEST_TRACEABILITY.md"

    if not doc_path.is_file():
        print(f"::error::Missing {doc_path}")
        return 1

    text = doc_path.read_text(encoding="utf-8")
    rows = _parse_matrix_rows(text)

    if not rows:
        print("::error::No V1 traceability rows parsed from markdown.")
        return 1

    failures: list[str] = []

    for v1_ref, tests_cell in rows:
        tokens = _tokens_from_tests_cell(tests_cell)

        if not tokens:
            failures.append(f"{v1_ref}: no backtick test references in matrix cell.")
            continue

        evidence_tokens = [tok for tok in tokens if _is_evidence_token(tok)]

        if not evidence_tokens:
            failures.append(f"{v1_ref}: no verifiable evidence tokens (projects/workflows/scripts) in cell.")
            continue

        missing = [tok for tok in evidence_tokens if not _token_exists(root, tok)]

        if missing:
            failures.append(f"{v1_ref}: missing path(s): {', '.join(missing)}")

    if failures:
        for f in failures:
            print(f"::warning::{f}")
        print(f"::error::Traceability check failed ({len(failures)} issue(s)).")
        return 1

    print(f"Traceability OK: {len(rows)} V1 scope row(s) have resolvable test references.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
