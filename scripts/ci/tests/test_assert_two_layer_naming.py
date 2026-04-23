from __future__ import annotations

import subprocess
import sys
import unittest
from pathlib import Path


class TestAssertTwoLayerNaming(unittest.TestCase):
    def test_script_exits_zero(self) -> None:
        repo_root = Path(__file__).resolve().parents[3]
        script = repo_root / "scripts" / "ci" / "assert_two_layer_naming.py"
        result = subprocess.run(
            [sys.executable, str(script)],
            cwd=str(repo_root),
            capture_output=True,
            text=True,
            check=False,
        )

        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
