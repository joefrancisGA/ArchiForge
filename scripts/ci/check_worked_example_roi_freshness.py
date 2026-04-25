"""Warn (or optionally fail) when WORKED_EXAMPLE_ROI.pdf is stale.

Default behavior (V1): print a stderr warning when the PDF is older than 30 days, exit 0.

Strict mode: set environment variable ``WORKED_EXAMPLE_ROI_FRESHNESS_FAIL=1`` to exit 1 when the file is missing
or older than 30 days (merge-blocking gate).

Usage:
    python scripts/ci/check_worked_example_roi_freshness.py
    python scripts/ci/check_worked_example_roi_freshness.py --repo-root /path/to/repo

Exit codes:
    0 — fresh enough, or warn-only stale/missing
    1 — strict mode and stale/missing
    2 — invocation error
"""

from __future__ import annotations

import argparse
import datetime as dt
import os
import pathlib
import sys

PDF_RELATIVE = pathlib.Path("docs/go-to-market/WORKED_EXAMPLE_ROI.pdf")
MAX_AGE_DAYS = 30


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo-root", type=pathlib.Path, default=pathlib.Path.cwd())
    args = parser.parse_args()
    repo_root: pathlib.Path = args.repo_root.resolve()
    pdf_path = repo_root / PDF_RELATIVE

    enforce = os.environ.get("WORKED_EXAMPLE_ROI_FRESHNESS_FAIL", "").strip() == "1"

    if not pdf_path.is_file():
        message = f"WORKED_EXAMPLE_ROI freshness: PDF missing at {pdf_path}."
        print(message, file=sys.stderr)

        return 1 if enforce else 0

    mtime = dt.datetime.fromtimestamp(pdf_path.stat().st_mtime, tz=dt.timezone.utc)
    age = dt.datetime.now(tz=dt.timezone.utc) - mtime

    if age > dt.timedelta(days=MAX_AGE_DAYS):
        message = (
            f"WORKED_EXAMPLE_ROI freshness: {pdf_path.name} is {age.days} days old "
            f"(>{MAX_AGE_DAYS}d). Regenerate via scripts/ops/generate-worked-example-roi.ps1."
        )
        print(message, file=sys.stderr)

        return 1 if enforce else 0

    print(f"WORKED_EXAMPLE_ROI freshness: OK ({pdf_path.name}, age {age.days}d).")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
