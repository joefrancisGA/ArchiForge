"""Unit tests for assert_merged_line_coverage_min._main()."""

from __future__ import annotations

from pathlib import Path

import assert_merged_line_coverage_min as merged


def _write_cobertura(
    path: Path,
    *,
    line_rate: str,
    branch_rate: str | None,
    packages: list[tuple[str, str, str | None, int]],
) -> Path:
    """
    packages: (name, line_rate_attr, branch_rate_attr or omit if None, num_line_elements)
    """
    pkg_blocks: list[str] = []
    for name, lr, br, n_lines in packages:
        br_attr = f' branch-rate="{br}"' if br is not None else ""
        lines_xml = "\n".join(f'            <line number="{i + 1}"/>' for i in range(n_lines))
        pkg_blocks.append(
            f"""  <package name="{name}" line-rate="{lr}"{br_attr}>
{lines_xml}
  </package>""",
        )
    branch_root = f' branch-rate="{branch_rate}"' if branch_rate is not None else ""
    body = "\n".join(pkg_blocks)
    path.write_text(
        f"""<?xml version="1.0"?>
<coverage line-rate="{line_rate}"{branch_root}>
{body}
</coverage>""",
        encoding="utf-8",
    )
    return path


def test_main_all_passes(tmp_path: Path) -> None:
    p = _write_cobertura(
        tmp_path / "ok.xml",
        line_rate="0.80",
        branch_rate="0.60",
        packages=[
            ("ArchLucid.Core", "0.90", "0.80", 2),
            ("ArchLucid.Host.Core", "0.85", "0.70", 1),
        ],
    )
    assert merged._main([str(p), "--min-line-pct", "70", "--min-branch-pct", "50", "--min-package-line-pct", "40"]) == 0


def test_main_line_below_min(tmp_path: Path) -> None:
    p = _write_cobertura(
        tmp_path / "low-line.xml",
        line_rate="0.69",
        branch_rate="0.60",
        packages=[("ArchLucid.Core", "0.90", "0.80", 1)],
    )
    assert merged._main([str(p)]) == 1


def test_main_branch_below_min(tmp_path: Path) -> None:
    p = _write_cobertura(
        tmp_path / "low-branch.xml",
        line_rate="0.90",
        branch_rate="0.49",
        packages=[("ArchLucid.Core", "0.90", "0.80", 1)],
    )
    assert merged._main([str(p), "--min-branch-pct", "50"]) == 1


def test_main_package_below_min(tmp_path: Path) -> None:
    p = _write_cobertura(
        tmp_path / "low-pkg.xml",
        line_rate="0.90",
        branch_rate="0.60",
        packages=[("ArchLucid.Core", "0.39", "0.50", 2)],
    )
    assert merged._main([str(p), "--min-package-line-pct", "40"]) == 1


def test_main_missing_branch_rate_root(tmp_path: Path) -> None:
    p = _write_cobertura(
        tmp_path / "no-branch.xml",
        line_rate="0.90",
        branch_rate=None,
        packages=[("ArchLucid.Core", "0.90", "0.80", 1)],
    )
    assert merged._main([str(p)]) == 2


def test_main_missing_file(tmp_path: Path) -> None:
    assert merged._main([str(tmp_path / "missing.xml")]) == 2


def test_main_positional_min_line_backward_compatible(tmp_path: Path) -> None:
    p = _write_cobertura(
        tmp_path / "pos.xml",
        line_rate="0.75",
        branch_rate="0.60",
        packages=[("ArchLucid.Core", "0.90", "0.80", 1)],
    )
    assert merged._main([str(p), "76"]) == 1
    assert merged._main([str(p), "70"]) == 0
