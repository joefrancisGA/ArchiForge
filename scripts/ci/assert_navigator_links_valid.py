"""Fail if ``docs/NAVIGATOR.md`` links to a missing file (repo-relative).

Reuses the same link extraction and path resolution as ``check_read_this_first_links.py``:
``http`` / ``https`` / ``mailto`` / ``tel`` targets and empty ``#`` anchors are skipped;
fragment-only targets are ignored.

Usage:
    python scripts/ci/assert_navigator_links_valid.py
    python scripts/ci/assert_navigator_links_valid.py --repo-root /path/to/repo
"""

from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path
from urllib.parse import unquote

LINK_RE = re.compile(r"(?<!\!)\[[^\]]*\]\(([^()]*(?:\([^()]*\))*[^()]*)\)")

NAVIGATOR = Path("docs") / "NAVIGATOR.md"


def repo_root() -> Path:
    return Path(__file__).resolve().parents[2]


def should_skip_target(raw: str) -> bool:
    t = raw.strip()
    if not t or t.startswith("#"):
        return True
    if t.startswith("http://") or t.startswith("https://"):
        return True
    if t.startswith("mailto:") or t.startswith("tel:"):
        return True
    if "{" in t or "*" in t:
        return True
    return False


def resolve_target(md_file: Path, target: str) -> Path | None:
    t = target.strip()
    pos = t.find("#")
    if pos >= 0:
        t = t[:pos].strip()
    if not t or should_skip_target(t):
        return None
    t = unquote(t)
    resolved = (md_file.parent / t).resolve()
    try:
        resolved.relative_to(repo_root())
    except ValueError:
        return None
    return resolved


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo-root", type=Path, default=repo_root())
    args = parser.parse_args()
    root: Path = args.repo_root.resolve()
    md_path = root / NAVIGATOR
    if not md_path.is_file():
        print(f"Missing required file: {md_path}", file=sys.stderr)
        return 2
    text = md_path.read_text(encoding="utf-8")
    missing: list[str] = []
    for match in LINK_RE.finditer(text):
        raw = match.group(1).strip()
        resolved = resolve_target(md_path, raw)
        if resolved is None:
            continue
        if not resolved.is_file():
            missing.append(f"{raw} -> {resolved}")
    if missing:
        print("assert_navigator_links_valid: broken relative links in docs/NAVIGATOR.md:", file=sys.stderr)
        for line in missing:
            print(f"  {line}", file=sys.stderr)
        return 1
    print("assert_navigator_links_valid: OK.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
