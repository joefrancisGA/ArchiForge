#!/usr/bin/env python3
"""
Lightweight guard for committed golden agent-result fixtures used by reference-case evaluation.

Reads scripts/ci/agent-reference-baselines.json (optional). When present, verifies each listed
fixture path exists and parses as JSON with a top-level object. Intended for CI steps that
want a merge gate without invoking the full API reference evaluator.

Usage:
  python3 scripts/ci/assert_agent_reference_baselines.py
"""

from __future__ import annotations

import json
import sys
from pathlib import Path


def _repo_root() -> Path:
    return Path(__file__).resolve().parents[2]


def main() -> int:
    root = _repo_root()
    baseline_path = root / "scripts" / "ci" / "agent-reference-baselines.json"

    if not baseline_path.is_file():
        return 0

    raw = json.loads(baseline_path.read_text(encoding="utf-8"))
    fixtures = raw.get("goldenAgentResultFixtures")

    if not isinstance(fixtures, list) or not fixtures:
        print("agent-reference-baselines.json: goldenAgentResultFixtures missing or empty — skipping.", file=sys.stderr)
        return 0

    errors: list[str] = []

    for entry in fixtures:
        if not isinstance(entry, str) or not entry.strip():
            errors.append(f"Invalid fixture entry: {entry!r}")
            continue

        path = (root / entry).resolve()
        if not path.is_file():
            errors.append(f"Missing fixture file: {entry}")
            continue

        try:
            doc = json.loads(path.read_text(encoding="utf-8"))
        except json.JSONDecodeError as ex:
            errors.append(f"Invalid JSON {entry}: {ex}")
            continue

        if not isinstance(doc, dict):
            errors.append(f"Fixture root must be a JSON object: {entry}")

    if errors:
        print("assert_agent_reference_baselines.py failures:", file=sys.stderr)
        for line in errors:
            print(f"  - {line}", file=sys.stderr)
        return 1

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
