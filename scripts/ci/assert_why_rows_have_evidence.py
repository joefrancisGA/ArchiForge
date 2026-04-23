"""CI guard: every /why differentiation row must carry non-empty evidence fields.

Fails if ``claim``, ``archlucidEvidence``, ``competitorBaseline``, ``citation``, or
``narrativeParagraph`` is empty after trim in ``WHY_ARCHLUCID_COMPARISON_ROWS``.

Uses the same row extraction as ``check_why_archlucid_comparison_sync.py`` so field
parsing stays single-sourced.

Usage:
    python scripts/ci/assert_why_rows_have_evidence.py
"""

from __future__ import annotations

import importlib.util
import pathlib
import sys

_CI_DIR = pathlib.Path(__file__).resolve().parent


def _load_sync_module():
    spec = importlib.util.spec_from_file_location(
        "check_why_archlucid_comparison_sync",
        _CI_DIR / "check_why_archlucid_comparison_sync.py",
    )

    if spec is None or spec.loader is None:
        raise RuntimeError("Could not load check_why_archlucid_comparison_sync module spec.")

    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)

    return module


def main() -> int:
    sync = _load_sync_module()
    repo_root = pathlib.Path(__file__).resolve().parents[2]
    ts_path = repo_root / sync.TS_RELATIVE

    try:
        rows = sync._extract_ts_rows(sync._read_text(ts_path))
    except (FileNotFoundError, ValueError) as exc:
        print(f"error: {exc}", file=sys.stderr)

        return 2

    if len(rows) != 5:
        print(
            f"error: expected exactly 5 differentiation rows, found {len(rows)} in {sync.TS_RELATIVE}.",
            file=sys.stderr,
        )

        return 1

    for index, row in enumerate(rows):
        for field, value in zip(sync.ROW_FIELDS, row):
            if not value.strip():
                print(
                    f"error: row {index} field '{field}' is empty in {sync.TS_RELATIVE}.",
                    file=sys.stderr,
                )

                return 1

    print(f"OK: {len(rows)} /why rows have non-empty evidence fields in {sync.TS_RELATIVE}.")

    return 0


if __name__ == "__main__":
    sys.exit(main())
