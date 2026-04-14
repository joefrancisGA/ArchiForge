#!/usr/bin/env python3
"""Fail CI if k6 ci-smoke summary-export JSON exceeds http_req_failed rate or p(95) latency."""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path


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
        help="Maximum allowed http_req_duration p(95) in ms (align with docs/LOAD_TEST_BASELINE.md CI gate)",
    )
    args = parser.parse_args()

    path: Path = args.summary_json
    if not path.is_file():
        print(f"error: missing k6 summary file: {path}", file=sys.stderr)
        return 2

    payload = json.loads(path.read_text(encoding="utf-8"))
    failed = _metric_values(payload, "http_req_failed")
    duration = _metric_values(payload, "http_req_duration")

    failed_rate = _float(failed, "rate")
    p95_ms = _float(duration, "p(95)")

    errors: list[str] = []
    if failed_rate is not None and failed_rate > args.max_failed_rate + 1e-12:
        errors.append(
            f"http_req_failed rate {failed_rate:.6f} exceeds cap {args.max_failed_rate:.6f}",
        )
    if p95_ms is not None and p95_ms > args.max_p95_ms + 1e-9:
        errors.append(
            f"http_req_duration p(95) {p95_ms:.1f} ms exceeds cap {args.max_p95_ms:.0f} ms",
        )

    if errors:
        print("k6 CI smoke gate failed:", file=sys.stderr)
        for line in errors:
            print(f"  - {line}", file=sys.stderr)
        return 1

    print(
        f"k6 CI smoke gate OK (http_req_failed rate={failed_rate!s}, "
        f"http_req_duration p(95)={p95_ms!s} ms)",
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
