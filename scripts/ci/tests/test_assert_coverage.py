"""Tests for assert_merged_line_coverage_min._main (Cobertura gates)."""

from __future__ import annotations

from pathlib import Path

import pytest

from assert_merged_line_coverage_min import _main


def _write_cobertura(
    path: Path,
    *,
    root_line: str = "0.80",
    root_branch: str | None = "0.60",
    packages_snippet: str = "",
) -> None:
    br_attr = f' branch-rate="{root_branch}"' if root_branch is not None else ""
    path.write_text(
        f"""<?xml version="1.0" encoding="utf-8"?>
<coverage line-rate="{root_line}"{br_attr} version="1.9">
{packages_snippet}
</coverage>
""",
        encoding="utf-8",
    )


def _product_pkg(name: str, line_rate: str, lines_xml: str) -> str:
    return f"""  <package name="{name}" line-rate="{line_rate}" branch-rate="0.5">
    <classes><class><lines>{lines_xml}</lines></class></classes>
  </package>
"""


def test_all_gates_pass(tmp_path: Path) -> None:
    two_lines = "<line number='1'/><line number='2'/>"
    xml = tmp_path / "c.xml"
    _write_cobertura(
        xml,
        root_line="0.80",
        root_branch="0.60",
        packages_snippet=_product_pkg("ArchLucid.Core", "0.85", two_lines),
    )
    assert _main([str(xml)]) == 0


def test_line_below_default_min_fails(tmp_path: Path) -> None:
    two_lines = "<line number='1'/><line number='2'/>"
    xml = tmp_path / "c.xml"
    _write_cobertura(
        xml,
        root_line="0.65",
        root_branch="0.60",
        packages_snippet=_product_pkg("ArchLucid.Core", "0.90", two_lines),
    )
    assert _main([str(xml)]) == 1


def test_branch_below_default_min_fails(tmp_path: Path) -> None:
    two_lines = "<line number='1'/><line number='2'/>"
    xml = tmp_path / "c.xml"
    _write_cobertura(
        xml,
        root_line="0.80",
        root_branch="0.40",
        packages_snippet=_product_pkg("ArchLucid.Core", "0.90", two_lines),
    )
    assert _main([str(xml)]) == 1


def test_product_package_below_40_fails(tmp_path: Path) -> None:
    two_lines = "<line number='1'/><line number='2'/>"
    xml = tmp_path / "c.xml"
    _write_cobertura(
        xml,
        root_line="0.80",
        root_branch="0.60",
        packages_snippet=_product_pkg("ArchLucid.Core", "0.35", two_lines),
    )
    assert _main([str(xml)]) == 1


def test_missing_branch_rate_on_root_exits_2(tmp_path: Path) -> None:
    xml = tmp_path / "c.xml"
    _write_cobertura(xml, root_line="0.80", root_branch=None, packages_snippet="")
    assert _main([str(xml)]) == 2


def test_missing_file_exits_2(tmp_path: Path) -> None:
    assert _main([str(tmp_path / "missing.xml")]) == 2


def test_positional_min_line_overrides_default(tmp_path: Path) -> None:
    two_lines = "<line number='1'/><line number='2'/>"
    xml = tmp_path / "c.xml"
    _write_cobertura(
        xml,
        root_line="0.75",
        root_branch="0.60",
        packages_snippet=_product_pkg("ArchLucid.Core", "0.90", two_lines),
    )
    assert _main([str(xml), "70"]) == 0
    assert _main([str(xml), "80"]) == 1
