#!/usr/bin/env python3
"""Fail when PMF evidence rows claim Captured status but Result is still TBD (fabrication guard)."""

from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path


def repo_root() -> Path:
    return Path(__file__).resolve().parents[2]


def strip_cell(raw: str) -> str:
    s = raw.strip()
    s = re.sub(r"^\*+|\*+$", "", s)
    return s.strip()


def main(argv: list[str]) -> int:
    root = repo_root()
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument(
        "--tracker",
        type=Path,
        default=root / "docs" / "go-to-market" / "PMF_VALIDATION_TRACKER.md",
        help="Path to PMF_VALIDATION_TRACKER.md",
    )
    ns = p.parse_args(argv)

    text = ns.tracker.resolve().read_text(encoding="utf-8")
    errors: list[str] = []

    for i, line in enumerate(text.splitlines(), start=1):
        s = line.strip()

        if not s.startswith("|"):
            continue

        if "Hypothesis" in s and "Pilot ID" in s:
            continue

        if re.match(r"^\|\s*[-:]+\s*\|", s):
            continue

        parts = [strip_cell(x) for x in s.split("|")]

        if len(parts) < 10:
            continue

        hypothesis = parts[1]

        if not re.match(r"^H\d+$", hypothesis):
            continue

        baseline = parts[6]
        result = parts[7]
        status = parts[8]

        if status.lower() != "captured":
            continue

        if result.upper() == "TBD":
            errors.append(
                f"{ns.tracker}:{i}: {hypothesis} Status=Captured but Result is still TBD "
                "(use a measured value, 'See scorecard', or 'Unknown' per §2.2)."
            )

        if baseline.upper() == "TBD" and result.upper() == "TBD":
            errors.append(
                f"{ns.tracker}:{i}: {hypothesis} Status=Captured but Baseline and Result are both TBD."
            )

    if errors:
        print("PMF tracker discipline violations:", file=sys.stderr)

        for e in errors:
            print(e, file=sys.stderr)

        return 1

    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
