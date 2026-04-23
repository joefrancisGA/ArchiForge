"""CI guard: keep the ``/why`` inline comparison rows and the PDF builder rows in sync.

The differentiation rows on the public ``/why`` marketing page are rendered from
``archlucid-ui/src/marketing/why-archlucid-comparison.ts`` (TypeScript), and the same rows
are emitted into the side-by-side PDF pack by
``ArchLucid.Application/Pilots/WhyArchLucidPackBuilder.cs`` (C#). The two source files are
hand-maintained — this script fails CI if they drift.

Decision: **PENDING_QUESTIONS.md item 31**, resolved 2026-04-21 (ship both PDF and inline
section, with a CI sync check).

The check is intentionally a **content** comparison, not a tokenized AST diff. It extracts
five fields per row from each file (``claim``, ``archlucidEvidence``, ``competitorBaseline``,
``citation``, ``narrativeParagraph``) and asserts equality after light normalization
(collapse whitespace, decode common JSON / C-string escapes).

Usage:
    python scripts/ci/check_why_archlucid_comparison_sync.py
    python scripts/ci/check_why_archlucid_comparison_sync.py --repo-root /path/to/repo

Exit codes:
    0 — rows match
    1 — rows diverge (unified diff printed)
    2 — invocation error or one of the source files is missing / unparseable
"""

from __future__ import annotations

import argparse
import difflib
import pathlib
import re
import sys
from typing import List, Tuple

TS_RELATIVE: str = "archlucid-ui/src/marketing/why-archlucid-comparison.ts"
CS_RELATIVE: str = "ArchLucid.Application/Pilots/WhyArchLucidPackBuilder.cs"

ROW_FIELDS: Tuple[str, ...] = (
    "claim",
    "archlucidEvidence",
    "competitorBaseline",
    "citation",
    "narrativeParagraph",
)


def _read_text(path: pathlib.Path) -> str:
    """Read UTF-8 text; surface a clear error for the operator if the file is missing."""
    if not path.exists():
        raise FileNotFoundError(f"Required source file not found: {path}")

    return path.read_text(encoding="utf-8")


def _normalize(value: str) -> str:
    """Collapse whitespace and strip surrounding quotes for stable equality checks."""
    return " ".join(value.split()).strip()


def _decode_string_literal(literal: str) -> str:
    """Decode the most common C-style escapes appearing in our row strings.

    The two source files use different escape conventions (TS uses ``\\u00a7`` for
    section signs, C# uses literal ``§``). We decode the common cases so equality
    checks succeed when the *rendered* strings match.
    """
    # Strip surrounding quotes if present.
    if len(literal) >= 2 and literal[0] in ('"', "'") and literal[-1] == literal[0]:
        literal = literal[1:-1]

    decoded = literal
    decoded = re.sub(r"\\u([0-9a-fA-F]{4})", lambda m: chr(int(m.group(1), 16)), decoded)
    decoded = decoded.replace('\\"', '"').replace("\\'", "'").replace("\\\\", "\\")
    decoded = decoded.replace("\\|", "|")

    return _normalize(decoded)


def _extract_ts_rows(ts_text: str) -> List[Tuple[str, ...]]:
    """Pull the literal initializer of ``WHY_ARCHLUCID_COMPARISON_ROWS`` and parse it.

    The TS file declares::

        export const WHY_ARCHLUCID_COMPARISON_ROWS: readonly WhyArchlucidComparisonRow[] = [
            { claim: "...", archlucidEvidence: "...", ... },
            ...
        ];

    Each row is a single object literal. We extract every ``{ ... }`` block inside the
    array and read the named fields from each.
    """
    array_match = re.search(
        r"WHY_ARCHLUCID_COMPARISON_ROWS\s*:[^=]*=\s*\[(?P<body>.*?)\];",
        ts_text,
        re.DOTALL,
    )

    if array_match is None:
        raise ValueError(
            f"Could not locate WHY_ARCHLUCID_COMPARISON_ROWS array in {TS_RELATIVE}."
        )

    body = array_match.group("body")
    rows: List[Tuple[str, ...]] = []
    object_pattern = re.compile(r"\{(?P<obj>[^{}]+(?:\{[^}]*\}[^{}]*)*)\}", re.DOTALL)

    for object_match in object_pattern.finditer(body):
        object_text = object_match.group("obj")
        row: List[str] = []

        for field in ROW_FIELDS:
            field_match = re.search(
                rf"{field}\s*:\s*(?P<value>\"(?:[^\"\\]|\\.)*\")",
                object_text,
                re.DOTALL,
            )

            if field_match is None:
                raise ValueError(
                    f"Field '{field}' missing from a row in {TS_RELATIVE}: {object_text[:120]}..."
                )

            row.append(_decode_string_literal(field_match.group("value")))

        rows.append(tuple(row))

    if not rows:
        raise ValueError(f"No rows extracted from {TS_RELATIVE}.")

    return rows


def _extract_cs_rows(cs_text: str) -> List[Tuple[str, ...]]:
    """Pull the tuple-array literal inside ``BuildDifferentiationMarkdownTable`` and parse each row.

    Each tuple is five C# string literals in the order
    ``(Claim, ArchlucidEvidence, CompetitorBaseline, Citation, Narrative)``.
    """
    method_match = re.search(
        r"BuildDifferentiationMarkdownTable\s*\(\)\s*\{(?P<body>.*?)return\s+t\.ToString",
        cs_text,
        re.DOTALL,
    )

    if method_match is None:
        raise ValueError(
            f"Could not locate BuildDifferentiationMarkdownTable in {CS_RELATIVE}."
        )

    body = method_match.group("body")
    # Anchor on ``= [`` (the ``[]`` in the tuple-element type declaration must not match).
    array_match = re.search(r"=\s*\[\s*(?P<rows>.*?)\s*\];", body, re.DOTALL)

    if array_match is None:
        raise ValueError(
            f"Could not locate the tuple-array literal in {CS_RELATIVE} BuildDifferentiationMarkdownTable body."
        )

    rows_text = array_match.group("rows")
    tuple_blocks: List[str] = _extract_top_level_paren_blocks(rows_text)
    rows: List[Tuple[str, ...]] = []

    for block in tuple_blocks:
        literals = re.findall(r"\"(?:[^\"\\]|\\.)*\"", block, re.DOTALL)

        if len(literals) != len(ROW_FIELDS):
            # Tuples that aren't the row tuple (e.g. nested calls) produce a different count;
            # skip them silently.
            continue

        row = tuple(_decode_string_literal(literal) for literal in literals)
        rows.append(row)

    if not rows:
        raise ValueError(f"No rows extracted from {CS_RELATIVE}.")

    return rows


def _extract_top_level_paren_blocks(text: str) -> List[str]:
    """Return every top-level ``( ... )`` block, ignoring parens inside C# string literals.

    A naive ``re.findall(r"\\([^()]+\\)", ...)`` is fooled by row strings that contain
    parens (for example ``"Multi-agent pipeline (Topology, ...)"``). This walker tracks
    string literals so we only count parens that are part of C# syntax.
    """
    blocks: List[str] = []
    depth: int = 0
    start: int = -1
    in_string: bool = False
    in_verbatim: bool = False
    escape: bool = False

    for index, ch in enumerate(text):
        if in_string:
            if in_verbatim:
                if ch == '"':
                    if index + 1 < len(text) and text[index + 1] == '"':
                        # ``""`` is a literal quote inside a verbatim string; skip the next char.
                        escape = True
                        continue

                    in_string = False
                    in_verbatim = False
                continue

            if escape:
                escape = False
                continue

            if ch == "\\":
                escape = True
                continue

            if ch == '"':
                in_string = False

            continue

        if ch == '"':
            in_string = True
            in_verbatim = index > 0 and text[index - 1] == "@"
            continue

        if ch == "(":
            if depth == 0:
                start = index + 1
            depth += 1
            continue

        if ch == ")":
            depth -= 1

            if depth == 0 and start >= 0:
                blocks.append(text[start:index])
                start = -1

    return blocks


def _format_rows_for_diff(rows: List[Tuple[str, ...]]) -> List[str]:
    """One line per row field, ordered, so difflib produces a readable diff on mismatch."""
    lines: List[str] = []

    for index, row in enumerate(rows):
        lines.append(f"--- row {index} ---")

        for field, value in zip(ROW_FIELDS, row):
            lines.append(f"{field}: {value}")

    return lines


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument(
        "--repo-root",
        type=pathlib.Path,
        default=pathlib.Path(__file__).resolve().parents[2],
        help="Repository root (defaults to two parents above this script).",
    )
    args = parser.parse_args()

    repo_root: pathlib.Path = args.repo_root.resolve()

    if not repo_root.is_dir():
        print(f"error: --repo-root is not a directory: {repo_root}", file=sys.stderr)

        return 2

    ts_path: pathlib.Path = repo_root / TS_RELATIVE
    cs_path: pathlib.Path = repo_root / CS_RELATIVE

    try:
        ts_rows = _extract_ts_rows(_read_text(ts_path))
        cs_rows = _extract_cs_rows(_read_text(cs_path))
    except (FileNotFoundError, ValueError) as exc:
        print(f"error: {exc}", file=sys.stderr)

        return 2

    if ts_rows == cs_rows:
        print(
            f"OK: {len(ts_rows)} comparison rows match between {TS_RELATIVE} and {CS_RELATIVE}.",
        )

        return 0

    print(
        "FAIL: /why inline comparison rows and the PDF builder rows are out of sync.\n"
        f"  TS source: {TS_RELATIVE}\n"
        f"  C# source: {CS_RELATIVE}\n"
        "  Decision: PENDING_QUESTIONS.md item 31 (resolved 2026-04-21) requires both surfaces to render the\n"
        "  same five differentiation rows. Update both files in the same commit.",
        file=sys.stderr,
    )

    diff = difflib.unified_diff(
        _format_rows_for_diff(ts_rows),
        _format_rows_for_diff(cs_rows),
        fromfile=TS_RELATIVE,
        tofile=CS_RELATIVE,
        lineterm="",
    )

    for line in diff:
        print(line, file=sys.stderr)

    return 1


if __name__ == "__main__":
    sys.exit(main())
