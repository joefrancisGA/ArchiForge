#!/usr/bin/env python3
"""Ensure the N most recent forward DbUp migrations have a matching Rollback/RNNN_*.sql script."""

from __future__ import annotations

import re
import sys
from pathlib import Path


def _repo_root() -> Path:
    return Path(__file__).resolve().parents[2]


def _migration_numbers_and_names(migrations_dir: Path) -> list[tuple[int, str]]:
    pattern = re.compile(r"^(\d{3})_.+\.sql$")
    result: list[tuple[int, str]] = []

    for path in migrations_dir.iterdir():
        if not path.is_file():
            continue

        match = pattern.match(path.name)

        if match is None:
            continue

        result.append((int(match.group(1)), path.name))

    return result


def main() -> int:
    root = _repo_root()
    migrations_dir = root / "ArchLucid.Persistence" / "Migrations"
    rollback_dir = migrations_dir / "Rollback"

    if not migrations_dir.is_dir():
        print(f"ERROR: migrations directory not found: {migrations_dir}", file=sys.stderr)
        return 1

    if not rollback_dir.is_dir():
        print(f"ERROR: Rollback directory not found: {rollback_dir}", file=sys.stderr)
        return 1

    migrations = _migration_numbers_and_names(migrations_dir)

    if len(migrations) < 5:
        print(f"ERROR: expected at least 5 numbered migrations, found {len(migrations)}", file=sys.stderr)
        return 1

    migrations.sort(key=lambda t: t[0], reverse=True)
    top = migrations[:5]
    errors: list[str] = []

    for num, forward_name in top:
        prefix = f"R{num:03d}_"
        matches = sorted(rollback_dir.glob(f"{prefix}*.sql"))

        if len(matches) == 0:
            errors.append(f"Missing rollback for {forward_name}: no {rollback_dir.name}/{prefix}*.sql")

    if errors:
        print("Rollback script guard failed:", file=sys.stderr)

        for line in errors:
            print(f"  - {line}", file=sys.stderr)

        return 1

    print(f"Rollback scripts OK for latest migrations: {', '.join(n for _, n in top)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
