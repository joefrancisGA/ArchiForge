import json
import subprocess
import tempfile
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[3]
SCRIPT = ROOT / "scripts" / "ci" / "assert_query_performance.py"
ALLOWLIST = ROOT / "tests" / "performance" / "query-allowlist.json"


class AssertQueryPerformanceTests(unittest.TestCase):
    def test_dry_run_exits_zero(self):
        proc = subprocess.run(
            ["python", str(SCRIPT), "--dry-run"],
            cwd=str(ROOT),
            capture_output=True,
            text=True,
            check=False,
        )
        self.assertEqual(proc.returncode, 0, proc.stderr + proc.stdout)

    def test_measurement_over_threshold_exits_nonzero(self):
        allow = json.loads(ALLOWLIST.read_text(encoding="utf-8"))

        inflated = [{"name": row["name"], "p95Ms": float(row["p95ThresholdMs"]) + 50.0} for row in allow]

        with tempfile.NamedTemporaryFile("w", suffix=".json", delete=False, encoding="utf-8") as tmp:
            json.dump({"queries": inflated}, tmp)
            tmp_path = Path(tmp.name)

        try:
            proc = subprocess.run(
                ["python", str(SCRIPT), "--measurements-json", str(tmp_path)],
                cwd=str(ROOT),
                capture_output=True,
                text=True,
                check=False,
            )
            self.assertNotEqual(proc.returncode, 0, proc.stdout)

        finally:
            tmp_path.unlink(missing_ok=True)

    def test_unknown_query_names_warn_only_when_under_threshold_subset(self):
        allow = json.loads(ALLOWLIST.read_text(encoding="utf-8"))

        qs = [{"name": row["name"], "p95Ms": float(row["p95ThresholdMs"]) * 0.1} for row in allow]
        qs.append({"name": "NotInAllowlistSandbox", "p95Ms": 1.0})

        with tempfile.NamedTemporaryFile("w", suffix=".json", delete=False, encoding="utf-8") as tmp:
            json.dump({"queries": qs}, tmp)
            tmp_path = Path(tmp.name)

        try:
            proc = subprocess.run(
                ["python", str(SCRIPT), "--measurements-json", str(tmp_path)],
                cwd=str(ROOT),
                capture_output=True,
                text=True,
                check=False,
            )
            self.assertEqual(proc.returncode, 0, proc.stderr + proc.stdout)
            self.assertIn("NotInAllowlistSandbox", proc.stdout)

        finally:
            tmp_path.unlink(missing_ok=True)


if __name__ == "__main__":
    unittest.main()
