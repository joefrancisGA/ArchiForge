#!/usr/bin/env python3
"""
Regression gate for committed agent-output score floors (Topology + Cost/Compliance/Critic).

1) Validates scripts/ci/prompt_regression_baseline.json shape.
2) Enforces non-placeholder Topology minimums so the file cannot silently revert to all zeros.
3) Enforces Cost/Compliance/Critic structural and semantic minimums (shared AgentResult JSON contract).

Merge-blocking structural/semantic checks run in ArchLucid.AgentRuntime.Tests.Evaluation.PromptRegressionBaselineContractTests
using the same JSON copied to test output.
"""

from __future__ import annotations

import json
import sys
from pathlib import Path


def _topology_mins(raw: dict) -> tuple[float, float]:
    struct = float(raw["minStructuralCompletenessByAgentType"]["Topology"])
    sem = float(raw["minSemanticScoreByAgentType"]["Topology"])
    return struct, sem


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

    topology_struct, topology_sem = _topology_mins(raw)
    if topology_struct < 0.9:
        print(
            "Invalid baseline: minStructuralCompletenessByAgentType.Topology must be >= 0.9 "
            "(merge-blocking contract tests depend on non-placeholder Topology floors).",
            file=sys.stderr,
        )
        return 2
    if topology_sem < 0.5:
        print(
            "Invalid baseline: minSemanticScoreByAgentType.Topology must be >= 0.5 "
            "(merge-blocking contract tests depend on non-placeholder Topology floors).",
            file=sys.stderr,
        )
        return 2

    for agent in ("Cost", "Compliance", "Critic"):
        struct = float(raw["minStructuralCompletenessByAgentType"][agent])
        sem = float(raw["minSemanticScoreByAgentType"][agent])
        if struct < 0.85:
            print(
                f"Invalid baseline: minStructuralCompletenessByAgentType.{agent} must be >= 0.85 "
                "(non-placeholder floors for merge-blocking contract tests).",
                file=sys.stderr,
            )
            return 2
        if sem < 0.7:
            print(
                f"Invalid baseline: minSemanticScoreByAgentType.{agent} must be >= 0.7 "
                "(non-placeholder floors for merge-blocking contract tests).",
                file=sys.stderr,
            )
            return 2

    print(
        "prompt_regression_baseline.json: OK (shape + Topology + Cost/Compliance/Critic floor policy). "
        "Scores are asserted in ArchLucid.AgentRuntime.Tests.Evaluation.PromptRegressionBaselineContractTests."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
