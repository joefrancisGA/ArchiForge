#!/usr/bin/env python3
"""Cross-check ``AuditEventTypes.cs`` ``public const string`` keys vs ``AUDIT_COVERAGE_MATRIX.md`` appendices + marker."""

from __future__ import annotations

import argparse
import re
import sys
from collections import Counter
from pathlib import Path

_MARKER_RE = re.compile(r"<!--\s*audit-core-const-count:(\d+)\s*-->")
_CONST_RE = re.compile(r"^\s*public const string (\w+)\s*=")
_FIRST_CELL_RE = re.compile(r"^\|\s*`([^`]+)`\s*\|")

_APPENDIX_CORE = "## Appendix — Core"
_APPENDIX_RUN = "## Appendix — `AuditEventTypes.Run`"
_APPENDIX_BASELINE = "## Appendix — `AuditEventTypes.Baseline`"


def repo_root() -> Path:
    return Path(__file__).resolve().parents[2]


def read_marker(matrix_text: str) -> int | None:
    match = _MARKER_RE.search(matrix_text)
    if match is None:
        return None

    return int(match.group(1))


def parse_audit_event_type_keys(lines: list[str]) -> list[str]:
    """Build canonical matrix keys from ``AuditEventTypes.cs`` lines (same naming as appendix tables)."""

    def find_line(predicate: str, start: int = 0) -> int:
        for i in range(start, len(lines)):
            if predicate in lines[i]:
                return i

        raise ValueError(f"assert_audit_const_count: anchor not found: {predicate!r} (from line {start})")

    idx_run = find_line("public static class Run")
    idx_baseline = find_line("public static class Baseline")
    idx_arch = find_line("public static class Architecture", start=idx_baseline)
    idx_gov = find_line("public static class Governance", start=idx_baseline)
    keys: list[str] = []
    for i, line in enumerate(lines):
        match = _CONST_RE.match(line)
        if match is None:
            continue

        name = match.group(1)
        if i < idx_run:
            keys.append(name)
            continue

        if i < idx_baseline:
            keys.append("Run." + name)
            continue

        if idx_arch <= i < idx_gov:
            keys.append("Baseline.Architecture." + name)
            continue

        if i >= idx_gov:
            keys.append("Baseline.Governance." + name)
            continue

    if len(keys) == 0:
        raise ValueError("assert_audit_const_count: no public const string entries parsed")

    return keys


def _slice_appendix(matrix_text: str, start_marker: str, end_marker: str | None) -> str:
    start = matrix_text.find(start_marker)
    if start < 0:
        raise ValueError(f"assert_audit_const_count: missing appendix section {start_marker!r}")

    nl = matrix_text.find("\n", start)
    if nl < 0:
        return ""

    body_start = nl + 1
    if end_marker is None:
        return matrix_text[body_start:]

    end = matrix_text.find(end_marker, body_start)
    if end < 0:
        raise ValueError(f"assert_audit_const_count: missing appendix end {end_marker!r}")

    return matrix_text[body_start:end]


def parse_matrix_registry_names(matrix_text: str) -> list[str]:
    """First-column backtick names from Core, Run, and Baseline appendix tables."""

    core = _slice_appendix(matrix_text, _APPENDIX_CORE, _APPENDIX_RUN)
    run = _slice_appendix(matrix_text, _APPENDIX_RUN, _APPENDIX_BASELINE)
    baseline_end = "\nWhen adding a `Baseline` constant"
    baseline = _slice_appendix(matrix_text, _APPENDIX_BASELINE, baseline_end)
    combined = core + "\n" + run + "\n" + baseline
    names: list[str] = []
    for line in combined.splitlines():
        stripped = line.strip()
        if not stripped.startswith("| `"):
            continue

        if stripped.startswith("|---"):
            continue

        match = _FIRST_CELL_RE.match(stripped)
        if match is None:
            continue

        cell = match.group(1).strip()
        if cell in ("Constant", "Constant path"):
            continue

        names.append(cell)

    return names


def build_errors(source_keys: list[str], matrix_names: list[str], marker: int | None) -> list[str]:
    errors: list[str] = []
    if marker is None:
        errors.append("MARKER_MISSING: expected <!-- audit-core-const-count:N --> in AUDIT_COVERAGE_MATRIX.md")
        return errors

    source_set = set(source_keys)
    if len(source_keys) != len(source_set):
        dupes = sorted({k for k in source_set if source_keys.count(k) > 1})
        errors.append("DUPLICATE_SOURCE_KEYS: " + ", ".join(dupes))
        return errors

    counts = Counter(matrix_names)
    dup_matrix = sorted([k for k, c in counts.items() if c > 1])
    if dup_matrix:
        errors.append("DUPLICATE_MATRIX_ROWS: " + ", ".join(f"{k} x{counts[k]}" for k in dup_matrix))

    matrix_set = set(matrix_names)
    if marker != len(source_keys):
        errors.append(f"MARKER_MISMATCH: marker={marker} source_const_count={len(source_keys)}")

    if len(matrix_names) != len(source_keys):
        errors.append(
            f"ROW_COUNT_MISMATCH: appendix_rows={len(matrix_names)} source_const_count={len(source_keys)}"
        )

    missing = sorted(source_set - matrix_set)
    extra = sorted(matrix_set - source_set)
    if missing:
        errors.append("MISSING_IN_MATRIX (add appendix row or remove stale source):\n  " + "\n  ".join(missing))

    if extra:
        errors.append("EXTRA_IN_MATRIX (remove row or add source constant):\n  " + "\n  ".join(extra))

    return errors


def emit_github_errors(messages: list[str]) -> None:
    for msg in messages:
        safe = msg.replace("\n", "%0A")
        print(f"::error title=audit-matrix::{safe}", file=sys.stderr)


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo-root", type=Path, default=repo_root(), help="Repository root.")
    parser.add_argument(
        "--audit-types",
        type=Path,
        default=None,
        help="Override path to AuditEventTypes.cs (default: ArchLucid.Core/Audit/AuditEventTypes.cs).",
    )
    parser.add_argument(
        "--matrix",
        type=Path,
        default=None,
        help="Override path to AUDIT_COVERAGE_MATRIX.md (default: docs/library/AUDIT_COVERAGE_MATRIX.md).",
    )
    args = parser.parse_args(argv)
    root: Path = args.repo_root.resolve()
    audit_path = (
        args.audit_types if args.audit_types is not None else root / "ArchLucid.Core" / "Audit" / "AuditEventTypes.cs"
    ).resolve()
    matrix_path = (
        args.matrix if args.matrix is not None else root / "docs" / "library" / "AUDIT_COVERAGE_MATRIX.md"
    ).resolve()

    if not audit_path.is_file():
        print(f"assert_audit_const_count: missing {audit_path}", file=sys.stderr)
        return 1

    if not matrix_path.is_file():
        print(f"assert_audit_const_count: missing {matrix_path}", file=sys.stderr)
        return 1

    cs_lines = audit_path.read_text(encoding="utf-8", errors="strict").splitlines()
    matrix_text = matrix_path.read_text(encoding="utf-8", errors="strict")
    marker = read_marker(matrix_text)
    try:
        source_keys = parse_audit_event_type_keys(cs_lines)
        matrix_names = parse_matrix_registry_names(matrix_text)
    except ValueError as ex:
        print(f"assert_audit_const_count: {ex}", file=sys.stderr)
        return 1

    errors = build_errors(source_keys, matrix_names, marker)
    if errors:
        emit_github_errors(errors)
        print("assert_audit_const_count: FAILED —", file=sys.stderr)
        for block in errors:
            print(block, file=sys.stderr)

        return 1

    print(
        f"assert_audit_const_count: OK ({len(source_keys)} const(s); marker={marker}; "
        f"{matrix_path.relative_to(root)} rows match {audit_path.relative_to(root)})."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
