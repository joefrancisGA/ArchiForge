#!/usr/bin/env python3
"""Fail if ``docs/CONTRIBUTOR_ON_ONE_PAGE.md`` exceeds the hard line budget (80)."""

from __future__ import annotations

import argparse
import sys
from pathlib import Path


def repo_root() -> Path:
    return Path(__file__).resolve().parents[2]


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--repo-root",
        type=Path,
        default=repo_root(),
        help="Repository root (default: inferred from this script).",
    )
    parser.add_argument(
        "--max-lines",
        type=int,
        default=80,
        help="Maximum line count including blank lines and fences (default: 80).",
    )
    parser.add_argument(
        "--path",
        type=Path,
        default=None,
        help="Override path to the markdown file (default: <repo>/docs/CONTRIBUTOR_ON_ONE_PAGE.md).",
    )
    args = parser.parse_args(argv)
    root: Path = args.repo_root.resolve()
    doc_path: Path = (args.path if args.path is not None else root / "docs" / "CONTRIBUTOR_ON_ONE_PAGE.md").resolve()

    if not doc_path.is_file():
        print(f"assert_contributor_on_one_page_size: missing {doc_path}", file=sys.stderr)
        return 1

    text = doc_path.read_text(encoding="utf-8", errors="replace")
    line_count = len(text.splitlines())

    if line_count > args.max_lines:
        print(
            f"assert_contributor_on_one_page_size: FAILED — {doc_path.relative_to(root)} has {line_count} lines "
            f"(max {args.max_lines}). Keep the one-pager terse; link out instead of inlining.",
            file=sys.stderr,
        )
        return 1

    print(
        f"assert_contributor_on_one_page_size: OK ({line_count} line(s), max {args.max_lines}) "
        f"for {doc_path.relative_to(root)}."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
