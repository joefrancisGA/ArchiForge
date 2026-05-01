#!/usr/bin/env python3
"""TB-003: enforce p95 ceilings for allowlisted named queries (CI / local gate).

Expected measurements JSON (default):
{
  "queries": [
    {"name": "AppendAuditEvent", "p95Ms": 12.3},
    ...
  ]
}

Non-allowlisted query names in measurements are ignored but counted in the footer ratio.
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path


def repo_root() -> Path:
    return Path(__file__).resolve().parents[2]


def load_allowlist(path: Path) -> dict[str, float]:
    rows = json.loads(path.read_text(encoding="utf-8"))
    return {r["name"]: float(r["p95ThresholdMs"]) for r in rows}


def load_measurements(path: Path) -> dict[str, float]:
    data = json.loads(path.read_text(encoding="utf-8"))
    queries = data.get("queries")
    if queries is None:
        raise ValueError("measurements JSON missing top-level 'queries' array")

    return {q["name"]: float(q["p95Ms"]) for q in queries}


def synth_measurements_under(allowlist: dict[str, float]) -> dict[str, float]:
    return {name: max(0.0, threshold * 0.5) for name, threshold in allowlist.items()}


def main(argv: list[str]) -> int:
    root = repo_root()
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument(
        "--allowlist",
        type=Path,
        default=root / "tests" / "performance" / "query-allowlist.json",
        help="TB-003 allowlist JSON path",
    )
    p.add_argument(
        "--measurements-json",
        type=Path,
        help="Measurements JSON (queries[].name + p95Ms)",
    )
    p.add_argument(
        "--dry-run",
        action="store_true",
        help="Ignore --measurements-json; synthesize measurements at 50 percent of each allowlist threshold.",
    )
    ns = p.parse_args(argv)

    allow = load_allowlist(ns.allowlist.resolve())

    if ns.dry_run:
        obs = synth_measurements_under(allow)
        print("TB-003 assert_query_performance.py: --dry-run mode (synthetic measurements).")
    else:
        if ns.measurements_json is None:
            print("error: --measurements-json is required unless --dry-run", file=sys.stderr)
            return 2
        obs = load_measurements(ns.measurements_json.resolve())

    failures: list[str] = []
    for name, threshold in sorted(allow.items(), key=lambda kv: kv[0]):
        if name not in obs:
            failures.append(f"missing measurement for allowlisted query {name!r}")
            continue

        if obs[name] > threshold + 1e-6:
            failures.append(f"{name}: p95Ms={obs[name]:.3f} exceeds threshold {threshold:.3f}")

    allow_set = set(allow)
    observed_names = set(obs)
    unknown_observed = sorted(observed_names - allow_set)

    print(f"allowlisted_queries={len(allow)} observed_query_names={len(observed_names)} ")
    print(f"allowlisted_with_observation={len(allow_set & observed_names)} ")
    print(f"non_allowlisted_observed={len(unknown_observed)} ")
    if unknown_observed:
        print("informational: non-allowlisted query names present: " + ", ".join(unknown_observed))

    if failures:
        print("TB-003 gate failures:", file=sys.stderr)
        for line in failures:
            print(f"  {line}", file=sys.stderr)
        return 1

    print("TB-003 query performance allowlist: OK")
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
