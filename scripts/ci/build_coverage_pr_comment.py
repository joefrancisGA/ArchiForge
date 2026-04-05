#!/usr/bin/env python3
"""Build markdown for sticky PR comment from full-regression coverage metrics + optional merged Cobertura."""

from __future__ import annotations

import os
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

# Informational gates (do not fail the job): warn in the PR comment + workflow annotations.
OVERALL_LINE_WARN_PCT = 70.0
PER_PROJECT_LINE_WARN_PCT = 50.0


def _local_name(tag: str) -> str:
    if "}" in tag:
        return tag.split("}", 1)[1]
    return tag


def parse_metrics(path: Path) -> tuple[float | None, float | None]:
    if not path.is_file():
        return None, None

    text = path.read_text(encoding="utf-8", errors="replace")
    line_m = re.search(r"Line coverage:\s*([\d.]+)\s*%", text, re.IGNORECASE)
    branch_m = re.search(r"Branch coverage:\s*([\d.]+)\s*%", text, re.IGNORECASE)
    line_pct = float(line_m.group(1)) if line_m else None
    branch_pct = float(branch_m.group(1)) if branch_m else None

    return line_pct, branch_pct


def parse_cobertura_packages(path: Path) -> tuple[float | None, list[tuple[str, float]]]:
    """Return (overall_line_pct, [(package_name, line_pct), ...]) from merged Cobertura."""
    if not path.is_file():
        return None, []

    try:
        tree = ET.parse(path)
    except ET.ParseError:
        return None, []

    root = tree.getroot()
    if root is None:
        return None, []

    overall_raw = root.get("line-rate")
    overall_pct = float(overall_raw) * 100.0 if overall_raw is not None else None

    packages: list[tuple[str, float]] = []
    for element in root.iter():
        if _local_name(element.tag) != "package":
            continue
        name = (element.get("name") or "").strip()
        rate_raw = element.get("line-rate")
        if not name or rate_raw is None:
            continue
        try:
            packages.append((name, float(rate_raw) * 100.0))
        except ValueError:
            continue

    packages.sort(key=lambda x: x[0])
    return overall_pct, packages


def _is_product_archiforge_package(name: str) -> bool:
    if not name.startswith("ArchiForge."):
        return False
    lower = name.lower()
    if ".tests" in lower or name.endswith("Tests"):
        return False
    if "tests." in lower or ".testsupport" in lower or "TestSupport" in name:
        return False
    return True


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

    cob_overall, cob_packages = parse_cobertura_packages(cobertura_file)
    product_rows = [(n, p) for n, p in cob_packages if _is_product_archiforge_package(n)]
    low_projects = sorted(
        [(n, p) for n, p in product_rows if p < PER_PROJECT_LINE_WARN_PCT],
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
            f"⚠️ **Projects under {PER_PROJECT_LINE_WARN_PCT:.0f}% line coverage** (informational; consider tests or exclusions review):",
        )
        lines.append("")
        lines.append("| Assembly / package | Line % |")
        lines.append("| --- | --- |")
        for name, pct in low_projects:
            lines.append(f"| `{name}` | {pct:.1f} |")
        lines.append("")
        for name, pct in low_projects[:10]:
            annotation_lines.append(
                f"Low coverage: {name} at {pct:.1f}% line (threshold {PER_PROJECT_LINE_WARN_PCT:.0f}%).",
            )

    if cobertura_file.is_file() and not product_rows and cob_packages:
        lines.append(
            "_Per-project table skipped: Cobertura packages did not match `ArchiForge.*` product filters._",
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
