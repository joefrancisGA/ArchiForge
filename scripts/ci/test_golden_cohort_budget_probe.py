"""Threshold smoke tests for ``scripts/golden_cohort_budget_probe.py`` (simulate mode, no Azure).

Improvement 11 (2026-04-24) — exercises the **dual-threshold** kill-switch shape mandated by
PENDING_QUESTIONS Q15 (warn at 80% of cap, kill at 95% of cap, hard skip at-or-above 95% — the
"100% of cap" line is no longer special because the kill threshold sits below the cap).

Run locally:

    python -m unittest scripts/ci/test_golden_cohort_budget_probe.py

Also exercised by ``scripts/ci/assert_golden_cohort_kill_switch_present.py`` self-test, which
asserts the workflow still calls the probe with these exit-code semantics.
"""

from __future__ import annotations

import json
import os
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]
PROBE = REPO_ROOT / "scripts" / "golden_cohort_budget_probe.py"
LIVE_CONFIG = REPO_ROOT / "tests" / "golden-cohort" / "budget.config.json"


class GoldenCohortBudgetProbeDualThresholdTests(unittest.TestCase):
    """Asserts the live ``budget.config.json`` produces the prompt-mandated 0.80 / 0.95 exit codes."""

    def _run(self, mtd: str, config: Path | None = None) -> int:
        env = os.environ.copy()
        env["ARCHLUCID_GOLDEN_COHORT_BUDGET_PROBE_SIMULATE_MTD_USD"] = mtd
        cmd = [sys.executable, str(PROBE)]

        if config is not None:
            cmd.extend(["--config", str(config)])

        proc = subprocess.run(cmd, cwd=REPO_ROOT, capture_output=True, text=True, check=False, env=env)
        return proc.returncode

    def test_under_warn_threshold_returns_zero(self) -> None:
        # 80% of $50 = $40 — anything below is the green continue path.
        self.assertEqual(self._run("0"), 0)
        self.assertEqual(self._run("10"), 0)
        self.assertEqual(self._run("39.99"), 0)

    def test_at_or_above_warn_below_kill_returns_one(self) -> None:
        # Warn band [$40, $47.50). Workflow continues but posts an issue.
        self.assertEqual(self._run("40"), 1)
        self.assertEqual(self._run("45"), 1)
        self.assertEqual(self._run("47.49"), 1)

    def test_at_or_above_kill_threshold_returns_two(self) -> None:
        # Kill at $47.50 (95% of $50). Workflow SKIPS cohort run, posts issue, does NOT count as failure.
        self.assertEqual(self._run("47.50"), 2)
        self.assertEqual(self._run("50"), 2)
        self.assertEqual(self._run("60"), 2)


class GoldenCohortBudgetProbeBackwardsCompatTests(unittest.TestCase):
    """Single-threshold configs (no ``warnThresholdPercent``) keep working — no exit-1 band."""

    def _write_legacy_config(self, tmp: Path) -> Path:
        config = tmp / "legacy.json"
        config.write_text(
            json.dumps({"monthlyTokenBudgetUsd": 50, "killSwitchThresholdPercent": 95}),
            encoding="utf-8",
        )
        return config

    def _run(self, mtd: str, config: Path) -> int:
        env = os.environ.copy()
        env["ARCHLUCID_GOLDEN_COHORT_BUDGET_PROBE_SIMULATE_MTD_USD"] = mtd
        proc = subprocess.run(
            [sys.executable, str(PROBE), "--config", str(config)],
            cwd=REPO_ROOT,
            capture_output=True,
            text=True,
            check=False,
            env=env,
        )
        return proc.returncode

    def test_legacy_single_threshold_skips_warn_band(self) -> None:
        with tempfile.TemporaryDirectory() as raw_tmp:
            tmp = Path(raw_tmp)
            config = self._write_legacy_config(tmp)
            self.assertEqual(self._run("0", config), 0, "below kill threshold → 0")
            self.assertEqual(self._run("46", config), 0, "no warn band → still 0 below kill")
            self.assertEqual(self._run("47.50", config), 2, "at kill threshold → 2")


class GoldenCohortBudgetProbeExportsTests(unittest.TestCase):
    """Asserts the new ``EXPORT_WARN_THRESHOLD_*`` lines are emitted only when warn is configured.

    The workflow's downstream issue-creation step greps for these lines, so emitting them on the
    happy path keeps the issue body informative even when warn isn't tripped.
    """

    def _run_capture(self, mtd: str) -> str:
        env = os.environ.copy()
        env["ARCHLUCID_GOLDEN_COHORT_BUDGET_PROBE_SIMULATE_MTD_USD"] = mtd
        proc = subprocess.run(
            [sys.executable, str(PROBE)],
            cwd=REPO_ROOT,
            capture_output=True,
            text=True,
            check=False,
            env=env,
        )
        return proc.stdout

    def test_emits_warn_threshold_export(self) -> None:
        out = self._run_capture("0")
        self.assertIn("EXPORT_WARN_THRESHOLD_USD=", out)
        self.assertIn("EXPORT_WARN_THRESHOLD_PCT=80.0000", out)
        self.assertIn("EXPORT_KILL_THRESHOLD_PCT=95.0000", out)


if __name__ == "__main__":
    unittest.main()
