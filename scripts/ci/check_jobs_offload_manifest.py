#!/usr/bin/env python3
"""Validate Production Worker offload invariant: every Jobs:OffloadedToContainerJobs slug is listed in Jobs:DeployedContainerJobNames.

Usage (example):
  python scripts/ci/check_jobs_offload_manifest.py \\
    --offloaded advisory-scan,orphan-probe \\
    --deployed advisory-scan,orphan-probe,data-archival

Exit 1 when any offloaded name is missing from the deployed manifest (comma-separated, case-insensitive).
"""

from __future__ import annotations

import argparse
import sys


def _parse_csv(raw: str) -> list[str]:
    parts = [p.strip() for p in raw.split(",")]
    return [p for p in parts if p]


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--offloaded",
        required=True,
        help="Comma-separated Jobs:OffloadedToContainerJobs values.",
    )
    parser.add_argument(
        "--deployed",
        required=True,
        help="Comma-separated Jobs:DeployedContainerJobNames manifest (from Terraform / operator).",
    )
    args = parser.parse_args()

    off = _parse_csv(args.offloaded)
    dep = {n.casefold() for n in _parse_csv(args.deployed)}

    missing = [n for n in off if n.casefold() not in dep]

    if missing:
        print(
            "Jobs offload manifest check failed: offloaded job(s) not in deployed manifest: "
            + ", ".join(missing),
            file=sys.stderr,
        )

        return 1

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
