#!/usr/bin/env python3
"""
Verify that markdown links in docs/trust-center.md resolve to existing repo files.

Resolves:
  - https://github.com/joefrancisGA/ArchLucid/blob/main/<path>
  - Relative paths from docs/ (e.g. security/X.md)
Skips mailto:, http(s) non-blob GitHub URLs, and anchors-only targets.
"""

from __future__ import annotations

import re
import sys
from pathlib import Path
from urllib.parse import unquote

LINK_RE = re.compile(r"\[[^\]]*\]\(([^)]+)\)")
GITHUB_BLOB_PREFIX = "https://github.com/joefrancisGA/ArchLucid/blob/main/"


def repo_root() -> Path:
    return Path(__file__).resolve().parents[2]


def resolve_url_target(url: str, trust_md: Path, root: Path) -> Path | None:
    u = url.strip()
    if not u or u.startswith("#") or u.startswith("mailto:"):
        return None

    if u.startswith(GITHUB_BLOB_PREFIX):
        rel = unquote(u[len(GITHUB_BLOB_PREFIX) :].split("#", 1)[0].strip())
        return (root / rel).resolve()

    if u.startswith("http://") or u.startswith("https://"):
        return None

    # Relative to docs/ (parent of trust-center.md)
    base = trust_md.parent
    rel = unquote(u.split("#", 1)[0].strip())
    if not rel:
        return None

    return (base / rel).resolve()


def main() -> int:
    root = repo_root()
    trust_md = root / "docs" / "trust-center.md"

    if not trust_md.is_file():
        print(f"ERROR: missing {trust_md.relative_to(root)}", file=sys.stderr)
        return 1

    text = trust_md.read_text(encoding="utf-8")
    broken: list[str] = []

    for m in LINK_RE.finditer(text):
        raw = m.group(1).strip()
        if '"' in raw or "'" in raw:
            continue

        target = resolve_url_target(raw, trust_md, root)
        if target is None:
            continue

        try:
            target.relative_to(root)
        except ValueError:
            broken.append(f"escape/outside repo: {raw}")
            continue

        if not target.is_file():
            broken.append(f"missing file: {raw} -> {target.relative_to(root).as_posix()}")

    if broken:
        print("Trust center link check failed:", file=sys.stderr)
        for b in broken:
            print(f"  {b}", file=sys.stderr)
        return 1

    print("Trust center links OK:", trust_md.relative_to(root).as_posix())
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
