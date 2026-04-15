#!/usr/bin/env python3
"""Emit docs/COVERAGE_GAP_ANALYSIS.md from a merged Cobertura.xml (ReportGenerator output)."""

from __future__ import annotations

import sys
from collections import defaultdict
from pathlib import Path
import xml.etree.ElementTree as ET

_CI_DIR = Path(__file__).resolve().parent
if str(_CI_DIR) not in sys.path:
    sys.path.insert(0, str(_CI_DIR))

from coverage_cobertura import is_product_archlucid_package


def local_name(tag: str) -> str:
    if "}" in tag:
        return tag.split("}", 1)[1]
    return tag


def is_target_product_package(name: str) -> bool:
    if not is_product_archlucid_package(name):
        return False
    if "Benchmark" in name:
        return False
    return True


def uncovered_by_file(package_el: ET.Element) -> dict[str, int]:
    counts: dict[str, int] = defaultdict(int)
    for element in package_el.iter():
        if local_name(element.tag) != "class":
            continue
        fn = (element.get("filename") or "").strip()
        if not fn:
            continue
        for child in element:
            if local_name(child.tag) != "lines":
                continue
            for line in child:
                if local_name(line.tag) != "line":
                    continue
                hits = line.get("hits")
                if hits is None:
                    continue
                try:
                    if int(hits) == 0:
                        counts[fn] += 1
                except ValueError:
                    pass
    return dict(counts)


def main() -> int:
    repo = Path(__file__).resolve().parents[2]
    cobertura = repo / "coverage-gap-1a" / "merged" / "Cobertura.xml"
    if not cobertura.is_file():
        print(f"Missing merged Cobertura: {cobertura}", file=sys.stderr)
        return 2

    tree = ET.parse(cobertura)
    root = tree.getroot()
    if root is None:
        return 3

    packages: list[tuple[str, float, int, dict[str, int]]] = []
    for pkg in root.iter():
        if local_name(pkg.tag) != "package":
            continue
        name = (pkg.get("name") or "").strip()
        if not name or not is_target_product_package(name):
            continue
        lr = pkg.get("line-rate")
        if lr is None:
            continue
        line_rate = float(lr)
        by_file = uncovered_by_file(pkg)
        # Total coverable lines: count all line elements with hits
        total_lines = 0
        for element in pkg.iter():
            if local_name(element.tag) != "line":
                continue
            if element.get("number") is not None:
                total_lines += 1
        if total_lines == 0:
            continue
        packages.append((name, line_rate, total_lines, by_file))

    packages.sort(key=lambda t: t[1])

    out_lines: list[str] = [
        "# Coverage gap analysis (merged Cobertura)",
        "",
        f"**Generated:** from `{cobertura.relative_to(repo)}` (full `ArchLucid.sln` test run + ReportGenerator merge).",
        "",
        "**Scope:** Production `ArchLucid.*` assemblies only; excludes `*.Tests`, TestSupport, and Benchmarks.",
        "",
        "## Bottom five assemblies by line coverage",
        "",
        "| Assembly | Line coverage % | Coverable lines (approx.) |",
        "|----------|-----------------|---------------------------|",
    ]

    bottom5 = packages[:5]
    for name, lr, total_lines, by_file in bottom5:
        out_lines.append(f"| {name} | {lr * 100.0:.2f} | {total_lines} |")

    out_lines.extend(
        [
            "",
            "## Files with most uncovered lines (top three per assembly above)",
            "",
        ]
    )

    for name, lr, total_lines, by_file in bottom5:
        out_lines.append(f"### {name} ({lr * 100.0:.2f}% line coverage)")
        out_lines.append("")
        if not by_file:
            out_lines.append("_No uncovered line rows in Cobertura for this package (or only branches uncovered)._")
            out_lines.append("")
            continue
        ranked = sorted(by_file.items(), key=lambda kv: kv[1], reverse=True)[:3]
        out_lines.append("| Rank | File | Uncovered line entries |")
        out_lines.append("|------|------|------------------------|")
        for i, (path, n) in enumerate(ranked, start=1):
            rel = Path(path)
            try:
                rel = rel.relative_to(repo)
            except ValueError:
                rel = Path(path)
            out_lines.append(f"| {i} | `{rel}` | {n} |")
        out_lines.append("")

    out_lines.extend(
        [
            "## Merged totals (reference)",
            "",
        ]
    )
    line_raw = root.get("line-rate")
    branch_raw = root.get("branch-rate")
    if line_raw:
        out_lines.append(f"- **Merged line coverage:** {float(line_raw) * 100.0:.2f}%")
    if branch_raw:
        out_lines.append(f"- **Merged branch coverage:** {float(branch_raw) * 100.0:.2f}%")
    out_lines.append("")
    recent_path = repo / "docs" / "COVERAGE_GAP_ANALYSIS_RECENT.md"
    if recent_path.is_file():
        out_lines.append(recent_path.read_text(encoding="utf-8").strip())
        out_lines.append("")
    out_lines.append("## How to refresh")
    out_lines.append("")
    out_lines.append(
        "Narrative bullets under **Recent targeted tests** live in "
        "`docs/COVERAGE_GAP_ANALYSIS_RECENT.md` and are merged by this script when that file exists."
    )
    out_lines.append("")
    out_lines.append("```powershell")
    out_lines.append("dotnet test ArchLucid.sln -c Release --settings coverage.runsettings `")
    out_lines.append("  --collect:\"XPlat Code Coverage\" --results-directory .\\coverage-gap-1a")
    out_lines.append("dotnet tool restore")
    out_lines.append(
        "dotnet reportgenerator \"-reports:coverage-gap-1a/**/coverage.cobertura.xml\" "
        "\"-targetdir:coverage-gap-1a/merged\" \"-reporttypes:Cobertura\""
    )
    out_lines.append("python scripts/ci/coverage_gap_analysis.py")
    out_lines.append("```")
    out_lines.append("")

    dest = repo / "docs" / "COVERAGE_GAP_ANALYSIS.md"
    dest.write_text("\n".join(out_lines), encoding="utf-8")
    print(f"Wrote {dest}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
