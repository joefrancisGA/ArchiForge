#!/usr/bin/env python3
"""
Placeholder regression gate for agent output quality metrics.

Extend this script to:
  1) Run dotnet test on ArchLucid.AgentRuntime.Tests with a fixed trait/filter, or
  2) Read a JSON metrics file produced by tests,

then compare against scripts/ci/prompt_regression_baseline.json.

Today: validates baseline JSON shape only (always exits 0) so CI can adopt the file without a merge-blocking hook.
"""

from __future__ import annotations

import json
import sys
from pathlib import Path


def main() -> int:
    root = Path(__file__).resolve().parents[2]
    baseline_path = root / "scripts" / "ci" / "prompt_regression_baseline.json"
    raw = json.loads(baseline_path.read_text(encoding="utf-8"))
    for key in ("minStructuralCompletenessByAgentType", "minSemanticScoreByAgentType"):
        block = raw.get(key)
        if not isinstance(block, dict):
            print(f"Invalid baseline: missing or non-object {key}", file=sys.stderr)
            return 2
        for agent in ("Topology", "Cost", "Compliance", "Critic"):
            if agent not in block:
                print(f"Invalid baseline: {key} missing {agent}", file=sys.stderr)
                return 2
            v = block[agent]
            if not isinstance(v, (int, float)):
                print(f"Invalid baseline: {key}.{agent} must be numeric", file=sys.stderr)
                return 2
    print("prompt_regression_baseline.json: OK (shape check only; no metrics asserted).")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
