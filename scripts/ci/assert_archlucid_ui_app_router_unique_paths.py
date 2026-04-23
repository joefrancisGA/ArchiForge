#!/usr/bin/env python3
"""
Fail when two ``archlucid-ui/src/app/**/page.tsx`` files resolve to the same URL path.

Next.js App Router **route groups** — folders named ``(segment)`` — do **not** appear in the URL.
Two pages under ``(marketing)/foo`` and ``(operator)/foo`` both map to ``/foo``, which triggers:

  "You cannot have two parallel pages that resolve to the same path"

This check runs without Node/npm so CI and local contributors catch the mistake before ``next build`` / Docker.
"""

from __future__ import annotations

import argparse
import sys
from collections import defaultdict
from pathlib import Path


def repo_root() -> Path:
    return Path(__file__).resolve().parents[2]


def _omit_from_url(segment: str) -> bool:
    if segment.startswith("@"):  # parallel route slot — not part of URL
        return True
    if segment.startswith("_"):  # private folder — should not host routable pages
        return True
    if len(segment) >= 2 and segment[0] == "(" and segment[-1] == ")":
        # Route group ``(name)`` or intercept folder ``(.)name`` — neither adds a URL segment
        return True
    return False


def url_path_for_page(page_file: Path, app_dir: Path) -> str:
    rel_parent = page_file.parent.relative_to(app_dir)
    parts: list[str] = []
    for segment in rel_parent.parts:
        if _omit_from_url(segment):
            continue
        parts.append(segment)
    if not parts:
        return "/"
    return "/" + "/".join(parts)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--app-dir",
        type=Path,
        default=repo_root() / "archlucid-ui" / "src" / "app",
        help="Path to Next.js app directory (default: <repo>/archlucid-ui/src/app)",
    )
    args = parser.parse_args()
    app_dir: Path = args.app_dir.resolve()
    if not app_dir.is_dir():
        print(f"assert_archlucid_ui_app_router_unique_paths: missing app dir {app_dir}", file=sys.stderr)
        return 1

    pages = sorted(app_dir.rglob("page.tsx"))
    by_url: dict[str, list[Path]] = defaultdict(list)
    for p in pages:
        if not p.is_file():
            continue
        url = url_path_for_page(p, app_dir)
        by_url[url].append(p)

    dupes = {u: files for u, files in by_url.items() if len(files) > 1}
    if dupes:
        print(
            "assert_archlucid_ui_app_router_unique_paths: FAILED — duplicate URL paths "
            "(route groups do not disambiguate). Move one page under a distinct path segment, e.g. "
            "`/workspace/...` for signed-in-only mirrors of public routes.\n",
            file=sys.stderr,
        )
        for url in sorted(dupes):
            print(f"  URL {url!r}:", file=sys.stderr)
            for f in dupes[url]:
                print(f"    - {f.relative_to(repo_root())}", file=sys.stderr)
        return 1

    print(f"assert_archlucid_ui_app_router_unique_paths: OK ({len(pages)} page.tsx file(s), all unique URL paths).")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
