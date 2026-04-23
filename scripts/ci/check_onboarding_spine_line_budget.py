#!/usr/bin/env python3
"""
Merge-blocking guard: the five-document onboarding spine must stay trim.

Each spine markdown file must be <= ``--max-lines`` (default **600**) lines so the
Day-1 path cannot silently grow into another unbounded library.
"""

from __future__ import annotations

import argparse
import sys
from pathlib import Path


SPINE_FILES: tuple[str, ...] = (
    "docs/engineering/INSTALL_ORDER.md",
    "docs/engineering/FIRST_30_MINUTES.md",
    "docs/CORE_PILOT.md",
    "docs/ARCHITECTURE_ON_ONE_PAGE.md",
    "docs/PENDING_QUESTIONS.md",
)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--max-lines",
        type=int,
        default=600,
        help="Maximum allowed lines per spine file (inclusive).",
    )
    parser.add_argument(
        "--repo-root",
        type=Path,
        default=Path(__file__).resolve().parents[2],
        help="Repository root (defaults to two parents above this script).",
    )
    args = parser.parse_args()
    root: Path = args.repo_root.resolve()
    max_lines: int = args.max_lines
    failures: list[str] = []

    for rel in SPINE_FILES:
        path = root / rel

        if not path.is_file():
            failures.append(f"{rel}: missing file")
            continue

        text = path.read_text(encoding="utf-8", errors="replace")
        line_count = 0 if len(text) == 0 else text.count("\n") + 1

        if line_count > max_lines:
            failures.append(f"{rel}: {line_count} lines exceeds spine budget {max_lines}")

    if failures:
        print("check_onboarding_spine_line_budget: FAILED", file=sys.stderr)

        for line in failures:
            print(line, file=sys.stderr)

        return 1

    print(f"check_onboarding_spine_line_budget: OK (max {max_lines} lines × {len(SPINE_FILES)} spine files)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
