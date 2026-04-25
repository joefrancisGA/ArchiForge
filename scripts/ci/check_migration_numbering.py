r"""
check_migration_numbering.py
-----------------------------
Validates the DbUp migration filename invariants for ArchLucid.Persistence/Migrations/.

Rules enforced (each violation is fatal, exit code 1):

  R1  Every file matches the pattern  ^\d{3}_[A-Za-z0-9_]+\.sql$
  R2  No two files share the same 3-digit numeric prefix.
  R3  No gap larger than MAX_GAP between consecutive prefixes
      (gaps are allowed — DbUp orders lexically — but a giant gap usually
       means a renumbered or missing file and is worth a human glance).

Why this script exists:
  DbUp orders migrations by **embedded resource name** (lexical). Two files
  numbered 096_Foo.sql and 096_Bar.sql will both run, but the order between
  them is implementation-defined and changes silently when a file is renamed.
  This script makes that hazard merge-blocking.

Run:  python scripts/ci/check_migration_numbering.py
Exit: 0 = clean. 1 = at least one violation.

Companion to:
  - scripts/ci/check_pricing_single_source.py
  - scripts/ci/check_doc_links.py
"""

from __future__ import annotations

import os
import re
import sys
from collections import defaultdict


REPO_ROOT = os.path.normpath(os.path.join(os.path.dirname(__file__), "..", ".."))
MIGRATIONS_DIR = os.path.join(REPO_ROOT, "ArchLucid.Persistence", "Migrations")

FILENAME_PATTERN = re.compile(r"^(\d{3})_[A-Za-z0-9_]+\.sql$")
MAX_GAP = 3  # Tolerate small gaps from removed/renumbered files; flag big ones.


def main() -> int:
    if not os.path.isdir(MIGRATIONS_DIR):
        print(f"[FAIL] Migrations directory not found: {MIGRATIONS_DIR}", file=sys.stderr)
        return 1

    filenames = sorted(
        f for f in os.listdir(MIGRATIONS_DIR)
        if os.path.isfile(os.path.join(MIGRATIONS_DIR, f)) and f.endswith(".sql")
    )

    if not filenames:
        print(f"[FAIL] No .sql files in {MIGRATIONS_DIR}", file=sys.stderr)
        return 1

    violations: list[str] = []

    # R1: pattern check
    bad_pattern = [f for f in filenames if not FILENAME_PATTERN.match(f)]
    for f in bad_pattern:
        violations.append(f"R1 (pattern): '{f}' does not match '###_Description.sql'")

    # R2: duplicate-prefix check
    by_prefix: dict[str, list[str]] = defaultdict(list)
    for f in filenames:
        m = FILENAME_PATTERN.match(f)
        if m:
            by_prefix[m.group(1)].append(f)
    for prefix, group in by_prefix.items():
        if len(group) > 1:
            violations.append(
                f"R2 (duplicate prefix '{prefix}'): {sorted(group)}. "
                "DbUp orders lexically; rename one of these to the next free 3-digit prefix."
            )

    # R3: oversized-gap check (informational unless flagged as violation by length)
    sorted_prefixes = sorted(int(p) for p in by_prefix.keys())
    big_gaps: list[tuple[int, int]] = []
    for prev, curr in zip(sorted_prefixes, sorted_prefixes[1:]):
        gap = curr - prev
        if gap > MAX_GAP:
            big_gaps.append((prev, curr))
    for prev, curr in big_gaps:
        violations.append(
            f"R3 (large gap): {prev:03d} -> {curr:03d} (gap of {curr - prev}). "
            "If this is intentional (renumbered or removed migrations), document in docs/SQL_SCRIPTS.md."
        )

    if violations:
        print(f"[FAIL] {len(violations)} migration-numbering violation(s):", file=sys.stderr)
        for v in violations:
            print(f"  - {v}", file=sys.stderr)
        print("", file=sys.stderr)
        print("Hint: see docs/SQL_DDL_DISCIPLINE.md for the migration-naming convention.", file=sys.stderr)
        return 1

    print(
        f"[OK] {len(filenames)} migration files; "
        f"{len(by_prefix)} unique prefixes; no duplicates; max gap <= {MAX_GAP}."
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
