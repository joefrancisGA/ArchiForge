#!/usr/bin/env python3
"""Hygiene checks for docs/security/pen-test-summaries/REMEDIATION_TRACKER.md.

- Emits a warning (stderr) for Open rows whose Target date is in the past.
- Exits 1 when --strict-critical and any Critical+Open row has Target date >30 days ago.
"""

from __future__ import annotations

import argparse
import re
import sys
from datetime import UTC, datetime, timedelta
from pathlib import Path


def repo_root() -> Path:
    return Path(__file__).resolve().parents[2]


def _parse_table_rows(text: str) -> list[dict[str, str]]:
    rows: list[dict[str, str]] = []
    in_table = False
    header_seen = False

    for line in text.splitlines():
        line_stripped = line.strip()

        if not line_stripped.startswith("|"):
            continue

        if "Finding ID" in line_stripped and "Severity" in line_stripped:
            in_table = True
            header_seen = True
            continue

        if not in_table or not header_seen:
            continue

        if re.match(r"^\|\s*-+", line_stripped):
            continue

        cells = [c.strip() for c in line_stripped.strip("|").split("|")]

        if len(cells) < 8:
            continue

        row = {
            "finding_id": cells[0],
            "severity": cells[1],
            "title": cells[2],
            "status": cells[3],
            "owner": cells[4],
            "target_date": cells[5],
            "verification": cells[6],
            "notes": cells[7],
        }

        if row["finding_id"].lower().startswith("(template)") or row["finding_id"].startswith("*"):
            continue

        rows.append(row)

    return rows


def _parse_target_date(value: str) -> datetime | None:
    value = value.strip()

    if not value or value == "—":
        return None

    try:
        return datetime.strptime(value[:10], "%Y-%m-%d").replace(tzinfo=UTC)
    except ValueError:
        return None


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--path",
        type=Path,
        default=repo_root() / "docs" / "security" / "pen-test-summaries" / "REMEDIATION_TRACKER.md",
        help="Path to REMEDIATION_TRACKER.md.",
    )
    parser.add_argument(
        "--strict-critical",
        action="store_true",
        help="Fail if Critical+Open has target date older than 30 days.",
    )
    args = parser.parse_args(argv)
    path: Path = args.path

    if not path.is_file():
        print(f"assert_pen_test_remediation_no_stale: missing {path}", file=sys.stderr)
        return 1

    text = path.read_text(encoding="utf-8", errors="replace")
    rows = _parse_table_rows(text)
    now = datetime.now(tz=UTC)
    exit_code = 0

    for row in rows:
        status = row["status"].strip()

        if status.lower() != "open":
            continue

        target = _parse_target_date(row["target_date"])

        if target is None:
            continue

        if target.date() < now.date():
            print(
                f"assert_pen_test_remediation_no_stale: WARNING — Open finding {row['finding_id']!r} "
                f"target date {target.date()} is in the past.",
                file=sys.stderr,
            )

        severity = row["severity"].strip().lower()

        if args.strict_critical and severity == "critical" and now - target > timedelta(days=30):
            print(
                f"assert_pen_test_remediation_no_stale: FAILED — Critical Open finding {row['finding_id']!r} "
                f"older than 30 days (target {target.date()}).",
                file=sys.stderr,
            )
            exit_code = 1

    print(f"assert_pen_test_remediation_no_stale: OK ({len(rows)} data row(s) parsed).")

    return exit_code


if __name__ == "__main__":
    raise SystemExit(main())
