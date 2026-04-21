#!/usr/bin/env python3
"""
Plan which Stryker.NET configs to run on a pull request by diffing base...head.

Maps changed paths to the seven scheduled targets (Persistence, Application, AgentRuntime,
Coordinator, Decisioning, PersistenceCoordination, Api). Emits GitHub Actions outputs:
  run=true|false
  matrix_include=JSON array of {"label","config"} for strategy.matrix.include

See docs/MUTATION_TESTING_STRYKER.md § Per-PR differential.
"""
from __future__ import annotations

import argparse
import json
import os
import subprocess
import sys
from pathlib import Path

# (label, config) — order matches .github/workflows/stryker-scheduled.yml
FULL_MATRIX: list[tuple[str, str]] = [
    ("Persistence", "stryker-config.persistence.json"),
    ("Application", "stryker-config.application.json"),
    ("AgentRuntime", "stryker-config.agentruntime.json"),
    ("Coordinator", "stryker-config.coordinator.json"),
    ("Decisioning", "stryker-config.decisioning.json"),
    ("PersistenceCoordination", "stryker-config.persistence-coordination.json"),
    ("Api", "stryker-config.api.json"),
    ("DecisioningMerge", "stryker-config.decisioning-merge.json"),
    ("ApplicationGovernance", "stryker-config.application-governance.json"),
]

# Paths that should run the full matrix (config / CI / tool pins).
RUN_ALL_EXACT: tuple[str, ...] = (
    ".github/workflows/stryker-pr.yml",
    ".github/workflows/stryker-scheduled.yml",
    "scripts/ci/stryker-baselines.json",
    "scripts/ci/assert_stryker_score_vs_baseline.py",
    "scripts/ci/stryker_pr_plan.py",
    ".config/dotnet-tools.json",
)


def _normalize_path(p: str) -> str:
    return p.replace("\\", "/")


def _is_stryker_config_json(path: str) -> bool:
    n = _normalize_path(path)
    base = os.path.basename(n)
    return base.startswith("stryker-config") and base.endswith(".json")


def _git_diff_name_only(repo_root: Path, base: str, head: str) -> list[str]:
    r = subprocess.run(
        ["git", "diff", "--name-only", f"{base}...{head}"],
        cwd=repo_root,
        capture_output=True,
        text=True,
        check=False,
    )
    if r.returncode != 0:
        print(r.stderr, file=sys.stderr)
        raise RuntimeError(f"git diff failed with exit {r.returncode}")
    lines = [ln.strip() for ln in r.stdout.splitlines() if ln.strip()]
    return lines


def _should_run_full_matrix(path: str) -> bool:
    n = _normalize_path(path)
    if _is_stryker_config_json(n):
        return True
    if n in RUN_ALL_EXACT:
        return True
    return False


def _targets_for_path(path: str) -> list[tuple[str, str]]:
    """Return Stryker (label, config) tuples for one changed file path."""
    p = _normalize_path(path)

    if _should_run_full_matrix(p):
        return list(FULL_MATRIX)

    # Longest / most specific rules first.
    if p.startswith("ArchLucid.Persistence.Coordination/"):
        return [("PersistenceCoordination", "stryker-config.persistence-coordination.json")]

    if p.startswith("ArchLucid.Persistence.Tests/"):
        return [
            ("Persistence", "stryker-config.persistence.json"),
            ("PersistenceCoordination", "stryker-config.persistence-coordination.json"),
        ]

    excluded_under_persistence = (
        "ArchLucid.Persistence.Runtime/",
        "ArchLucid.Persistence.Integration/",
        "ArchLucid.Persistence.Advisory/",
        "ArchLucid.Persistence.Alerts/",
    )
    for ex in excluded_under_persistence:
        if p.startswith(ex):
            return []

    if p.startswith("ArchLucid.Persistence/"):
        return [("Persistence", "stryker-config.persistence.json")]

    if p.startswith("ArchLucid.Application.Tests/"):
        return [
            ("Application", "stryker-config.application.json"),
            ("ApplicationGovernance", "stryker-config.application-governance.json"),
        ]

    if p.startswith("ArchLucid.Application/Governance/"):
        return [("ApplicationGovernance", "stryker-config.application-governance.json")]

    if p.startswith("ArchLucid.Application/"):
        return [("Application", "stryker-config.application.json")]

    if p.startswith("ArchLucid.AgentRuntime.Tests/"):
        return [("AgentRuntime", "stryker-config.agentruntime.json")]

    if p.startswith("ArchLucid.AgentRuntime/"):
        return [("AgentRuntime", "stryker-config.agentruntime.json")]

    if p.startswith("ArchLucid.Coordinator.Tests/"):
        return [("Coordinator", "stryker-config.coordinator.json")]

    if p.startswith("ArchLucid.Coordinator/"):
        return [("Coordinator", "stryker-config.coordinator.json")]

    if p.startswith("ArchLucid.Decisioning.Tests/"):
        return [
            ("Decisioning", "stryker-config.decisioning.json"),
            ("DecisioningMerge", "stryker-config.decisioning-merge.json"),
        ]

    if p.startswith("ArchLucid.Decisioning/Merge/"):
        return [("DecisioningMerge", "stryker-config.decisioning-merge.json")]

    if p.startswith("ArchLucid.Decisioning/"):
        return [("Decisioning", "stryker-config.decisioning.json")]

    if p.startswith("ArchLucid.Api.Tests/"):
        return [("Api", "stryker-config.api.json")]

    if p.startswith("ArchLucid.Api/"):
        return [("Api", "stryker-config.api.json")]

    return []


def _dedupe_preserve_order(items: list[tuple[str, str]]) -> list[tuple[str, str]]:
    seen: set[str] = set()
    out: list[tuple[str, str]] = []
    for label, cfg in items:
        if label in seen:
            continue
        seen.add(label)
        out.append((label, cfg))
    return out


def plan_matrix(paths: list[str]) -> list[tuple[str, str]]:
    acc: list[tuple[str, str]] = []
    for path in paths:
        t = _targets_for_path(path)
        if len(t) == len(FULL_MATRIX):
            return list(FULL_MATRIX)
        acc.extend(t)
    return _dedupe_preserve_order(acc)


def _write_github_output(run: bool, include: list[dict[str, str]]) -> None:
    out = os.environ.get("GITHUB_OUTPUT")
    if not out:
        return
    payload = json.dumps(include, separators=(",", ":"))
    with open(out, "a", encoding="utf-8") as f:
        f.write(f"run={'true' if run else 'false'}\n")
        f.write(f"matrix_include={payload}\n")


def _self_test() -> None:
    assert plan_matrix([]) == []
    assert len(plan_matrix(["stryker-config.persistence.json"])) == len(FULL_MATRIX)
    assert plan_matrix(["ArchLucid.Decisioning/Foo.cs"]) == [
        ("Decisioning", "stryker-config.decisioning.json"),
    ]
    assert plan_matrix(["ArchLucid.Persistence.Tests/x.cs"]) == [
        ("Persistence", "stryker-config.persistence.json"),
        ("PersistenceCoordination", "stryker-config.persistence-coordination.json"),
    ]
    assert plan_matrix(["ArchLucid.Persistence.Runtime/x.cs"]) == []
    assert plan_matrix(["ArchLucid.Persistence/Sql/x.cs"]) == [
        ("Persistence", "stryker-config.persistence.json"),
    ]
    assert plan_matrix(["ArchLucid.Persistence.Coordination/x.cs"]) == [
        ("PersistenceCoordination", "stryker-config.persistence-coordination.json"),
    ]
    assert plan_matrix(["ArchLucid.Decisioning/Merge/x.cs"]) == [
        ("DecisioningMerge", "stryker-config.decisioning-merge.json"),
    ]
    assert plan_matrix(["ArchLucid.Application/Governance/x.cs"]) == [
        ("ApplicationGovernance", "stryker-config.application-governance.json"),
    ]


def _main() -> int:
    parser = argparse.ArgumentParser(description="Plan Stryker PR differential matrix from git diff.")
    parser.add_argument("--repo-root", type=Path, default=Path.cwd())
    parser.add_argument("--base", required=True, help="Base ref (e.g. merge-base parent SHA).")
    parser.add_argument("--head", required=True, help="Head ref (e.g. PR head SHA).")
    parser.add_argument("--dry-run", action="store_true", help="Print plan to stdout; do not write GITHUB_OUTPUT.")
    args = parser.parse_args()

    repo = args.repo_root.resolve()
    paths = _git_diff_name_only(repo, args.base, args.head)
    matrix_tuples = plan_matrix(paths)
    include = [{"label": a, "config": b} for a, b in matrix_tuples]
    run = len(include) > 0

    print(f"Changed files: {len(paths)}")
    if paths and len(paths) <= 40:
        for p in paths:
            print(f"  {p}")
    elif paths:
        for p in paths[:20]:
            print(f"  {p}")
        print(f"  ... and {len(paths) - 20} more")

    print(f"Stryker PR plan: run={run}, targets={len(include)}")
    for row in include:
        print(f"  - {row['label']}: {row['config']}")

    if args.dry_run:
        return 0

    _write_github_output(run, include)
    return 0


if __name__ == "__main__":
    if len(sys.argv) > 1 and sys.argv[1] == "--self-test":
        _self_test()
        print("stryker_pr_plan.py --self-test OK", flush=True)
        raise SystemExit(0)
    raise SystemExit(_main())
