#!/usr/bin/env python3
"""Fail if ``docs/*.md`` count exceeds the documentation-surface budget (default 31).

Default raised from 30 → 31 to include ``docs/CONTRIBUTOR_ON_ONE_PAGE.md`` at repo root next to
``READ_THIS_FIRST.md`` without moving depth content; see ``docs/CHANGELOG.md``.
"""

from __future__ import annotations

import argparse
import sys
from pathlib import Path


def repo_root() -> Path:
    return Path(__file__).resolve().parents[2]


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--max",
        type=int,
        default=31,
        help="Maximum markdown files allowed directly under docs/ (default: 31; includes contributor one-pager).",
    )
    parser.add_argument(
        "--docs-dir",
        type=Path,
        default=repo_root() / "docs",
        help="Path to docs directory (default: <repo>/docs)",
    )
    args = parser.parse_args()
    docs_dir: Path = args.docs_dir.resolve()
    if not docs_dir.is_dir():
        print(f"assert_docs_root_size: missing docs dir {docs_dir}", file=sys.stderr)
        return 1
    count = sum(1 for p in docs_dir.glob("*.md") if p.is_file())
    if count > args.max:
        print(
            f"assert_docs_root_size: FAILED — {count} *.md under {docs_dir} (max {args.max}). "
            "Move depth content to docs/library/ or docs/<topic>/ and keep buyer spine at root.",
            file=sys.stderr,
        )
        return 1
    print(f"assert_docs_root_size: OK ({count} markdown file(s) at docs root, max {args.max}).")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
