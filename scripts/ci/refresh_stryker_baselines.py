#!/usr/bin/env python3
"""
Run Stryker.NET for each matrix target, read mutation-report.json, and rewrite
scripts/ci/stryker-baselines.json with floored one-decimal scores plus _measuredDate.

Requires: repo root, `dotnet tool restore`, and pinned `dotnet-stryker` (see .config/dotnet-tools.json).

Usage (from repository root):
  dotnet tool restore
  python3 scripts/ci/refresh_stryker_baselines.py

Subset + merge (keep other labels from the current baseline file):
  python3 scripts/ci/refresh_stryker_baselines.py --only Decisioning --merge-existing
"""
from __future__ import annotations

import argparse
import importlib.util
import json
import math
import shutil
import subprocess
import sys
from datetime import date
from pathlib import Path


STRYKER_TARGETS: list[tuple[str, str]] = [
    ("Persistence", "stryker-config.json"),
    ("Application", "stryker-config.application.json"),
    ("AgentRuntime", "stryker-config.agentruntime.json"),
    ("Coordinator", "stryker-config.coordinator.json"),
    ("Decisioning", "stryker-config.decisioning.json"),
]


def _load_assert_helpers():
    """Reuse score/report discovery from assert_stryker_score_vs_baseline.py (no edits to that file)."""
    path = Path(__file__).resolve().parent / "assert_stryker_score_vs_baseline.py"
    spec = importlib.util.spec_from_file_location("_stryker_assert_helpers", path)
    mod = importlib.util.module_from_spec(spec)
    assert spec.loader
    spec.loader.exec_module(mod)
    return mod


def _floor_one_decimal(score: float) -> float:
    return math.floor(score * 10 + 1e-9) / 10


def _run_stryker(repo_root: Path, config_file: str) -> None:
    cmd = ["dotnet", "dotnet-stryker", "-f", config_file, "-s", "ArchLucid.sln"]
    print(f"+ {' '.join(cmd)}", flush=True)
    subprocess.run(cmd, cwd=repo_root, check=True)


def _score_from_output(stryker_root: Path, helpers: object) -> float:
    report_path = helpers.find_mutation_report_json(stryker_root)
    if report_path is None:
        raise FileNotFoundError(f"No mutation-report.json under {stryker_root} (enable json reporter in stryker-config).")
    data = json.loads(report_path.read_text(encoding="utf-8"))
    if not isinstance(data, dict):
        raise ValueError("Mutation report JSON root must be an object.")
    score, detected, denom = helpers.mutation_score_from_report(data)
    print(
        f"  Raw score {score:.4f}% from {report_path}"
        + (f" (mutants detected={detected}, denom={denom})" if denom else ""),
        flush=True,
    )
    return score


def _main() -> int:
    parser = argparse.ArgumentParser(description="Refresh Stryker mutation score baselines from local runs.")
    parser.add_argument(
        "--repo-root",
        type=Path,
        default=Path.cwd(),
        help="Repository root (default: current directory).",
    )
    parser.add_argument(
        "--output",
        type=Path,
        default=Path("scripts/ci/stryker-baselines.json"),
        help="Baseline JSON path relative to repo root unless absolute.",
    )
    parser.add_argument(
        "--only",
        action="append",
        default=[],
        metavar="LABEL",
        help="Run only this matrix label (repeatable). Default: all five targets.",
    )
    parser.add_argument(
        "--merge-existing",
        action="store_true",
        help="With --only, copy mutationScore entries for labels not run from the existing baseline file.",
    )
    parser.add_argument(
        "--skip-restore",
        action="store_true",
        help="Skip `dotnet tool restore` (use when tools are already restored).",
    )
    parser.add_argument(
        "--skip-stryker",
        action="store_true",
        help="Do not run dotnet-stryker; read the newest mutation-report.json under StrykerOutput only (single-target debugging).",
    )
    args = parser.parse_args()

    repo_root = args.repo_root.resolve()
    output_path = args.output if args.output.is_absolute() else (repo_root / args.output)
    stryker_output = repo_root / "StrykerOutput"

    only_set = {x.strip() for x in args.only if x.strip()}
    if only_set:
        unknown = only_set - {label for label, _ in STRYKER_TARGETS}
        if unknown:
            print(f"Unknown --only label(s): {sorted(unknown)}", file=sys.stderr)
            return 2
        targets = [(label, cfg) for label, cfg in STRYKER_TARGETS if label in only_set]
    else:
        targets = list(STRYKER_TARGETS)

    if args.merge_existing and not only_set:
        print("--merge-existing requires at least one --only LABEL.", file=sys.stderr)
        return 2

    if only_set and not args.merge_existing:
        print(
            "Use --merge-existing when passing --only (CI baselines must list all five matrix labels).",
            file=sys.stderr,
        )
        return 2

    if not args.skip_restore:
        print("+ dotnet tool restore", flush=True)
        subprocess.run(["dotnet", "tool", "restore"], cwd=repo_root, check=True)

    helpers = _load_assert_helpers()
    measured: dict[str, dict[str, float]] = {}

    if args.merge_existing:
        if not output_path.is_file():
            print(f"--merge-existing requires existing baseline at {output_path}", file=sys.stderr)
            return 2
        prior = json.loads(output_path.read_text(encoding="utf-8"))
        for label, _ in STRYKER_TARGETS:
            entry = prior.get(label)
            if isinstance(entry, dict) and isinstance(entry.get("mutationScore"), (int, float)):
                measured[label] = {"mutationScore": float(entry["mutationScore"])}

    for label, config_file in targets:
        print(f"\n=== {label} ({config_file}) ===", flush=True)
        if not args.skip_stryker:
            shutil.rmtree(stryker_output, ignore_errors=True)
            _run_stryker(repo_root, config_file)
        score = _score_from_output(stryker_output, helpers)
        measured[label] = {"mutationScore": _floor_one_decimal(score)}

    missing = [label for label, _ in STRYKER_TARGETS if label not in measured]
    if missing:
        print(f"Missing scores for label(s): {missing}. Use full run or --merge-existing with a complete prior file.", file=sys.stderr)
        return 2

    out_doc: dict[str, object] = {"_measuredDate": date.today().isoformat()}
    for label, _ in STRYKER_TARGETS:
        out_doc[label] = measured[label]

    output_path.parent.mkdir(parents=True, exist_ok=True)
    text = json.dumps(out_doc, indent=2, sort_keys=False) + "\n"
    output_path.write_text(text, encoding="utf-8", newline="\n")
    print(f"\nWrote {output_path} ({out_doc['_measuredDate']}).", flush=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(_main())
