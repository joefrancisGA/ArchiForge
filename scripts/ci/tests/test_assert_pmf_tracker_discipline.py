import subprocess
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
SCRIPT = ROOT / "scripts" / "ci" / "assert_pmf_tracker_discipline.py"
TRACKER = ROOT / "docs" / "go-to-market" / "PMF_VALIDATION_TRACKER.md"


class AssertPmfTrackerDisciplineTests(unittest.TestCase):
    def test_committed_tracker_passes(self):
        proc = subprocess.run(
            ["python", str(SCRIPT), "--tracker", str(TRACKER)],
            cwd=str(ROOT),
            capture_output=True,
            text=True,
            check=False,
        )
        self.assertEqual(proc.returncode, 0, proc.stderr + proc.stdout)

    def test_captured_with_tbd_result_fails(self):
        bad = (
            "| Hypothesis | Pilot ID | ICP score | ICP segment | Scorecard metric | Baseline | Result | Status | Notes |\n"
            "|------------|----------|-----------|-------------|------------------|----------|--------|--------|-------|\n"
            "| H9 | Pilot Z | TBD | TBD | Test | TBD | TBD | Captured | bogus |\n"
        )
        import tempfile

        with tempfile.NamedTemporaryFile(
            mode="w", suffix=".md", delete=False, encoding="utf-8"
        ) as tmp:
            tmp.write(bad)
            tmp_path = Path(tmp.name)

        try:
            proc = subprocess.run(
                ["python", str(SCRIPT), "--tracker", str(tmp_path)],
                cwd=str(ROOT),
                capture_output=True,
                text=True,
                check=False,
            )
            self.assertNotEqual(proc.returncode, 0, proc.stdout)
        finally:
            tmp_path.unlink(missing_ok=True)


if __name__ == "__main__":
    unittest.main()
