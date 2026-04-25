"""CI guard: front-door /why table row labels match COMPETITIVE_LANDSCAPE.md.

Compares the first column of the markdown table under
``## Hard comparison table (front-door)`` with
``WHY_COMPARISON_TABLE_ROW_LABELS_IN_ORDER`` in
``archlucid-ui/src/lib/why-comparison.ts`` (same strings, same order).

Usage:
    python scripts/ci/check_why_table_alignment.py
    python scripts/ci/check_why_table_alignment.py --repo-root /path/to/repo

Exit codes:
    0 — labels match
    1 — mismatch or parse failure
    2 — missing files / invocation error
"""

from __future__ import annotations

import argparse
import pathlib
import re
import sys
from typing import List

MD_RELATIVE: str = "docs/go-to-market/COMPETITIVE_LANDSCAPE.md"
TS_RELATIVE: str = "archlucid-ui/src/lib/why-comparison.ts"
SECTION_HEADING: str = "## Hard comparison table (front-door)"


def _read_text(path: pathlib.Path) -> str:
    if not path.exists():
        raise FileNotFoundError(f"Required file not found: {path}")

    return path.read_text(encoding="utf-8")


def _extract_markdown_table_first_column_labels(md_text: str) -> List[str]:
    try:
        start = md_text.index(SECTION_HEADING)
    except ValueError as exc:
        raise ValueError(f"Section not found: {SECTION_HEADING}") from exc

    tail = md_text[start + len(SECTION_HEADING) :]
    next_h2 = re.search(r"\n## ", tail)
    section_end = next_h2.start() if next_h2 else len(tail)
    section_body = tail[:section_end]

    lines = [ln.rstrip() for ln in section_body.splitlines() if ln.strip().startswith("|")]
    if len(lines) < 3:
        raise ValueError("Expected at least header, separator, and one data row in markdown table.")

    # Skip header row (line 0) and separator (line 1, contains ---).
    data_lines = [ln for ln in lines[2:] if not re.match(r"^\|\s*:?-{3,}", ln.strip())]

    labels: List[str] = []
    for line in data_lines:
        cells = [c.strip() for c in line.strip().strip("|").split("|")]
        if not cells:
            continue

        labels.append(cells[0])

    if not labels:
        raise ValueError("No data rows parsed from markdown table.")

    return labels


def _extract_ts_ordered_labels(ts_text: str) -> List[str]:
    match = re.search(
        r"export const WHY_COMPARISON_TABLE_ROW_LABELS_IN_ORDER\s*=\s*\[(?P<body>[\s\S]*?)\]\s*as const",
        ts_text,
    )
    if match is None:
        raise ValueError("Could not find WHY_COMPARISON_TABLE_ROW_LABELS_IN_ORDER array in why-comparison.ts.")

    body = match.group("body")
    return re.findall(r'"((?:[^"\\]|\\.)*)"', body)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--repo-root",
        type=pathlib.Path,
        default=pathlib.Path.cwd(),
        help="Repository root (default: current working directory).",
    )
    args = parser.parse_args()
    repo_root: pathlib.Path = args.repo_root.resolve()

    md_path = repo_root / MD_RELATIVE
    ts_path = repo_root / TS_RELATIVE

    try:
        md_labels = _extract_markdown_table_first_column_labels(_read_text(md_path))
        ts_labels = _extract_ts_ordered_labels(_read_text(ts_path))
    except (FileNotFoundError, ValueError) as exc:
        print(f"check_why_table_alignment: {exc}", file=sys.stderr)
        return 2

    if md_labels != ts_labels:
        print("check_why_table_alignment: row label lists differ.", file=sys.stderr)
        print(f"markdown ({len(md_labels)}): {md_labels}", file=sys.stderr)
        print(f"typescript ({len(ts_labels)}): {ts_labels}", file=sys.stderr)
        return 1

    print(f"check_why_table_alignment: OK ({len(md_labels)} rows).")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
