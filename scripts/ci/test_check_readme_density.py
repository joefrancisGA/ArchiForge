"""Unit tests for check_readme_density.py."""

from __future__ import annotations

import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


class CheckReadmeDensityTests(unittest.TestCase):
    def test_fails_without_details(self) -> None:
        script = Path(__file__).resolve().parent / "check_readme_density.py"
        with tempfile.TemporaryDirectory() as tmp:
            p = Path(tmp) / "README.md"
            p.write_text("# Hi\n\n[one](a.md)\n", encoding="utf-8")
            r = subprocess.run(
                [sys.executable, str(script), "--readme", str(p), "--max-links", "12"],
                capture_output=True,
                text=True,
                check=False,
            )
        self.assertEqual(r.returncode, 1)

    def test_fails_when_too_many_links_above_details(self) -> None:
        script = Path(__file__).resolve().parent / "check_readme_density.py"
        links = "\n".join(f"[L{i}](x{i}.md)" for i in range(15))
        with tempfile.TemporaryDirectory() as tmp:
            p = Path(tmp) / "README.md"
            p.write_text(f"# T\n{links}\n<details>\n<summary>x</summary>\n[more](z.md)\n</details>\n", encoding="utf-8")
            r = subprocess.run(
                [sys.executable, str(script), "--readme", str(p), "--max-links", "12"],
                capture_output=True,
                text=True,
                check=False,
            )
        self.assertEqual(r.returncode, 1)

    def test_passes_at_boundary(self) -> None:
        script = Path(__file__).resolve().parent / "check_readme_density.py"
        links = "\n".join(f"[L{i}](x{i}.md)" for i in range(12))
        with tempfile.TemporaryDirectory() as tmp:
            p = Path(tmp) / "README.md"
            p.write_text(f"# T\n{links}\n<details>\n<summary>x</summary>\n</details>\n", encoding="utf-8")
            r = subprocess.run(
                [sys.executable, str(script), "--readme", str(p), "--max-links", "12"],
                capture_output=True,
                text=True,
                check=False,
            )
        self.assertEqual(r.returncode, 0)

    def test_repo_readme_passes(self) -> None:
        script = Path(__file__).resolve().parent / "check_readme_density.py"
        root = Path(__file__).resolve().parents[2]
        r = subprocess.run(
            [sys.executable, str(script), "--readme", str(root / "README.md")],
            capture_output=True,
            text=True,
            check=False,
        )
        self.assertEqual(r.returncode, 0, msg=r.stderr + r.stdout)


if __name__ == "__main__":
    unittest.main()
