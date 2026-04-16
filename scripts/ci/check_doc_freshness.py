#!/usr/bin/env python3
"""
Warn when docs/runbooks/*.md lack a **Last reviewed:** date in the header (first 50 lines),
or when the date is older than STALE_DAYS (default 365). Exits 0 (informational only).
"""

from __future__ import annotations

import re
import sys
from datetime import date, datetime
from pathlib import Path

LAST_REVIEWED_RE = re.compile(
    r"^\s*\*\*Last reviewed:\*\*\s*(\d{4}-\d{2}-\d{2})\s*$",
    re.IGNORECASE | re.MULTILINE,
)

STALE_DAYS = int(__import__("os").environ.get("ARCHLUCID_DOC_STALE_DAYS", "365"))


def repo_root() -> Path:
    return Path(__file__).resolve().parents[2]


def main() -> int:
    runbooks = repo_root() / "docs" / "runbooks"

    if not runbooks.is_dir():
        print("check_doc_freshness: no docs/runbooks directory")
        return 0

    today = date.today()
    missing: list[str] = []
    stale: list[str] = []

    for md in sorted(runbooks.glob("*.md")):
        head = "\n".join(md.read_text(encoding="utf-8", errors="replace").splitlines()[:50])
        m = LAST_REVIEWED_RE.search(head)

        if not m:
            missing.append(md.name)
            continue

        reviewed = datetime.strptime(m.group(1), "%Y-%m-%d").date()
        age = (today - reviewed).days

        if age > STALE_DAYS:
            stale.append(f"{md.name} ({m.group(1)}, {age} days old)")

    if missing:
        print("WARN: runbooks missing **Last reviewed:** YYYY-MM-DD in first 50 lines:")
        print("  " + ", ".join(missing))

    if stale:
        print(f"WARN: runbooks older than {STALE_DAYS} days by **Last reviewed:**")
        print("  " + "; ".join(stale))

    if not missing and not stale:
        print("check_doc_freshness: OK (runbook headers present / not stale)")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
