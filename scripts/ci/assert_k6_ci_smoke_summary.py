#!/usr/bin/env python3
"""Fail CI if k6 ci-smoke summary-export JSON exceeds http_req_failed rate or p(95) latency.

Supports two modes:
  1. **Global** (default): checks overall ``http_req_duration`` p(95) against ``--max-p95-ms``.
  2. **Per-tag** (``--per-tag-ci-smoke``): checks per-``k6ci`` tag p(95) against built-in caps
     matching ``tests/load/ci-smoke.js`` thresholds.  Falls back to global if tagged metrics
     are absent (older k6 or different script).
"""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

# Per-tag p95 caps (ms) aligned with tests/load/ci-smoke.js thresholds.
# Key = k6 summary metric name for the tagged sub-metric.
_CI_SMOKE_TAG_CAPS: dict[str, float] = {
    "http_req_duration{k6ci:health_live}": 500.0,
    "http_req_duration{k6ci:health_ready}": 1500.0,
    "http_req_duration{k6ci:create_run}": 3000.0,
    "http_req_duration{k6ci:list_runs}": 1500.0,
    "http_req_duration{k6ci:audit_search}": 1500.0,
    "http_req_duration{k6ci:version}": 1500.0,
    "http_req_duration{k6ci:list_for_get_run}": 1500.0,
    "http_req_duration{k6ci:get_run_detail}": 2500.0,
    "http_req_duration{k6ci:client_error_telemetry}": 1500.0,
}


def _metric_values(payload: dict, metric_name: str) -> dict:
    metrics = payload.get("metrics")
    if not isinstance(metrics, dict):
        return {}

    block = metrics.get(metric_name)
    if not isinstance(block, dict):
        return {}

    values = block.get("values")
    if isinstance(values, dict):
        return values

    trend_keys = ("rate", "count", "med", "p(50)", "p(95)", "p(99)")
    return {key: block[key] for key in trend_keys if key in block}


def _float(values: dict, *keys: str) -> float | None:
    for key in keys:
        if key in values and values[key] is not None:
            try:
                return float(values[key])
            except (TypeError, ValueError):
                return None
    return None


def _check_per_tag(payload: dict, errors: list[str]) -> bool:
    """Check per-tag p95 caps.  Returns True if at least one tagged metric was found."""
    found_any = False

    for metric_name, cap_ms in _CI_SMOKE_TAG_CAPS.items():
        values = _metric_values(payload, metric_name)
        p95 = _float(values, "p(95)")

        if p95 is None:
            continue

        found_any = True

        if p95 > cap_ms + 1e-9:
            errors.append(
                f"{metric_name} p(95) {p95:.1f} ms exceeds cap {cap_ms:.0f} ms",
            )

    return found_any


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("summary_json", type=Path, help="k6 --summary-export JSON path")
    parser.add_argument(
        "--max-failed-rate",
        type=float,
        default=0.0,
        help="Maximum allowed http_req_failed rate (default: 0 — no failed checks)",
    )
    parser.add_argument(
        "--max-p95-ms",
        type=float,
        default=1500.0,
        help="Maximum allowed global http_req_duration p(95) in ms (fallback when --per-tag-ci-smoke tags are missing)",
    )
    parser.add_argument(
        "--per-tag-ci-smoke",
        action="store_true",
        default=False,
        help="Enforce per-k6ci-tag p95 caps matching tests/load/ci-smoke.js thresholds; falls back to global --max-p95-ms if tags are absent",
    )
    args = parser.parse_args()

    path: Path = args.summary_json
    if not path.is_file():
        print(f"error: missing k6 summary file: {path}", file=sys.stderr)
        return 2

    payload = json.loads(path.read_text(encoding="utf-8"))
    failed = _metric_values(payload, "http_req_failed")

    failed_rate = _float(failed, "rate")

    errors: list[str] = []

    if failed_rate is not None and failed_rate > args.max_failed_rate + 1e-12:
        errors.append(
            f"http_req_failed rate {failed_rate:.6f} exceeds cap {args.max_failed_rate:.6f}",
        )

    if args.per_tag_ci_smoke:
        found = _check_per_tag(payload, errors)

        if not found:
            print(
                "warning: --per-tag-ci-smoke requested but no tagged metrics found; "
                "falling back to global --max-p95-ms",
                file=sys.stderr,
            )
            duration = _metric_values(payload, "http_req_duration")
            p95_ms = _float(duration, "p(95)")

            if p95_ms is not None and p95_ms > args.max_p95_ms + 1e-9:
                errors.append(
                    f"http_req_duration p(95) {p95_ms:.1f} ms exceeds cap {args.max_p95_ms:.0f} ms (global fallback)",
                )
    else:
        duration = _metric_values(payload, "http_req_duration")
        p95_ms = _float(duration, "p(95)")

        if p95_ms is not None and p95_ms > args.max_p95_ms + 1e-9:
            errors.append(
                f"http_req_duration p(95) {p95_ms:.1f} ms exceeds cap {args.max_p95_ms:.0f} ms",
            )

    if errors:
        print("k6 CI smoke gate failed:", file=sys.stderr)
        for line in errors:
            print(f"  - {line}", file=sys.stderr)
        return 1

    mode = "per-tag" if args.per_tag_ci_smoke else "global"
    print(f"k6 CI smoke gate OK ({mode}; http_req_failed rate={failed_rate!s})")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
