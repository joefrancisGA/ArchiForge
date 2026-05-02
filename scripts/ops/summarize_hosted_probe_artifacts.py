#!/usr/bin/env python3
"""Summarize downloaded hosted-saas-probe probe-result.json artifacts (local files only)."""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path


def load_payload(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8"))


def main(argv: list[str]) -> int:
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument(
        "paths",
        nargs="*",
        type=Path,
        help="probe-result.json files; if empty, read JSON objects from stdin one per line",
    )
    ns = p.parse_args(argv)

    rows: list[dict] = []

    if ns.paths:
        for path in ns.paths:
            rows.append(load_payload(path))
    else:
        for line in sys.stdin:
            line = line.strip()

            if not line:
                continue

            rows.append(json.loads(line))

    attempted = sum(1 for r in rows if not r.get("skipped"))
    skipped = sum(1 for r in rows if r.get("skipped"))
    ok = sum(
        1
        for r in rows
        if not r.get("skipped") and r.get("live_ok") is True and r.get("ready_ok") is True
    )

    print(f"files: {len(rows)}")
    print(f"skipped_no_base_url: {skipped}")
    print(f"attempted_probe: {attempted}")
    print(f"both_endpoints_ok: {ok}")

    if attempted:
        print(f"success_ratio_attempted: {ok / attempted:.4f}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
