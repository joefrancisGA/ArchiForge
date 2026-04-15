#!/usr/bin/env python3
"""
Compare Stryker.NET JSON mutation-report score to a committed baseline (scheduled CI regression guard).

Uses the mutation-testing-elements JSON shape: top-level ``files`` map with per-file ``mutants`` arrays
and ``status`` strings. If the report embeds a numeric ``mutationScore`` anywhere, that value is
preferred so we stay aligned with Stryker's own calculation.
"""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path


def _deep_find_mutation_score(obj: object) -> float | None:
    """Return first numeric mutationScore found in nested dict/list structure."""
    if isinstance(obj, dict):
        raw = obj.get("mutationScore")
        if isinstance(raw, (int, float)):
            return float(raw)
        for v in obj.values():
            found = _deep_find_mutation_score(v)
            if found is not None:
                return found
    elif isinstance(obj, list):
        for x in obj:
            found = _deep_find_mutation_score(x)
            if found is not None:
                return found
    return None


def _collect_mutant_statuses(obj: object) -> list[str]:
    statuses: list[str] = []

    def walk(o: object) -> None:
        if isinstance(o, dict):
            mutants = o.get("mutants")
            if isinstance(mutants, list):
                for m in mutants:
                    if isinstance(m, dict):
                        st = m.get("status")
                        if isinstance(st, str):
                            statuses.append(st)
            for v in o.values():
                walk(v)
        elif isinstance(o, list):
            for x in o:
                walk(x)

    walk(obj)
    return statuses


def mutation_score_from_report(data: dict) -> tuple[float, int, int]:
    """
    Return (score_percent_0_100, detected_count, denominator_count).

    Denominator excludes Ignored / NoCoverage / Pending (not meaningfully tested for score).
    Detected = Killed, Timeout, RuntimeError, CompileError (mutant did not survive undetected).
    """
    embedded = _deep_find_mutation_score(data)
    if embedded is not None:
        return embedded, 0, 0

    statuses = _collect_mutant_statuses(data)
    skip = {"Ignored", "NoCoverage", "Pending"}
    detected_labels = {"Killed", "Timeout", "RuntimeError", "CompileError"}

    denom = 0
    detected = 0
    for st in statuses:
        if st in skip:
            continue
        denom += 1
        if st in detected_labels:
            detected += 1

    if denom == 0:
        return 0.0, 0, 0

    return 100.0 * detected / denom, detected, denom


def find_mutation_report_json(stryker_root: Path) -> Path | None:
    """Pick the newest mutation-report.json under StrykerOutput (by mtime)."""
    if not stryker_root.is_dir():
        return None
    candidates = sorted(
        stryker_root.rglob("mutation-report.json"),
        key=lambda p: p.stat().st_mtime,
        reverse=True,
    )
    return candidates[0] if candidates else None


def _main() -> int:
    parser = argparse.ArgumentParser(description="Fail if Stryker mutation score regresses below baseline.")
    parser.add_argument("--baseline", type=Path, required=True, help="stryker-baselines.json path.")
    parser.add_argument("--label", required=True, help="Matrix label key (e.g. Persistence, Decisioning).")
    parser.add_argument(
        "--stryker-root",
        type=Path,
        default=Path("StrykerOutput"),
        help="Root directory Stryker writes to (default StrykerOutput).",
    )
    parser.add_argument(
        "--tolerance",
        type=float,
        default=0.15,
        help="Allowed drop below baseline in percentage points (default 0.15).",
    )
    parser.add_argument(
        "--allow-zero-denominator",
        action="store_true",
        help=(
            "Exit 0 when no scored mutants exist (denominator 0) and no embedded mutationScore. "
            "Use for Stryker --since runs where the diff produced no in-scope mutants."
        ),
    )
    args = parser.parse_args()

    if not args.baseline.is_file():
        print(f"Baseline file missing: {args.baseline}", file=sys.stderr)
        return 2

    baseline_doc = json.loads(args.baseline.read_text(encoding="utf-8"))
    entry = baseline_doc.get(args.label)
    if not isinstance(entry, dict):
        print(f"No baseline entry for label {args.label!r} in {args.baseline}.", file=sys.stderr)
        return 2

    base_raw = entry.get("mutationScore")
    if not isinstance(base_raw, (int, float)):
        print(f"baseline[{args.label!r}].mutationScore must be a number.", file=sys.stderr)
        return 2

    baseline = float(base_raw)

    report_path = find_mutation_report_json(args.stryker_root)
    if report_path is None:
        print(
            f"No mutation-report.json found under {args.stryker_root} "
            "(enable json reporter in stryker-config).",
            file=sys.stderr,
        )
        return 2

    data = json.loads(report_path.read_text(encoding="utf-8"))
    if not isinstance(data, dict):
        print("Mutation report JSON root must be an object.", file=sys.stderr)
        return 2

    score, detected, denom = mutation_score_from_report(data)
    floor = baseline - args.tolerance

    embedded = _deep_find_mutation_score(data)
    if (
        args.allow_zero_denominator
        and denom == 0
        and embedded is None
    ):
        print(
            f"OK: no mutants in report scope (denominator 0); skipping baseline compare "
            f"(report {report_path}).",
        )
        return 0

    print(
        f"Stryker mutation score: {score:.2f}% (baseline {baseline:.2f}%, floor {floor:.2f}%, "
        f"report {report_path})",
    )
    if detected or denom:
        print(f"  (computed from mutants: detected={detected}, denominator={denom})")

    if score + 1e-9 < floor:
        print(
            f"REGRESSION: score {score:.2f}% is below baseline floor {floor:.2f}% "
            f"(baseline {baseline:.2f}% minus tolerance {args.tolerance:.2f} pp).",
            file=sys.stderr,
        )
        return 1

    delta = score - baseline
    print(f"OK: {delta:+.2f} pp vs baseline (tolerance {args.tolerance:.2f} pp).")
    return 0


if __name__ == "__main__":
    raise SystemExit(_main())
