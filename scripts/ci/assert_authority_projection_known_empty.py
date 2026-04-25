#!/usr/bin/env python3
"""ADR 0030 — validate docs/architecture/AUTHORITY_PROJECTION_KNOWN_EMPTY.json shape and lifecycle fields."""

from __future__ import annotations

import json
import sys
from pathlib import Path


def main() -> int:
    repo_root = Path(__file__).resolve().parents[2]
    path = repo_root / "docs" / "architecture" / "AUTHORITY_PROJECTION_KNOWN_EMPTY.json"
    if not path.is_file():
        print(f"ERROR: missing {path}", file=sys.stderr)
        return 2

    data = json.loads(path.read_text(encoding="utf-8"))
    if data.get("schemaVersion") != 1:
        print("ERROR: schemaVersion must be 1", file=sys.stderr)
        return 2

    fields = data.get("emptyFields")
    if not isinstance(fields, list):
        print("ERROR: emptyFields must be a JSON array", file=sys.stderr)
        return 2

    for i, row in enumerate(fields):
        if not isinstance(row, dict):
            print(f"ERROR: emptyFields[{i}] must be an object", file=sys.stderr)
            return 2

        for key in ("name", "rationale", "trackedBy"):
            if key not in row or not isinstance(row[key], str) or not row[key].strip():
                print(f"ERROR: emptyFields[{i}].{key} must be a non-empty string", file=sys.stderr)
                return 2

    names = {str(r["name"]) for r in fields}

    # ADR 0030 PR A3 (2026-04-24) — Relationships now round-trips through the Authority FK chain
    # via TopologySection.Relationships. The allow-list must NOT re-introduce it (drift guard).
    if "Relationships" in names:
        print(
            "ERROR: allow-list must NOT include Relationships — ADR 0030 PR A3 (2026-04-24) wired "
            "TopologySection.Relationships through the Authority FK chain. "
            "If you genuinely need to suppress Relationships projection again, ship an ADR "
            "amendment first.",
            file=sys.stderr,
        )
        return 2

    print(f"OK: authority projection known-empty allow-list ({len(names)} field(s)) at {path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
