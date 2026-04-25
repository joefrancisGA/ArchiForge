#!/usr/bin/env python3
"""Merge per-profile k6 summary fragments (from tests/load/core-pilot.js handleSummary) into one baseline file."""
from __future__ import annotations

import json
import sys
from datetime import datetime, timezone
from pathlib import Path


def _load(path: Path) -> object:
    return json.loads(path.read_text(encoding="utf-8"))


def main() -> int:
    if len(sys.argv) < 2:
        print("usage: merge_baseline.py OUT.json CORE.json [READ.json MIXED.json]", file=sys.stderr)
        return 1

    out = Path(sys.argv[1])
    frags: list[Path] = [Path(p) for p in sys.argv[2:]]
    if not frags:
        print("error: provide at least one fragment JSON", file=sys.stderr)
        return 1

    profile_names: list[str | None] = ["core", "read", "mixed"]
    merged: dict = {
        "schema": "archlucid.k6-baseline.v1",
        "capturedAtUtc": datetime.now(tz=timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z"),
        "profiles": {},
    }

    for i, fpath in enumerate(frags):
        label = profile_names[i] if i < len(profile_names) else f"profile_{i + 1}"
        merged["profiles"][label] = _load(fpath)
        b = merged["profiles"][label].get("baseUrl")
        if isinstance(b, str) and i == 0:
            merged["baseUrl"] = b
        c = merged["profiles"][label].get("compress")
        if c is not None and i == 0:
            merged["compress"] = c

    out.write_text(json.dumps(merged, indent=2) + "\n", encoding="utf-8")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
