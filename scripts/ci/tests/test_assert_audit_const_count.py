from __future__ import annotations

import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

_FIXTURE_CS = """namespace ArchLucid.Core.Audit;

public static class AuditEventTypes
{
    public const string A = "A";

    public static class Run
    {
        public const string B = "Run.B";
    }

    public static class Baseline
    {
        public static class Architecture
        {
            public const string C = "Architecture.C";
        }

        public static class Governance
        {
            public const string D = "Governance.D";
        }
    }
}
"""

_FIXTURE_MATRIX = """<!-- audit-core-const-count:4 -->

## Appendix — Core `AuditEventTypes` registry (one row per constant)

| Constant | Value | Producer |
|----------|-------|----------|
| `A` | `A` | test |

## Appendix — `AuditEventTypes.Run` registry (Phase 2)

| Constant | Value | Producer |
|----------|-------|----------|
| `Run.B` | `Run.B` | test |

## Appendix — `AuditEventTypes.Baseline` registry (structured baseline log only)

| Constant path | Value | Producer |
|---------------|-------|----------|
| `Baseline.Architecture.C` | `Architecture.C` | test |
| `Baseline.Governance.D` | `Governance.D` | test |

When adding a `Baseline` constant, add a row here and bump `audit-core-const-count`.
"""


class TestAssertAuditConstCount(unittest.TestCase):
    def test_script_exits_zero_on_repo(self) -> None:
        repo_root = Path(__file__).resolve().parents[3]
        script = repo_root / "scripts" / "ci" / "assert_audit_const_count.py"
        result = subprocess.run(
            [sys.executable, str(script)],
            cwd=str(repo_root),
            capture_output=True,
            text=True,
            check=False,
        )

        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)

    def test_passes_on_minimal_fixture(self) -> None:
        repo_root = Path(__file__).resolve().parents[3]
        script = repo_root / "scripts" / "ci" / "assert_audit_const_count.py"
        with tempfile.TemporaryDirectory() as tmp:
            tmp_path = Path(tmp)
            cs = tmp_path / "AuditEventTypes.cs"
            md = tmp_path / "AUDIT_COVERAGE_MATRIX.md"
            cs.write_text(_FIXTURE_CS, encoding="utf-8")
            md.write_text(_FIXTURE_MATRIX, encoding="utf-8")
            result = subprocess.run(
                [
                    sys.executable,
                    str(script),
                    "--repo-root",
                    str(tmp_path),
                    "--audit-types",
                    str(cs),
                    "--matrix",
                    str(md),
                ],
                cwd=str(repo_root),
                capture_output=True,
                text=True,
                check=False,
            )

        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)

    def test_fails_when_matrix_missing_row(self) -> None:
        repo_root = Path(__file__).resolve().parents[3]
        script = repo_root / "scripts" / "ci" / "assert_audit_const_count.py"
        bad_matrix = _FIXTURE_MATRIX.replace("| `A` | `A` | test |\n", "", 1)
        with tempfile.TemporaryDirectory() as tmp:
            tmp_path = Path(tmp)
            cs = tmp_path / "AuditEventTypes.cs"
            md = tmp_path / "AUDIT_COVERAGE_MATRIX.md"
            cs.write_text(_FIXTURE_CS, encoding="utf-8")
            md.write_text(bad_matrix, encoding="utf-8")
            result = subprocess.run(
                [
                    sys.executable,
                    str(script),
                    "--repo-root",
                    str(tmp_path),
                    "--audit-types",
                    str(cs),
                    "--matrix",
                    str(md),
                ],
                cwd=str(repo_root),
                capture_output=True,
                text=True,
                check=False,
            )

        self.assertEqual(result.returncode, 1, result.stdout + result.stderr)
        self.assertIn("MISSING_IN_MATRIX", result.stderr)
        self.assertIn("A", result.stderr)

    def test_fails_when_marker_wrong(self) -> None:
        repo_root = Path(__file__).resolve().parents[3]
        script = repo_root / "scripts" / "ci" / "assert_audit_const_count.py"
        bad_matrix = _FIXTURE_MATRIX.replace("audit-core-const-count:4", "audit-core-const-count:3", 1)
        with tempfile.TemporaryDirectory() as tmp:
            tmp_path = Path(tmp)
            cs = tmp_path / "AuditEventTypes.cs"
            md = tmp_path / "AUDIT_COVERAGE_MATRIX.md"
            cs.write_text(_FIXTURE_CS, encoding="utf-8")
            md.write_text(bad_matrix, encoding="utf-8")
            result = subprocess.run(
                [
                    sys.executable,
                    str(script),
                    "--repo-root",
                    str(tmp_path),
                    "--audit-types",
                    str(cs),
                    "--matrix",
                    str(md),
                ],
                cwd=str(repo_root),
                capture_output=True,
                text=True,
                check=False,
            )

        self.assertEqual(result.returncode, 1, result.stdout + result.stderr)
        self.assertIn("MARKER_MISMATCH", result.stderr)

    def test_fails_on_duplicate_matrix_row(self) -> None:
        repo_root = Path(__file__).resolve().parents[3]
        script = repo_root / "scripts" / "ci" / "assert_audit_const_count.py"
        dup_matrix = _FIXTURE_MATRIX.replace(
            "| `A` | `A` | test |\n",
            "| `A` | `A` | test |\n| `A` | `A` | test |\n",
            1,
        )
        with tempfile.TemporaryDirectory() as tmp:
            tmp_path = Path(tmp)
            cs = tmp_path / "AuditEventTypes.cs"
            md = tmp_path / "AUDIT_COVERAGE_MATRIX.md"
            cs.write_text(_FIXTURE_CS, encoding="utf-8")
            md.write_text(dup_matrix, encoding="utf-8")
            result = subprocess.run(
                [
                    sys.executable,
                    str(script),
                    "--repo-root",
                    str(tmp_path),
                    "--audit-types",
                    str(cs),
                    "--matrix",
                    str(md),
                ],
                cwd=str(repo_root),
                capture_output=True,
                text=True,
                check=False,
            )

        self.assertEqual(result.returncode, 1, result.stdout + result.stderr)
        self.assertIn("DUPLICATE_MATRIX_ROWS", result.stderr)
        self.assertIn("ROW_COUNT_MISMATCH", result.stderr)

    def test_fails_when_matrix_has_extra_name(self) -> None:
        repo_root = Path(__file__).resolve().parents[3]
        script = repo_root / "scripts" / "ci" / "assert_audit_const_count.py"
        bad_matrix = _FIXTURE_MATRIX.replace(
            "| `A` | `A` | test |\n",
            "| `A` | `A` | test |\n| `ZOnlyInMatrix` | `ZOnlyInMatrix` | test |\n",
            1,
        )
        with tempfile.TemporaryDirectory() as tmp:
            tmp_path = Path(tmp)
            cs = tmp_path / "AuditEventTypes.cs"
            md = tmp_path / "AUDIT_COVERAGE_MATRIX.md"
            cs.write_text(_FIXTURE_CS, encoding="utf-8")
            md.write_text(bad_matrix, encoding="utf-8")
            result = subprocess.run(
                [
                    sys.executable,
                    str(script),
                    "--repo-root",
                    str(tmp_path),
                    "--audit-types",
                    str(cs),
                    "--matrix",
                    str(md),
                ],
                cwd=str(repo_root),
                capture_output=True,
                text=True,
                check=False,
            )

        self.assertEqual(result.returncode, 1, result.stdout + result.stderr)
        self.assertIn("EXTRA_IN_MATRIX", result.stderr)
        self.assertIn("ZOnlyInMatrix", result.stderr)
