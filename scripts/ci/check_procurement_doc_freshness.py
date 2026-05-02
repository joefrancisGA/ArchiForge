#!/usr/bin/env python3
"""Warn when key procurement docs are stale based on Last reviewed date."""

from __future__ import annotations

import re
import sys
from datetime import datetime, timezone
from pathlib import Path


DATE_RE = re.compile(r"\*?\*?Last reviewed:\*?\*?\s*(\d{4}-\d{2}-\d{2})", re.IGNORECASE)


def repo_root() -> Path:
    return Path(__file__).resolve().parents[2]


def parse_date(text: str) -> datetime.date | None:
    match = DATE_RE.search(text)
    if match is None:
        return None
    try:
        return datetime.strptime(match.group(1), "%Y-%m-%d").replace(tzinfo=timezone.utc).date()
    except ValueError:
        return None


def main() -> int:
    root = repo_root()
    today = datetime.now(timezone.utc).date()
    targets = [
        (root / "docs" / "go-to-market" / "TRUST_CENTER.md", 45),
        (root / "docs" / "go-to-market" / "SUBPROCESSORS.md", 90),
        (root / "docs" / "go-to-market" / "SLA_SUMMARY.md", 45),
        (root / "docs" / "go-to-market" / "INCIDENT_COMMUNICATIONS_POLICY.md", 45),
        (root / "docs" / "go-to-market" / "CURRENT_ASSURANCE_POSTURE.md", 45),
    ]

    stale = False
    for path, max_age in targets:
        rel = path.relative_to(root).as_posix()
        if not path.is_file():
            print(f"STALE_DOC missing {rel}", file=sys.stderr)
            stale = True
            continue

        parsed = parse_date(path.read_text(encoding="utf-8", errors="replace"))
        if parsed is None:
            print(f"STALE_DOC {rel}: missing or malformed Last reviewed date", file=sys.stderr)
            stale = True
            continue

        age = (today - parsed).days
        if age > max_age:
            print(f"STALE_DOC {rel}: {age} days old (max {max_age})", file=sys.stderr)
            stale = True

    if stale:
        return 1

    print("Procurement doc freshness OK")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

