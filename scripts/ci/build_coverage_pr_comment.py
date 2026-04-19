#!/usr/bin/env python3
"""Build markdown for sticky PR comment from full-regression coverage metrics + optional merged Cobertura."""

from __future__ import annotations

import os
import re
import sys
from pathlib import Path

_CI_DIR = Path(__file__).resolve().parent
if str(_CI_DIR) not in sys.path:
    sys.path.insert(0, str(_CI_DIR))

from coverage_cobertura import (
    is_product_archlucid_package,
    parse_cobertura,
    parse_cobertura_packages_simple,
)

# Informational: merged overall line below 70% — warn in the PR comment (merged line gate is still enforced by CI).
OVERALL_LINE_WARN_PCT = 70.0
# Same floor as `assert_merged_line_coverage_min.py --min-package-line-pct` (per-product-package merge gate).
PER_PROJECT_LINE_WARN_PCT = 63.0
# Omit from PR "under floor" table when CI uses `--skip-package-line-gate` for that package name (none today).
PER_PROJECT_LINE_GATE_SKIP_IN_PR_COMMENT = frozenset()


def parse_metrics(path: Path) -> tuple[float | None, float | None]:
    if not path.is_file():
        return None, None

    text = path.read_text(encoding="utf-8", errors="replace")
    line_m = re.search(r"Line coverage:\s*([\d.]+)\s*%", text, re.IGNORECASE)
    branch_m = re.search(r"Branch coverage:\s*([\d.]+)\s*%", text, re.IGNORECASE)
    line_pct = float(line_m.group(1)) if line_m else None
    branch_pct = float(branch_m.group(1)) if branch_m else None

    return line_pct, branch_pct


def format_delta(current: float | None, previous: float | None) -> str:
    if current is None:
        return ""

    if previous is None:
        return ""

    delta = current - previous
    if abs(delta) < 0.01:
        return " (±0.0 pp vs base)"

    sign = "+" if delta > 0 else ""
    return f" ({sign}{delta:.1f} pp vs base)"


def append_github_output(name: str, value: str) -> None:
    out_path = os.environ.get("GITHUB_OUTPUT")
    if not out_path:
        print(value, file=sys.stderr)
        return

    with open(out_path, "a", encoding="utf-8") as handle:
        handle.write(f"{name}<<EOF\n{value}\nEOF\n")


def write_annotation_lines(path: Path, lines: list[str]) -> None:
    if not lines:
        if path.is_file():
            path.unlink()
        return

    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text("\n".join(lines[:25]) + ("\n" if lines else ""), encoding="utf-8")


def main() -> int:
    run_url = os.environ.get("RUN_URL", "").strip()
    base_ref = os.environ.get("BASE_REF", "").strip()
    head_file = Path(os.environ.get("HEAD_FILE", "head-metrics/coverage-metrics.txt"))
    base_file = Path(os.environ.get("BASE_FILE", "base-metrics/coverage-metrics.txt"))
    cobertura_file = Path(os.environ.get("COBERTURA_FILE", "head-cobertura/Cobertura.xml"))
    annotations_file = Path(os.environ.get("ANNOTATIONS_FILE", "head-metrics/coverage-annotations.txt"))

    head_line, head_branch = parse_metrics(head_file)
    base_line, base_branch = parse_metrics(base_file)

    if head_line is None and head_branch is None:
        append_github_output("body", "_Could not read coverage metrics for this PR._")
        write_annotation_lines(annotations_file, [])
        return 0

    cob_overall, cob_packages = parse_cobertura_packages_simple(cobertura_file)
    product_rows = [(n, p) for n, p in cob_packages if is_product_archlucid_package(n)]
    low_projects = sorted(
        [
            (n, p)
            for n, p in product_rows
            if p < PER_PROJECT_LINE_WARN_PCT and n not in PER_PROJECT_LINE_GATE_SKIP_IN_PR_COMMENT
        ],
        key=lambda x: x[1],
    )

    lines: list[str] = [
        "### Code coverage (full .NET regression)",
        "",
        "| Metric | This PR |",
        "| --- | --- |",
    ]

    if head_line is not None:
        lines.append(
            f"| Line | **{head_line:.1f}%**{format_delta(head_line, base_line)} |",
        )

    if head_branch is not None:
        lines.append(
            f"| Branch | **{head_branch:.1f}%**{format_delta(head_branch, base_branch)} |",
        )

    cob_summary = parse_cobertura(cobertura_file)
    if cob_summary is not None and cob_summary.root_branch_pct is not None:
        lines.append(
            f"| Branch (merged Cobertura root) | **{cob_summary.root_branch_pct:.1f}%** |",
        )

    lines.append("")

    annotation_lines: list[str] = []

    effective_overall = cob_overall if cob_overall is not None else head_line
    if effective_overall is not None and effective_overall < OVERALL_LINE_WARN_PCT:
        msg = (
            f"Overall line coverage {effective_overall:.1f}% is below informational threshold "
            f"{OVERALL_LINE_WARN_PCT:.0f}% (not a merge blocker)."
        )
        lines.append(f"⚠️ **Coverage gate (warning):** {msg}")
        lines.append("")
        annotation_lines.append(msg)

    if low_projects:
        lines.append(
            f"**CI gate — per-package line floor** — projects under **{PER_PROJECT_LINE_WARN_PCT:.0f}%** "
            f"(`assert_merged_line_coverage_min.py` on merged Cobertura; merge blocked):",
        )
        lines.append("")
        lines.append("| Assembly / package | Line % |")
        lines.append("| --- | --- |")
        for name, pct in low_projects:
            lines.append(f"| `{name}` | {pct:.1f} |")
        lines.append("")
        for name, pct in low_projects[:10]:
            annotation_lines.append(
                f"Per-package line gate: {name} at {pct:.1f}% line (minimum {PER_PROJECT_LINE_WARN_PCT:.0f}%).",
            )

    if cobertura_file.is_file() and not product_rows and cob_packages:
        lines.append(
            "_Per-project table skipped: Cobertura packages did not match `ArchLucid.*` product filters._",
        )
        lines.append("")
    elif not cobertura_file.is_file():
        lines.append(
            "_Per-project coverage table unavailable (merged Cobertura artifact missing)._",
        )
        lines.append("")

    if base_line is None and base_branch is None and base_ref:
        lines.append(
            f"_No cached metrics for base `{base_ref}` yet (merge a green run to `main`/`master` to enable deltas)._",
        )
        lines.append("")

    if run_url:
        lines.append(f"[View workflow run]({run_url})")

    append_github_output("body", "\n".join(lines))
    write_annotation_lines(annotations_file, annotation_lines)
    return 0


if __name__ == "__main__":
    sys.exit(main())
