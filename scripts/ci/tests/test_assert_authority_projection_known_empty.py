"""Unit tests for assert_authority_projection_known_empty.py."""

from __future__ import annotations

import json
import subprocess
import sys
import unittest
from pathlib import Path


class AuthorityProjectionKnownEmptyTests(unittest.TestCase):
    def test_script_passes_on_repo_json(self) -> None:
        repo_root = Path(__file__).resolve().parents[3]
        script = repo_root / "scripts" / "ci" / "assert_authority_projection_known_empty.py"
        result = subprocess.run(
            [sys.executable, str(script)],
            cwd=str(repo_root),
            capture_output=True,
            text=True,
            check=False,
        )
        self.assertEqual(result.returncode, 0, result.stderr)

    def test_json_has_relationships_row(self) -> None:
        repo_root = Path(__file__).resolve().parents[3]
        path = repo_root / "docs" / "architecture" / "AUTHORITY_PROJECTION_KNOWN_EMPTY.json"
        data = json.loads(path.read_text(encoding="utf-8"))
        names = {r["name"] for r in data["emptyFields"]}
        self.assertIn("Relationships", names)


if __name__ == "__main__":
    unittest.main()
