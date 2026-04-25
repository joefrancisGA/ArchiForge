"""Smoke test: integration recipe guard exits zero on the real repo tree."""

from __future__ import annotations

import subprocess
import sys
import unittest
from pathlib import Path


class TestCheckIntegrationRecipesLinkAlignment(unittest.TestCase):
    def test_script_exits_zero_on_repo(self) -> None:
        repo_root = Path(__file__).resolve().parents[3]
        script = repo_root / "scripts" / "ci" / "check_integration_recipes_link_alignment.py"
        result = subprocess.run(
            [sys.executable, str(script)],
            cwd=str(repo_root),
            capture_output=True,
            text=True,
            check=False,
        )

        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)


if __name__ == "__main__":
    unittest.main()
