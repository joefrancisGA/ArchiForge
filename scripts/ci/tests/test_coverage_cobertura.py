"""Unit tests for coverage_cobertura.parse_cobertura and helpers."""

from __future__ import annotations

from pathlib import Path

import pytest

from coverage_cobertura import (
    CoberturaPackageMetrics,
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
        ("ArchLucid.Persistence.Tests.Something", False),
    ],
)
def test_is_product_archlucid_package(name: str, expected: bool) -> None:
    assert is_product_archlucid_package(name) is expected


def _minimal_cobertura_xml(
    *,
    line_rate: str | None = "0.75",
    branch_rate: str | None = "0.60",
    body: str = "",
) -> str:
    lr = f' line-rate="{line_rate}"' if line_rate is not None else ""
    br = f' branch-rate="{branch_rate}"' if branch_rate is not None else ""
    return f"""<?xml version="1.0" encoding="utf-8"?>
<coverage{lr}{br} version="1.9">
{body}
</coverage>
"""


def test_parse_cobertura_happy_path(tmp_path: Path) -> None:
    body = """
  <package name="ArchLucid.Core" line-rate="0.8" branch-rate="0.5">
    <classes>
      <class>
        <lines>
          <line number="1"/>
          <line number="2"/>
        </lines>
      </class>
    </classes>
  </package>
  <package name="OtherLib" line-rate="0.5" branch-rate="0.25">
    <lines><line number="10"/></lines>
  </package>
"""
    path = tmp_path / "Cobertura.xml"
    path.write_text(_minimal_cobertura_xml(body=body), encoding="utf-8")

    summary = parse_cobertura(path)
    assert summary is not None
    assert summary.root_line_pct == pytest.approx(75.0)
    assert summary.root_branch_pct == pytest.approx(60.0)
    assert len(summary.packages) == 2
    names = [p.name for p in summary.packages]
    assert names == ["ArchLucid.Core", "OtherLib"]
    core = summary.packages[0]
    assert core.line_rate == pytest.approx(0.8)
    assert core.branch_rate == pytest.approx(0.5)
    assert core.coverable_lines == 2
    other = summary.packages[1]
    assert other.coverable_lines == 1


def test_parse_cobertura_missing_file(tmp_path: Path) -> None:
    assert parse_cobertura(tmp_path / "nope.xml") is None


def test_parse_cobertura_malformed_xml(tmp_path: Path) -> None:
    path = tmp_path / "bad.xml"
    path.write_text("<<<", encoding="utf-8")
    assert parse_cobertura(path) is None


def test_parse_cobertura_missing_root_line_rate(tmp_path: Path) -> None:
    path = tmp_path / "no-line.xml"
    path.write_text(
        _minimal_cobertura_xml(line_rate=None, branch_rate="0.5", body="<package name='P' line-rate='1' branch-rate='1'><line number='1'/></package>"),
        encoding="utf-8",
    )
    summary = parse_cobertura(path)
    assert summary is not None
    assert summary.root_line_pct is None
    assert summary.root_branch_pct == pytest.approx(50.0)


def test_product_packages_for_gate_filters_non_product_and_zero_lines() -> None:
    summary = CoberturaSummary(
        root_line_pct=80.0,
        root_branch_pct=50.0,
        packages=[
            CoberturaPackageMetrics("ArchLucid.Core", 0.9, 0.5, 2),
            CoberturaPackageMetrics("ArchLucid.Core.Tests", 0.9, 0.5, 2),
            CoberturaPackageMetrics("ArchLucid.TestSupport", 0.9, 0.5, 2),
            CoberturaPackageMetrics("SomeOtherLib", 0.9, 0.5, 2),
            CoberturaPackageMetrics("ArchLucid.Persistence", 0.9, 0.5, 0),
        ],
    )
    gated = product_packages_for_gate(summary)
    assert len(gated) == 1
    assert gated[0].name == "ArchLucid.Core"
