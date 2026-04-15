#!/usr/bin/env python3
"""Validate offline agent eval dataset layout under tests/eval-datasets/.

Designed for future CI integration: currently shape-only (no live LLM or simulator runs).
Wire as a required check once deterministic eval execution is implemented.
"""

from __future__ import annotations

import json
import sys
from pathlib import Path


def _repo_root() -> Path:
    return Path(__file__).resolve().parents[2]


def _load_json(path: Path) -> object:
    return json.loads(path.read_text(encoding="utf-8"))


def main() -> int:
    root = _repo_root()
    base = root / "tests" / "eval-datasets"
    manifest_path = base / "manifest.json"

    if not manifest_path.is_file():
        print(f"::error::Missing {manifest_path}")
        return 1

    manifest = _load_json(manifest_path)
    if not isinstance(manifest, dict):
        print("::error::manifest.json must be an object")
        return 1

    if manifest.get("schemaVersion") != 1:
        print("::error::manifest.schemaVersion must be 1")
        return 1

    datasets = manifest.get("datasets")
    if not isinstance(datasets, list) or not datasets:
        print("::error::manifest.datasets must be a non-empty array")
        return 1

    for entry in datasets:
        if not isinstance(entry, dict):
            print("::error::each manifest.datasets entry must be an object")
            return 1

        kind = entry.get("agentKind")
        rel = entry.get("relativePath")
        minimum = entry.get("minCases")

        if not isinstance(kind, str) or not kind.strip():
            print("::error::dataset.agentKind required")
            return 1

        if not isinstance(rel, str) or not rel.strip():
            print("::error::dataset.relativePath required")
            return 1

        if not isinstance(minimum, int) or minimum < 1:
            print("::error::dataset.minCases must be a positive int")
            return 1

        data_path = base / rel
        if not data_path.is_file():
            print(f"::error::Missing dataset file {data_path}")
            return 1

        cases = _load_json(data_path)
        if not isinstance(cases, list):
            print(f"::error::{rel} must be a JSON array")
            return 1

        if len(cases) < minimum:
            print(f"::error::{rel} has {len(cases)} cases; minCases={minimum}")
            return 1

        for i, case in enumerate(cases):
            if not isinstance(case, dict):
                print(f"::error::{rel}[{i}] must be an object")
                return 1

            if "id" not in case or not isinstance(case["id"], str):
                print(f"::error::{rel}[{i}].id must be a string")
                return 1

            expect = case.get("expect")
            if not isinstance(expect, dict):
                print(f"::error::{rel}[{i}].expect must be an object")
                return 1

            for key in ("minFindings", "maxFindings"):
                if key not in expect or not isinstance(expect[key], int):
                    print(f"::error::{rel}[{i}].expect.{key} must be int")
                    return 1

            if expect["minFindings"] > expect["maxFindings"]:
                print(f"::error::{rel}[{i}].expect minFindings > maxFindings")
                return 1

    print(f"Eval dataset manifest OK: {len(datasets)} dataset(s).")
    return 0


if __name__ == "__main__":
    sys.exit(main())
