"""Unit tests for coverage_cobertura.py."""

from __future__ import annotations

from pathlib import Path

import pytest

from coverage_cobertura import (
    CoberturaSummary,
    is_product_archlucid_package,
    parse_cobertura,
    product_packages_for_gate,
)


@pytest.mark.parametrize(
    ("name", "expected"),
    [
        ("ArchLucid.Core", True),
        ("ArchLucid.Core.Tests", False),
        ("ArchLucid.TestSupport", False),
        ("SomeOtherLib", False),
    ],
)
def test_is_product_archlucid_package(name: str, expected: bool) -> None:
    assert is_product_archlucid_package(name) is expected


def _write_xml(path: Path, content: str) -> Path:
    path.write_text(content.strip(), encoding="utf-8")
    return path


def test_parse_cobertura_minimal_valid(tmp_path: Path) -> None:
    xml_path = tmp_path / "Cobertura.xml"
    _write_xml(
        xml_path,
        """<?xml version="1.0" encoding="utf-8"?>
<coverage line-rate="0.85" branch-rate="0.70">
  <packages>
    <package name="ArchLucid.Core" line-rate="0.90" branch-rate="0.80">
      <classes>
        <class name="T" filename="T.cs">
          <lines>
            <line number="1" hits="1"/>
            <line number="2" hits="1"/>
          </lines>
        </class>
      </classes>
    </package>
  </packages>
</coverage>""",
    )
    summary = parse_cobertura(xml_path)
    assert summary is not None
    assert summary.root_line_pct == pytest.approx(85.0)
    assert summary.root_branch_pct == pytest.approx(70.0)
    assert len(summary.packages) == 1
    pkg = summary.packages[0]
    assert pkg.name == "ArchLucid.Core"
    assert pkg.line_rate == pytest.approx(0.90)
    assert pkg.branch_rate == pytest.approx(0.80)
    assert pkg.coverable_lines == 2


def test_parse_cobertura_missing_file(tmp_path: Path) -> None:
    assert parse_cobertura(tmp_path / "nope.xml") is None


def test_parse_cobertura_malformed_xml(tmp_path: Path) -> None:
    p = tmp_path / "bad.xml"
    p.write_text("<coverage><unclosed", encoding="utf-8")
    assert parse_cobertura(p) is None


def test_parse_cobertura_missing_root_line_rate(tmp_path: Path) -> None:
    xml_path = tmp_path / "no-line.xml"
    _write_xml(
        xml_path,
        """<?xml version="1.0"?>
<coverage branch-rate="0.5">
  <package name="ArchLucid.Core" line-rate="1" branch-rate="1">
    <line number="1"/>
  </package>
</coverage>""",
    )
    summary = parse_cobertura(xml_path)
    assert summary is not None
    assert summary.root_line_pct is None
    assert summary.root_branch_pct == pytest.approx(50.0)


def test_product_packages_for_gate_filters_non_product_and_zero_lines(tmp_path: Path) -> None:
    xml_path = tmp_path / "merged.xml"
    _write_xml(
        xml_path,
        """<?xml version="1.0"?>
<coverage line-rate="1" branch-rate="1">
  <package name="ArchLucid.Core" line-rate="1" branch-rate="1">
    <line number="10"/>
  </package>
  <package name="ArchLucid.Core.Tests" line-rate="0.5" branch-rate="0.5">
    <line number="1"/>
  </package>
  <package name="ArchLucid.Host" line-rate="1" branch-rate="1">
  </package>
  <package name="OtherVendor" line-rate="1" branch-rate="1">
    <line number="2"/>
  </package>
</coverage>""",
    )
    summary = parse_cobertura(xml_path)
    assert summary is not None
    gated = product_packages_for_gate(summary)
    names = {p.name for p in gated}
    assert names == {"ArchLucid.Core"}
    assert all(p.coverable_lines > 0 for p in gated)
