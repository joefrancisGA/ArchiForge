#!/usr/bin/env python3
"""
One-time (or idempotent) back-fill: prepend `> **Scope:** ...` to every
``docs/**/*.md`` file that does not yet satisfy ``check_doc_scope_header.py``.

Skips ``docs/archive/`` (same default as the checker). Safe to re-run: files
that already have a valid scope header are unchanged.

Usage (from repo root)::

    python scripts/ci/backfill_doc_scope_headers.py --dry-run
    python scripts/ci/backfill_doc_scope_headers.py
"""

from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

# Same directory import when executed as ``python scripts/ci/backfill_...``.
_SCRIPT_DIR = Path(__file__).resolve().parent
if str(_SCRIPT_DIR) not in sys.path:
    sys.path.insert(0, str(_SCRIPT_DIR))

import check_doc_scope_header as scope_check  # noqa: E402  pylint: disable=wrong-import-position

UTF8_BOM = "\ufeff"

# First markdown ATX heading in the first N lines (after optional BOM / blank lines at file start).
_HEADING_RE = re.compile(r"^(#{1,6})\s+(.+?)\s*$", re.MULTILINE)


def repo_root() -> Path:
    return Path(__file__).resolve().parents[2]


def _strip_bom(s: str) -> str:
    if s.startswith(UTF8_BOM):
        return s[len(UTF8_BOM) :]

    return s


def _first_heading_summary(content: str, *, scan_first_lines: int) -> str | None:
    """Return plain text after leading #s for the first ATX heading in the first *scan_first_lines* lines."""

    text = _strip_bom(content)
    lines = text.splitlines()

    head = "\n".join(lines[:scan_first_lines])

    m = _HEADING_RE.search(head)

    if not m:
        return None

    title = m.group(2).strip()

    # Strip inline emphasis markers for a one-line scope (keeps line readable in raw markdown).
    title = title.replace("**", "").replace("`", "")

    if len(title) > 160:
        title = title[:157] + "..."

    return title


def _fallback_from_path(rel_under_docs: Path) -> str:
    parts = [p.replace("-", " ").replace("_", " ") for p in rel_under_docs.parts[:-1]]
    stem = rel_under_docs.stem.replace("_", " ").replace("-", " ")

    if parts:
        return f"{' / '.join(parts)} / {stem}"

    return stem


def build_scope_line(rel_under_docs: Path, content: str) -> str:
    heading = _first_heading_summary(content, scan_first_lines=80)

    if heading:
        return f"> **Scope:** {heading} - full detail, tables, and links in the sections below."

    fb = _fallback_from_path(rel_under_docs)

    return f"> **Scope:** {fb} - technical documentation; see body for narrative and cross-links."


def prepend_scope(content: str, scope_line: str) -> str:
    """Prepend scope after optional UTF-8 BOM."""

    if content.startswith(UTF8_BOM):
        return UTF8_BOM + scope_line + "\n\n" + content[len(UTF8_BOM) :].lstrip("\n")

    return scope_line + "\n\n" + content.lstrip("\n")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--dry-run", action="store_true", help="Print actions only; do not write files.")
    parser.add_argument(
        "--docs-dir",
        type=Path,
        default=repo_root() / "docs",
        help="Root directory to scan (default: <repo>/docs).",
    )
    parser.add_argument(
        "--exclude-archive",
        action=argparse.BooleanOptionalAction,
        default=True,
        help="Skip docs/archive/** (default: true).",
    )

    args = parser.parse_args()
    docs_dir: Path = args.docs_dir

    if not docs_dir.is_dir():
        print(f"::error::docs dir not found: {docs_dir}", file=sys.stderr)
        return 2

    changed = 0
    skipped = 0

    for md in scope_check.iter_markdown_files(docs_dir, exclude_archive=args.exclude_archive):
        rel_docs = md.relative_to(docs_dir)
        raw = md.read_text(encoding="utf-8", errors="replace")

        if scope_check.has_valid_docs_scope_header(raw):
            skipped += 1
            continue

        scope_line = build_scope_line(rel_docs, raw)
        new_text = prepend_scope(raw, scope_line)

        if args.dry_run:
            # Avoid UnicodeEncodeError on Windows consoles (cp1252) when headings contain arrows, etc.
            rel = str(md.relative_to(repo_root()))
            print(f"would update: {rel.encode('ascii', errors='replace').decode('ascii')}")
            changed += 1
            continue

        md.write_text(new_text, encoding="utf-8", newline="\n")
        rel = str(md.relative_to(repo_root()))
        print(f"updated: {rel.encode('ascii', errors='replace').decode('ascii')}")
        changed += 1

    mode = "dry-run" if args.dry_run else "write"

    print(f"backfill_doc_scope_headers ({mode}): changed={changed}, already_ok={skipped}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
