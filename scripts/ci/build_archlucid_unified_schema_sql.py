"""Emit ArchLucid_Unified_Schema.sql as a DDL-only subset of Scripts/ArchLucid.sql."""

from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
MASTER = ROOT / "ArchLucid.Persistence" / "Scripts" / "ArchLucid.sql"
OUT = ROOT / "ArchLucid.Persistence" / "Scripts" / "ArchLucid_Unified_Schema.sql"

HEADER = """/*
  ArchLucid_Unified_Schema.sql

  REFERENCE AND IaC ALIGNMENT ONLY. This script is NOT executed by DbUp, SqlSchemaBootstrapper,
  or deployment pipelines unless you deliberately wire it yourself.

  PURPOSE
    Consolidated declarative DDL (CREATE TABLE, CREATE INDEX, ALTER TABLE batches only) reflecting
    the final schema shape after sequential application of forward DbUp migrations
    ArchLucid.Persistence/Migrations/001_*.sql … 143_*.sql (excluding Rollback/).

  HOW THIS ARTIFACT RELATES TO MIGRATIONS
    Forward migrations remain the authoritative upgrade path on existing databases.
    This file is mechanically derived from ArchLucid.Persistence/Scripts/ArchLucid.sql—the same master
    greenfield DDL that CI requires to co-change with forward migrations—and therefore matches the
    final desired relational object model those migrations converge on.

    Regenerate after ArchLucid.sql changes:
      python scripts/ci/build_archlucid_unified_schema_sql.py

  OMITTED BATCH TYPES (present in ArchLucid.sql but not IaC-declarative table/index/column DDL here)
    RLS EXEC blocks, DENY/GRANT, standalone stored procedures/functions, EXEC-only batches, SET
    without accompanying DDL where applicable.

  SET ANSI_NULLS ON;
  SET QUOTED_IDENTIFIER ON;
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

"""


def split_go_batches(sql: str) -> list[str]:
    return [b.strip() for b in re.split(r"(?m)^\s*GO\s*$", sql) if b.strip()]


def strip_sql_comments_for_scan(batch: str) -> str:
    """Remove /* */ and -- line comments so documentation cannot trigger DDL detection."""
    without_blocks = re.sub(r"/\*.*?\*/", "", batch, flags=re.S)
    return re.sub(r"--[^\n]*", "", without_blocks)


def batch_has_declarative_ddl(batch: str) -> bool:
    u = strip_sql_comments_for_scan(batch).upper()

    if "CREATE TABLE" in u:
        return True

    if re.search(r"\bCREATE\s+(?:UNIQUE\s+)?(?:CLUSTERED\s+|NONCLUSTERED\s+)?INDEX\b", u):
        return True

    if "ALTER TABLE" in u:
        return True

    return False


def main() -> None:
    master_text = MASTER.read_text(encoding="utf-8")

    batches = split_go_batches(master_text)

    kept = [b for b in batches if batch_has_declarative_ddl(b)]

    OUT.write_text(HEADER + "\n\nGO\n\n".join(kept) + "\n", encoding="utf-8")

    print(f"Wrote {OUT} ({len(kept)} GO batches)")


if __name__ == "__main__":
    main()
