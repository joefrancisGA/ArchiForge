#!/usr/bin/env python3
r"""
check_doc_scope_header.py
-------------------------
Enforces a small documentation hygiene invariant: every active ``docs/**/*.md``
file must open with a **Scope** line so readers (and agents) know audience,
intent, and boundaries before the first heading.

**Rule (active docs under ``docs/`` — excluding ``docs/archive/`` by default)**

After optional UTF-8 BOM and leading blank lines, the first non-empty line must
match::

    ^\s*>\s*\*\*Scope:\*\*

That is a GFM **blockquote** whose visible text starts with ``**Scope:**``.
Additional text on the same line is allowed (and encouraged) after the colon.

**Optional README.md (repo root)**

The root ``README.md`` is not under ``docs/``. When ``--check-readme`` is set
(default: on), the same "first non-empty line" rule applies, but the scope may
alternatively be expressed as a single-line HTML comment::

    <!-- **Scope:** ... -->

Why HTML for README: the file is overwhelmingly H1-first for GitHub rendering;
a leading ``>`` blockquote would push the product title below a quote box.

Exit codes
~~~~~~~~~~
* ``0`` — every scanned file satisfies the rule.
* ``1`` — one or more files are missing a valid scope header (CI treats this as
  a merge-blocking failure once the docs tree is back-filled; use
  ``scripts/ci/backfill_doc_scope_headers.py`` for the mechanical prepend).

Run::

    python scripts/ci/check_doc_scope_header.py
    python scripts/ci/check_doc_scope_header.py --no-readme
"""

from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

UTF8_BOM = "\ufeff"

# First non-empty line must be a blockquote starting with **Scope:**
SCOPE_BLOCKQUOTE_RE = re.compile(r"^\s*>\s*\*\*Scope:\*\*", re.IGNORECASE)

# README-only: single-line HTML comment starting with <!-- **Scope:**
SCOPE_README_HTML_RE = re.compile(r"^\s*<!--\s*\*\*Scope:\*\*", re.IGNORECASE)


def repo_root() -> Path:
    return Path(__file__).resolve().parents[2]


def strip_leading_bom_and_blank_lines(text: str) -> str:
    if text.startswith(UTF8_BOM):
        text = text[len(UTF8_BOM) :]

    lines = text.splitlines()

    start = 0

    while start < len(lines) and not lines[start].strip():
        start += 1

    return "\n".join(lines[start:])


def first_non_empty_line(text: str) -> str | None:
    stripped = strip_leading_bom_and_blank_lines(text)

    if not stripped:
        return None

    first_line = stripped.splitlines()[0]

    return first_line if first_line.strip() else None


def has_valid_docs_scope_header(content: str) -> bool:
    first = first_non_empty_line(content)

    if first is None:
        return False

    return bool(SCOPE_BLOCKQUOTE_RE.match(first))


def has_valid_readme_scope_header(content: str) -> bool:
    first = first_non_empty_line(content)

    if first is None:
        return False

    if SCOPE_BLOCKQUOTE_RE.match(first):
        return True

    return bool(SCOPE_README_HTML_RE.match(first))


def iter_markdown_files(docs_dir: Path, *, exclude_archive: bool) -> list[Path]:
    if not docs_dir.is_dir():
        return []

    paths: list[Path] = []

    for path in sorted(docs_dir.rglob("*.md")):
        if not path.is_file():
            continue

        if exclude_archive:
            try:
                rel = path.relative_to(docs_dir)

                if rel.parts and rel.parts[0] == "archive":
                    continue
            except ValueError:
                continue

        paths.append(path)

    return paths


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
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
    parser.add_argument(
        "--check-readme",
        action=argparse.BooleanOptionalAction,
        default=True,
        help="Also validate repo root README.md (default: true).",
    )
    parser.add_argument(
        "--max-list",
        type=int,
        default=80,
        help="Maximum number of violating paths to print (default: 80).",
    )

    args = parser.parse_args(argv)

    violations: list[str] = []

    if args.check_readme:
        readme = repo_root() / "README.md"

        if readme.is_file():
            text = readme.read_text(encoding="utf-8", errors="replace")

            if not has_valid_readme_scope_header(text):
                violations.append(str(readme.relative_to(repo_root())))
        else:
            print("::warning::README.md not found at repo root; skipping.", file=sys.stderr)

    for md in iter_markdown_files(args.docs_dir, exclude_archive=args.exclude_archive):
        text = md.read_text(encoding="utf-8", errors="replace")

        if not has_valid_docs_scope_header(text):
            try:
                violations.append(str(md.relative_to(repo_root())))
            except ValueError:
                violations.append(str(md))

    if not violations:
        print(
            "check_doc_scope_header: OK (scope blockquote present on first non-empty line "
            f"for all scanned docs under {args.docs_dir}"
            + ("; README.md OK" if args.check_readme else "")
            + ")."
        )
        return 0

    print(
        f"::warning::{len(violations)} markdown file(s) missing a leading `> **Scope:**` blockquote "
        f"(or README missing `<!-- **Scope:**`). First {min(len(violations), args.max_list)}:",
        file=sys.stderr,
    )

    for path in violations[: args.max_list]:
        print(f"  - {path}", file=sys.stderr)

    if len(violations) > args.max_list:
        print(f"  ... and {len(violations) - args.max_list} more.", file=sys.stderr)

    print("", file=sys.stderr)
    print(
        "Hint: add as line 1 (after optional BOM / blank lines): "
        '`> **Scope:** One sentence: audience, intent, and what this doc is not.`',
        file=sys.stderr,
    )

    return 1


if __name__ == "__main__":
    raise SystemExit(main())
