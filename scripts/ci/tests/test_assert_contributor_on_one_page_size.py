from __future__ import annotations

import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


class TestAssertContributorOnOnePageSize(unittest.TestCase):
    def test_script_exits_zero_on_repo_file(self) -> None:
        repo_root = Path(__file__).resolve().parents[3]
        script = repo_root / "scripts" / "ci" / "assert_contributor_on_one_page_size.py"
        result = subprocess.run(
            [sys.executable, str(script)],
            cwd=str(repo_root),
            capture_output=True,
            text=True,
            check=False,
        )

        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)

    def test_script_fails_when_over_budget(self) -> None:
        repo_root = Path(__file__).resolve().parents[3]
        script = repo_root / "scripts" / "ci" / "assert_contributor_on_one_page_size.py"

        with tempfile.TemporaryDirectory() as tmp:
            tmp_path = Path(tmp)
            long_file = tmp_path / "long.md"
            long_file.write_text("x\n" * 81, encoding="utf-8")
            result = subprocess.run(
                [
                    sys.executable,
                    str(script),
                    "--repo-root",
                    str(tmp_path),
                    "--path",
                    str(long_file),
                    "--max-lines",
                    "80",
                ],
                cwd=str(repo_root),
                capture_output=True,
                text=True,
                check=False,
            )

        self.assertEqual(result.returncode, 1, result.stdout + result.stderr)
        self.assertIn("FAILED", result.stderr)
