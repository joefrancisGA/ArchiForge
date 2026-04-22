"""Fail if README.md has more than N markdown links above the first <details> block."""
from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

# Inline markdown links [text](url) — excludes bare URLs and reference-style [ref][id].
_LINK_RE = re.compile(r"\[[^\]]*\]\([^)\s]+\)")


def count_links_before_first_details(text: str) -> tuple[int, str | None]:
    lower = text.lower()
    idx = lower.find("<details")
    if idx == -1:
        return -1, "No <details> block found in README.md — opener must stay collapsible."

    prefix = text[:idx]
    return len(_LINK_RE.findall(prefix)), None


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--readme",
        type=Path,
        default=None,
        help="Path to README.md (default: repo root README.md)",
    )
    parser.add_argument("--max-links", type=int, default=12, help="Maximum markdown links allowed above <details>")
    args = parser.parse_args()

    root = Path(__file__).resolve().parents[2]
    readme = args.readme if args.readme is not None else root / "README.md"
    if not readme.is_file():
        print(f"README not found: {readme}", file=sys.stderr)
        return 1

    text = readme.read_text(encoding="utf-8")
    count, err = count_links_before_first_details(text)
    if err is not None:
        print(err, file=sys.stderr)
        return 1

    if count > args.max_links:
        print(
            f"README opener has {count} markdown [text](url) links; max is {args.max_links}. "
            "Move links into the <details> block or raise the budget with explicit team approval.",
            file=sys.stderr,
        )
        return 1

    print(f"OK: README opener has {count} markdown link(s) (max {args.max_links}).")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
