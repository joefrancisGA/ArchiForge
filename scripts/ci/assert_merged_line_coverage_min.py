#!/usr/bin/env python3
"""
CI guard: merged Cobertura (ReportGenerator output) must meet:
  - minimum merged line coverage (default 70%; CI passes explicit pct; full-regression job currently uses 79),
  - minimum merged branch coverage (default 50%; root branch-rate required; full-regression job currently uses 63),
  - minimum line coverage per product ArchLucid.* package with coverable lines (default 60%; full-regression uses 63, optional skips).

Coverlet runs per test assembly; enforcing <Threshold> in coverage.runsettings would not
represent solution-wide coverage. The full-regression job merges Cobertura files first.

Packages with zero coverable <line/> rows are skipped (no executable lines in Cobertura for
that package). Packages with coverable lines but missing line-rate are treated as failures
(inconsistent report).

Optional `--skip-package-line-gate` (repeatable) omits named product packages from the
per-package line floor (exact Cobertura `package name=` match). Use for thin entrypoints
(for example a jobs CLI host) while keeping the merged line/branch gates.

Edge cases are documented here so behavior stays stable across Coverlet/ReportGenerator versions.
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

_CI_DIR = Path(__file__).resolve().parent
if str(_CI_DIR) not in sys.path:
    sys.path.insert(0, str(_CI_DIR))

from coverage_cobertura import (
    is_product_archlucid_package,
    parse_cobertura,
    product_packages_for_gate,
)


def _failures_for_packages(
    summary,
    min_package_line_pct: float,
    skip_package_line_gate: frozenset[str],
) -> list[str]:
    """Return human-readable failure lines for product packages under the line floor."""
    bad: list[str] = []
    for p in product_packages_for_gate(summary):
        if p.name in skip_package_line_gate:
            continue
        if p.line_rate is None:
            bad.append(
                f"  {p.name}: missing line-rate in Cobertura but coverable_lines={p.coverable_lines} (invalid report).",
            )
            continue
        pct = p.line_rate * 100.0
        if pct + 1e-9 < min_package_line_pct:
            bad.append(f"  {p.name}: line coverage {pct:.2f}% (minimum {min_package_line_pct:.2f}%).")
    return bad


def _advisory_package_warnings(
    summary,
    min_package_line_pct: float,
    warn_below_package_line_pct: float,
    skip_package_line_gate: frozenset[str],
) -> list[str]:
    """
    Packages that pass the merge floor but sit below an advisory ceiling (e.g. at-floor OK, <70% warn).
    """
    if warn_below_package_line_pct <= min_package_line_pct + 1e-9:
        return []

    lines: list[str] = []
    for p in product_packages_for_gate(summary):
        if p.name in skip_package_line_gate:
            continue
        if p.line_rate is None:
            continue
        pct = p.line_rate * 100.0
        if pct + 1e-9 >= min_package_line_pct and pct + 1e-9 < warn_below_package_line_pct:
            lines.append(
                f"Per-package line advisory: {p.name} at {pct:.2f}% "
                f"(merge floor {min_package_line_pct:.0f}%; consider raising toward "
                f"{warn_below_package_line_pct:.0f}%).",
            )
    return lines


def _main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(
        description="Enforce merged Cobertura line, branch, and per-product-package line floors.",
    )
    parser.add_argument(
        "cobertura",
        type=Path,
        help="Path to merged Cobertura.xml (e.g. coverage-report-full/Cobertura.xml).",
    )
    parser.add_argument(
        "min_line_positional",
        nargs="?",
        type=float,
        default=None,
        help="Optional merged line minimum %% (backward compatible); overrides --min-line-pct when set.",
    )
    parser.add_argument(
        "--min-line-pct",
        type=float,
        default=70.0,
        help="Merged line coverage minimum (default 70).",
    )
    parser.add_argument(
        "--min-branch-pct",
        type=float,
        default=50.0,
        help="Merged branch coverage minimum from root branch-rate (default 50).",
    )
    parser.add_argument(
        "--min-package-line-pct",
        type=float,
        default=60.0,
        help="Minimum line %% for each product ArchLucid.* package with coverable lines (default 60).",
    )
    parser.add_argument(
        "--warn-below-package-line-pct",
        type=float,
        default=70.0,
        help=(
            "Emit GitHub ::warning:: (and optional --annotations-file lines) for product packages "
            "with line %% in [min-package-line-pct, this value). Default 70. Set equal to "
            "min-package-line-pct to disable."
        ),
    )
    parser.add_argument(
        "--annotations-file",
        type=Path,
        default=None,
        help="Append one advisory line per row (plain text) for PR workflow warning aggregation.",
    )
    parser.add_argument(
        "--skip-package-line-gate",
        action="append",
        default=[],
        metavar="PACKAGE",
        help=(
            "Exclude this Cobertura package name from the per-product-package line floor "
            "(repeatable). Must match package name= exactly."
        ),
    )
    args = parser.parse_args(argv)

    skip_package_line_gate = frozenset(str(name).strip() for name in args.skip_package_line_gate if str(name).strip())

    line_min = (
        args.min_line_positional
        if args.min_line_positional is not None
        else args.min_line_pct
    )

    summary = parse_cobertura(args.cobertura)
    if summary is None:
        print(f"Could not parse Cobertura: {args.cobertura!r}.", file=sys.stderr)
        return 2

    package_names = {p.name for p in summary.packages}
    for skipped in skip_package_line_gate:
        if skipped not in package_names:
            print(
                f"::warning::skip-package-line-gate {skipped!r} not found in Cobertura packages "
                f"(typo or report shape drift).",
                file=sys.stderr,
            )

    if summary.root_line_pct is None:
        print(f"Missing line-rate on root element in {args.cobertura!r}.", file=sys.stderr)
        return 2

    if summary.root_branch_pct is None:
        print(
            f"Missing branch-rate on root element in {args.cobertura!r} "
            "(merged branch gate requires root branch-rate; do not silently pass).",
            file=sys.stderr,
        )
        return 2

    exit_code = 0

    if summary.root_line_pct + 1e-9 < line_min:
        print(
            f"Merged line coverage {summary.root_line_pct:.2f}% is below required minimum {line_min:.2f}%.",
        )
        exit_code = 1
    else:
        print(
            f"Merged line coverage gate OK: {summary.root_line_pct:.2f}% (minimum {line_min:.2f}%).",
        )

    if summary.root_branch_pct + 1e-9 < args.min_branch_pct:
        print(
            f"Merged branch coverage {summary.root_branch_pct:.2f}% is below required minimum "
            f"{args.min_branch_pct:.2f}%.",
        )
        exit_code = 1
    else:
        print(
            f"Merged branch coverage gate OK: {summary.root_branch_pct:.2f}% "
            f"(minimum {args.min_branch_pct:.2f}%).",
        )

    pkg_failures = _failures_for_packages(summary, args.min_package_line_pct, skip_package_line_gate)
    if pkg_failures:
        print("Per-product-package line coverage failures (ArchLucid.* with coverable lines):")
        for line in pkg_failures:
            print(line)
        exit_code = 1
    else:
        product = [p for p in summary.packages if is_product_archlucid_package(p.name)]
        gated = product_packages_for_gate(summary)
        evaluated = [p for p in gated if p.name not in skip_package_line_gate]
        skip_note = ""
        if skip_package_line_gate:
            skip_note = (
                f" Skipped line floor for: {', '.join(sorted(skip_package_line_gate))}."
            )
        print(
            f"Per-package line gate OK: {len(evaluated)} evaluated product package(s) with coverable lines "
            f"all >= {args.min_package_line_pct:.2f}% "
            f"({len(gated)} gated total, {len(product)} ArchLucid.* product package nodes).{skip_note}",
        )

    advisory = _advisory_package_warnings(
        summary,
        args.min_package_line_pct,
        args.warn_below_package_line_pct,
        skip_package_line_gate,
    )
    if advisory:
        for w in advisory:
            print(w)
        if args.annotations_file is not None:
            args.annotations_file.parent.mkdir(parents=True, exist_ok=True)
            existing = ""
            if args.annotations_file.is_file():
                existing = args.annotations_file.read_text(encoding="utf-8", errors="replace")
            block = "\n".join(advisory) + ("\n" if advisory else "")
            args.annotations_file.write_text(existing + block, encoding="utf-8")
    elif args.annotations_file is not None:
        args.annotations_file.parent.mkdir(parents=True, exist_ok=True)
        if not args.annotations_file.is_file():
            args.annotations_file.write_text("", encoding="utf-8")

    return exit_code


if __name__ == "__main__":
    raise SystemExit(_main(sys.argv[1:]))
