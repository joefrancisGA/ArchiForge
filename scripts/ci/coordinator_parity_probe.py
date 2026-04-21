#!/usr/bin/env python3
"""ADR 0021 gate (iv) helper: append coordinator vs authority durable audit counts into the parity runbook.

When ``AuditEventTypes.Run`` is absent from ``ArchLucid.Core/Audit/AuditEventTypes.cs``, exits 0 without
mutating the runbook (probe ships before Phase 2 catalog is everywhere).

Optional: set ``ARCHLUCID_COORDINATOR_PARITY_ODBC`` to an ODBC connection string; if ``pyodbc`` is importable,
queries ``dbo.AuditEvents`` for the last 24h. Otherwise counts are recorded as ``—`` (owner fills manually
or wires SQL later).
"""
from __future__ import annotations

import argparse
import datetime as dt
import sys
from pathlib import Path

MARKER_START = "<!-- coordinator-parity-probe:table -->"
MARKER_END = "<!-- /coordinator-parity-probe:table -->"

COORD_LEGACY_TYPES = (
    "CoordinatorRunCreated",
    "CoordinatorRunExecuteStarted",
    "CoordinatorRunExecuteSucceeded",
    "CoordinatorRunCommitCompleted",
    "CoordinatorRunFailed",
)

CANONICAL_RUN_TYPES = (
    "Run.Created",
    "Run.ExecuteStarted",
    "Run.ExecuteSucceeded",
    "Run.CommitCompleted",
    "Run.Failed",
)

AUTHORITY_TYPES = ("RunStarted", "RunCompleted")


def run_catalog_present(audit_types_path: Path) -> bool:
    text = audit_types_path.read_text(encoding="utf-8")
    return "public static class Run" in text and "Run.Created" in text


def fetch_counts_from_sql(_odbc: str) -> tuple[int | None, int | None, int | None]:
    try:
        import pyodbc  # type: ignore[import-not-found]
    except ImportError:
        return None, None, None

    try:
        conn = pyodbc.connect(_odbc, timeout=15)
    except Exception:
        return None, None, None

    try:
        cursor = conn.cursor()
        end = dt.datetime.now(dt.timezone.utc)
        start = end - dt.timedelta(hours=24)

        def _count_in(types: tuple[str, ...]) -> int | None:
            placeholders = ",".join("?" * len(types))
            cursor.execute(
                f"SELECT COUNT(*) FROM dbo.AuditEvents WHERE OccurredUtc >= ? AND OccurredUtc < ? AND EventType IN ({placeholders})",
                (start, end, *types),
            )
            row = cursor.fetchone()
            if row is None or row[0] is None:
                return None
            return int(row[0])

        return _count_in(COORD_LEGACY_TYPES), _count_in(CANONICAL_RUN_TYPES), _count_in(AUTHORITY_TYPES)
    finally:
        conn.close()


def format_markdown_row(
    window_start: str,
    window_end: str,
    coord: int | None,
    canonical: int | None,
    authority: int | None,
) -> str:
    def cell(v: int | None) -> str:
        if v is None:
            return "-"
        return str(v)

    return (
        f"| {window_start} | {window_end} | *(sample)* | - | - | "
        f"{cell(coord)} / {cell(canonical)} / {cell(authority)} | - | "
        f"auto `scripts/ci/coordinator_parity_probe.py` |"
    )


def upsert_table(runbook: Path, new_row: str, max_rows: int = 14) -> bool:
    text = runbook.read_text(encoding="utf-8")
    if MARKER_START not in text or MARKER_END not in text:
        print(f"error: runbook missing markers {MARKER_START!r} … {MARKER_END!r}", file=sys.stderr)
        return False

    before, rest = text.split(MARKER_START, 1)
    middle, after = rest.split(MARKER_END, 1)
    raw_lines = [ln.rstrip() for ln in middle.strip().splitlines() if ln.strip().startswith("|")]
    if len(raw_lines) < 2:
        print("error: probe table missing header and separator rows", file=sys.stderr)
        return False

    header_sep = raw_lines[:2]
    body_lines = raw_lines[2:]
    body_lines.append(new_row.strip())
    body_lines = [b for b in body_lines if b]
    body_lines = body_lines[-max_rows:]
    rebuilt = "\n".join(header_sep + body_lines) + "\n"
    new_text = before + MARKER_START + "\n" + rebuilt + MARKER_END + after
    if new_text == text:
        return False

    runbook.write_text(new_text, encoding="utf-8")
    return True


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--audit-types",
        type=Path,
        default=Path("ArchLucid.Core/Audit/AuditEventTypes.cs"),
        help="Path to AuditEventTypes.cs",
    )
    parser.add_argument(
        "--runbook",
        type=Path,
        default=Path("docs/runbooks/COORDINATOR_TO_AUTHORITY_PARITY.md"),
        help="Parity runbook to upsert",
    )
    parser.add_argument("--dry-run", action="store_true", help="Print row only; do not write runbook")
    args = parser.parse_args()

    if not run_catalog_present(args.audit_types):
        print("coordinator_parity_probe: AuditEventTypes.Run not present — no-op.")
        return 0

    odbc = __import__("os").environ.get("ARCHLUCID_COORDINATOR_PARITY_ODBC", "").strip()
    coord, canon, auth = fetch_counts_from_sql(odbc) if odbc else (None, None, None)

    end = dt.datetime.now(dt.timezone.utc)
    start = end - dt.timedelta(hours=24)
    row = format_markdown_row(
        start.strftime("%Y-%m-%d %H:%M UTC"),
        end.strftime("%Y-%m-%d %H:%M UTC"),
        coord,
        canon,
        auth,
    )
    print(row)

    if args.dry_run:
        return 0

    if not args.runbook.is_file():
        print(f"error: runbook not found: {args.runbook}", file=sys.stderr)
        return 2

    if upsert_table(args.runbook, row):
        print(f"updated {args.runbook}")
    else:
        print("runbook unchanged")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
