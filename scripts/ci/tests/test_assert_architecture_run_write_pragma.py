"""Unit tests for assert_architecture_run_write_pragma.scan_repository()."""

from __future__ import annotations

from pathlib import Path

import assert_architecture_run_write_pragma as arwp


def test_passes_when_pragma_with_convergence_before_write(tmp_path: Path) -> None:
    (tmp_path / "Prod").mkdir()
    (tmp_path / "Prod" / "Writer.cs").write_text(
        """
class X {
  void M() {
#pragma warning disable CS0618 // RunsAuthorityConvergence: tracked for migration by 2026-09-30
    await runRepository.CreateAsync(run, ct);
#pragma warning restore CS0618
  }
}
""",
        encoding="utf-8",
    )
    assert arwp.scan_repository(tmp_path) == []


def test_fails_on_write_without_pragma(tmp_path: Path) -> None:
    (tmp_path / "Prod").mkdir()
    (tmp_path / "Prod" / "Bad.cs").write_text(
        """
class X {
  void M() {
    await runRepository.CreateAsync(run, ct);
  }
}
""",
        encoding="utf-8",
    )
    v = arwp.scan_repository(tmp_path)
    assert len(v) == 1
    assert "Bad.cs" in v[0]


def test_fails_when_cs0618_disable_lacks_convergence_marker(tmp_path: Path) -> None:
    (tmp_path / "Prod").mkdir()
    (tmp_path / "Prod" / "Bad2.cs").write_text(
        """
class X {
  void M() {
#pragma warning disable CS0618
    await _runRepository.UpdateStatusAsync(id, st, null, null, ct);
#pragma warning restore CS0618
  }
}
""",
        encoding="utf-8",
    )
    assert len(arwp.scan_repository(tmp_path)) == 1


def test_ignores_commented_writes(tmp_path: Path) -> None:
    (tmp_path / "Prod").mkdir()
    (tmp_path / "Prod" / "Ok.cs").write_text(
        """
class X {
  void M() {
//    await runRepository.CreateAsync(run, ct);
  }
}
""",
        encoding="utf-8",
    )
    assert arwp.scan_repository(tmp_path) == []


def test_skips_test_assembly_paths(tmp_path: Path) -> None:
    (tmp_path / "ArchLucid.Stuff.Tests").mkdir(parents=True)
    (tmp_path / "ArchLucid.Stuff.Tests" / "T.cs").write_text(
        "await runRepository.CreateAsync(run, ct);",
        encoding="utf-8",
    )
    assert arwp.scan_repository(tmp_path) == []


def test_di_registration_requires_pragma(tmp_path: Path) -> None:
    (tmp_path / "Host").mkdir()
    (tmp_path / "Host" / "Di.cs").write_text(
        """
services.AddScoped<IArchitectureRunRepository, ArchitectureRunRepository>();
""",
        encoding="utf-8",
    )
    assert len(arwp.scan_repository(tmp_path)) == 1
