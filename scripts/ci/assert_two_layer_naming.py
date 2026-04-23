#!/usr/bin/env python3
"""
Fail if buyer-facing docs under docs/ (excluding docs/archive and docs/adr) contain
legacy layer labels "Advanced Analysis" or "Enterprise Controls".

Canonical naming: Pilot + Operate (see docs/library/PRODUCT_PACKAGING.md).
Historical three-layer narrative: docs/archive/PRODUCT_PACKAGING_THREE_LAYERS_2026_04_23.md
"""

from __future__ import annotations

import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]
DOCS_ROOT = REPO_ROOT / "docs"
FORBIDDEN_SUBSTRINGS = ("Advanced Analysis", "Enterprise Controls")


def _iter_markdown_files() -> list[Path]:
    paths: list[Path] = []

    if not DOCS_ROOT.is_dir():
        return paths

    for path in DOCS_ROOT.rglob("*.md"):
        rel = path.relative_to(DOCS_ROOT)
        if rel.parts and rel.parts[0] == "archive":
            continue

        if "adr" in rel.parts:
            continue

        paths.append(path)

    return paths


def main() -> int:
    violations: list[tuple[Path, str]] = []

    for path in _iter_markdown_files():
        text = path.read_text(encoding="utf-8")

        for needle in FORBIDDEN_SUBSTRINGS:
            if needle in text:
                violations.append((path, needle))

    if not violations:
        return 0

    for path, needle in sorted(violations, key=lambda x: (str(x[0]), x[1])):
        print(f"Two-layer naming lint: forbidden substring {needle!r} in {path.relative_to(REPO_ROOT)}")

    return 1


if __name__ == "__main__":
    sys.exit(main())
