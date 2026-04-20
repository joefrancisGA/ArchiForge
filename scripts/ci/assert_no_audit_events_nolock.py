#!/usr/bin/env python3
"""Guard: dbo.AuditEvents reads must not use WITH (NOLOCK); use RCSI + default read committed instead."""

from __future__ import annotations

import re
import sys
from pathlib import Path


def main() -> int:
    root = Path(__file__).resolve().parents[2]
    target = root / "ArchLucid.Persistence" / "Audit" / "DapperAuditRepository.cs"

    if not target.is_file():
        print(f"ERROR: expected {target}", file=sys.stderr)
        return 1

    text = target.read_text(encoding="utf-8")

    if re.search(r"AuditEvents\s+WITH\s*\(\s*NOLOCK\s*\)", text, flags=re.IGNORECASE):
        print(
            "ERROR: dbo.AuditEvents must not use WITH (NOLOCK) in DapperAuditRepository "
            "(enable READ_COMMITTED_SNAPSHOT via migration 091).",
            file=sys.stderr,
        )
        return 1

    print("OK: no NOLOCK hint on dbo.AuditEvents in DapperAuditRepository")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
