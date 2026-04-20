"""CI guard: assert at least one ``Published`` reference customer row exists.

Reads the Markdown table in
``docs/go-to-market/reference-customers/README.md`` (single argument) and exits
non-zero when **zero** rows have a ``Status`` cell whose normalized value
equals ``published``.

Today this guard is wired into ``.github/workflows/ci.yml`` with
``continue-on-error: true`` — it surfaces as a **warning** until the first real
reference customer lands. See ``PRICING_PHILOSOPHY.md`` § 5.4 for how the
``-15%`` reference discount is re-rated when this guard flips to blocking.

Usage:
    python scripts/ci/check_reference_customer_status.py docs/go-to-market/reference-customers/README.md

Exit codes:
    0 - at least one row has ``Status: Published``
    1 - zero ``Published`` rows (today's expected outcome)
    2 - invocation / parse error (file missing, no table found, etc.)

Constraints (per the prompt that authored this script):
    * Python 3.11+, no third-party dependencies.
    * Public, unit-testable ``check(rows)`` helper accepts a list of dict-rows.
"""

from __future__ import annotations

import argparse
import pathlib
import sys
from typing import Iterable

PUBLISHED_STATUS_TOKEN: str = "published"
ALLOWED_STATUS_TOKENS: frozenset[str] = frozenset({
    "placeholder",
    "drafting",
    "customer review",
    "published",
})

EXPECTED_HEADER_TOKENS: list[str] = [
    "customer",
    "tier",
    "pilot start",
    "case-study link",
    "reference-call cadence",
    "status",
]


def check(rows: list[dict[str, str]]) -> bool:
    """Return True when at least one row's ``Status`` is ``Published``.

    Pure helper. Each row is a ``{ column_header_lower: cell_text }`` dict.
    The ``Status`` cell is matched case-insensitively against the canonical
    token set; any other token is treated as not-published (the structural
    enforcement of allowed tokens lives at the table-parse layer).
    """

    if rows is None: raise ValueError("rows is None")

    for row in rows:
        if row is None: continue

        status = row.get("status", "").strip().lower()

        # Trim trailing comments such as "Placeholder - replace before publishing"
        # so finance can see why a row is still gated. Anything before the first
        # dash counts as the canonical token.
        canonical = status.split("-", 1)[0].strip()

        if canonical == PUBLISHED_STATUS_TOKEN: return True

    return False


def parse_reference_table(markdown_text: str) -> list[dict[str, str]]:
    """Parse the first GFM table whose header includes every expected column.

    Returns the table rows as a list of header-keyed dicts (header text is
    lowercased and whitespace-collapsed). Raises ``ValueError`` if no
    matching table is found or if a row's column count does not match the
    header.
    """

    if markdown_text is None: raise ValueError("markdown_text is None")

    lines = [line.rstrip() for line in markdown_text.splitlines()]
    table_start = _find_table_header_index(lines)

    if table_start < 0: raise ValueError(
        "no GFM table found whose header contains every expected column: "
        + ", ".join(EXPECTED_HEADER_TOKENS)
    )

    header_cells = _split_table_row(lines[table_start])
    header_keys = [cell.strip().lower() for cell in header_cells]

    rows: list[dict[str, str]] = []

    for line in lines[table_start + 2:]:
        if not line.strip().startswith("|"): break
        if _is_table_separator(line): continue

        cells = _split_table_row(line)
        if len(cells) != len(header_keys): raise ValueError(
            f"row column count {len(cells)} does not match header count {len(header_keys)}: {line}"
        )

        rows.append({key: cell.strip() for key, cell in zip(header_keys, cells)})

    return rows


def _find_table_header_index(lines: list[str]) -> int:
    """Return the index of the first header line that matches every expected column."""

    for index, line in enumerate(lines):
        if not line.strip().startswith("|"): continue
        if index + 1 >= len(lines): continue
        if not _is_table_separator(lines[index + 1]): continue

        header_lower = line.lower()
        if all(token in header_lower for token in EXPECTED_HEADER_TOKENS): return index

    return -1


def _is_table_separator(line: str) -> bool:
    """Return True for the GFM `|---|---|` separator row."""

    stripped = line.strip()
    if not stripped.startswith("|"): return False
    return all(ch in "|-: " for ch in stripped)


def _split_table_row(line: str) -> list[str]:
    """Split a GFM table row on `|`, dropping the empty leading/trailing slots."""

    cells = line.split("|")

    if cells and cells[0].strip() == "": cells = cells[1:]
    if cells and cells[-1].strip() == "": cells = cells[:-1]

    return cells


def main(argv: Iterable[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument(
        "index_path",
        type=pathlib.Path,
        help="Path to docs/go-to-market/reference-customers/README.md (or a fixture in tests).",
    )

    args = parser.parse_args(argv if argv is None else list(argv))

    if not args.index_path.exists():
        print(f"::error::reference-customer index not found: {args.index_path}", file=sys.stderr)
        return 2

    try:
        rows = parse_reference_table(args.index_path.read_text(encoding="utf-8"))
    except ValueError as exc:
        print(f"::error::{exc}", file=sys.stderr)
        return 2

    if check(rows):
        published = [r.get("customer", "(unknown)") for r in rows if r.get("status", "").strip().lower().split("-", 1)[0].strip() == PUBLISHED_STATUS_TOKEN]
        print(f"OK: {len(published)} Published reference customer row(s): {', '.join(published)}.")
        return 0

    print("::warning::no Published reference customer rows yet in {0}.".format(args.index_path))
    print(
        "Reference-customer gate: zero rows with Status: Published. "
        "While this is true, CI treats exit code 1 as a **non-blocking** warning "
        "(see `.github/workflows/ci.yml` step `refcust-warn` with `continue-on-error: true`). "
        "The **next** step `Guard — reference-customer status (auto-flip: strict once any Published row exists)` "
        "runs the **same** script **without** `continue-on-error` **only after** the warn step exits 0 — i.e. once "
        "at least one Published row exists — so regressions become merge-blocking **without** manually editing YAML. "
        "See `docs/go-to-market/PRICING_PHILOSOPHY.md` § 5.4 (discount-stack work-down).",
        file=sys.stderr,
    )
    return 1


if __name__ == "__main__":
    sys.exit(main())
