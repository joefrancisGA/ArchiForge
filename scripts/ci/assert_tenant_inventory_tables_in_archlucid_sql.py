#!/usr/bin/env python3
"""Ensure dbo.* tables listed in TENANT_SCOPED_TABLES_INVENTORY.md appear in ArchLucid.sql."""

from __future__ import annotations

import re
import sys
from pathlib import Path


def _repo_root() -> Path:
    return Path(__file__).resolve().parents[2]


def _inventory_tables(inv_text: str) -> list[str]:
    """Parse `| `dbo.TableName` |` rows from the component breakdown table."""
    tables: list[str] = []

    for line in inv_text.splitlines():
        m = re.match(r"^\|\s*`(dbo\.\w+)`\s*\|", line.strip())

        if m:
            name = m.group(1).replace("dbo.", "", 1)
            tables.append(name)

    return tables


def _ddl_defines_table(ddl: str, table: str) -> bool:
    """Match CREATE TABLE [dbo].[Table] or dbo.Table in DDL."""
    escaped = re.escape(table)

    if re.search(rf"CREATE\s+TABLE\s+\[?dbo\]?\.\[{escaped}\]", ddl, re.IGNORECASE):
        return True

    if re.search(rf"CREATE\s+TABLE\s+dbo\.{escaped}\b", ddl, re.IGNORECASE):
        return True

    return False


def main() -> int:
    root = _repo_root()
    inv_path = root / "docs" / "TENANT_SCOPED_TABLES_INVENTORY.md"
    ddl_path = root / "ArchLucid.Persistence" / "Scripts" / "ArchLucid.sql"

    if not inv_path.is_file():
        print(f"Missing {inv_path}", file=sys.stderr)
        return 2

    if not ddl_path.is_file():
        print(f"Missing {ddl_path}", file=sys.stderr)
        return 2

    inv_text = inv_path.read_text(encoding="utf-8")
    ddl_text = ddl_path.read_text(encoding="utf-8")
    tables = _inventory_tables(inv_text)

    if not tables:
        print("No dbo.* rows found in tenant inventory (unexpected).", file=sys.stderr)
        return 2

    missing = [t for t in tables if not _ddl_defines_table(ddl_text, t)]

    if missing:
        print(
            "Tenant inventory / ArchLucid.sql mismatch: tables documented in "
            f"{inv_path.relative_to(root)} but no CREATE TABLE in master DDL: "
            + ", ".join(missing),
            file=sys.stderr,
        )
        return 1

    print(f"OK: {len(tables)} inventory table(s) present in ArchLucid.sql.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
