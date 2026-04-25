"""Self-test for ``scripts/ci/assert_golden_cohort_kill_switch_present.py``.

Asserts the guard catches each weakening / removal it is meant to defend against, using fixture
files in a temporary directory (never modifies the real repo). Verifies:

    * Passes against a fixture tree that mirrors the live repo's kill-switch shape.
    * Fails when ``budget.config.json`` is missing.
    * Fails when ``warnThresholdPercent`` is removed.
    * Fails when ``warnThresholdPercent`` is weakened (e.g., 50 instead of 80).
    * Fails when ``killSwitchThresholdPercent`` is weakened (e.g., 110 instead of 95).
    * Fails when the workflow file is missing.
    * Fails when the workflow no longer references ``scripts/golden_cohort_budget_probe.py``.
    * Fails when the workflow no longer gates downstream steps on
      ``steps.budget.outputs.exit_code``.
    * Fails when the probe script itself is deleted.
    * Asserts the guard against the real repo passes (so a refactor that breaks the guard
      shows up here, not just at PR-merge time).
"""

from __future__ import annotations

import json
import subprocess
import sys
import tempfile
import textwrap
import unittest
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[3]
SCRIPT = REPO_ROOT / "scripts" / "ci" / "assert_golden_cohort_kill_switch_present.py"


VALID_WORKFLOW = textwrap.dedent(
    """\
    name: golden-cohort-nightly
    on:
      schedule: [{cron: "0 6 * * *"}]
    jobs:
      cohort-real-llm-gate:
        runs-on: ubuntu-latest
        steps:
          - id: budget
            run: python scripts/golden_cohort_budget_probe.py
          - if: ${{ steps.budget.outputs.exit_code == '0' }}
            run: dotnet test
    """
)

VALID_CONFIG = {
    "monthlyTokenBudgetUsd": 50,
    "warnThresholdPercent": 80,
    "killSwitchThresholdPercent": 95,
    "deploymentName": "x",
    "region": "y",
}


def _write(path: Path, contents: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(contents, encoding="utf-8")


def _materialize_fixture(tmp: Path, *, config: dict | None = None, workflow: str | None = None, probe: bool = True) -> Path:
    """Builds a synthetic repo root with the kill-switch shape and returns it."""
    root = tmp / "repo"
    root.mkdir()

    if config is not None:
        _write(root / "tests" / "golden-cohort" / "budget.config.json", json.dumps(config))

    if workflow is not None:
        _write(root / ".github" / "workflows" / "golden-cohort-nightly.yml", workflow)

    if probe:
        _write(root / "scripts" / "golden_cohort_budget_probe.py", "# probe\n")

    return root


def _run(repo_root: Path) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        [sys.executable, str(SCRIPT), "--repo-root", str(repo_root)],
        capture_output=True,
        text=True,
        check=False,
    )


class GoldenCohortKillSwitchGuardTests(unittest.TestCase):
    def test_passes_against_real_repo(self) -> None:
        proc = _run(REPO_ROOT)
        self.assertEqual(
            proc.returncode,
            0,
            f"Real repo should satisfy the kill-switch guard. stderr:\n{proc.stderr}",
        )

    def test_passes_on_valid_fixture(self) -> None:
        with tempfile.TemporaryDirectory() as raw_tmp:
            root = _materialize_fixture(Path(raw_tmp), config=VALID_CONFIG, workflow=VALID_WORKFLOW)
            proc = _run(root)
            self.assertEqual(proc.returncode, 0, proc.stderr)

    def test_fails_when_budget_config_missing(self) -> None:
        with tempfile.TemporaryDirectory() as raw_tmp:
            root = _materialize_fixture(Path(raw_tmp), config=None, workflow=VALID_WORKFLOW)
            proc = _run(root)
            self.assertNotEqual(proc.returncode, 0)
            self.assertIn("missing config file", proc.stderr)

    def test_fails_when_warn_threshold_missing(self) -> None:
        bad = dict(VALID_CONFIG)
        bad.pop("warnThresholdPercent")

        with tempfile.TemporaryDirectory() as raw_tmp:
            root = _materialize_fixture(Path(raw_tmp), config=bad, workflow=VALID_WORKFLOW)
            proc = _run(root)
            self.assertNotEqual(proc.returncode, 0)
            self.assertIn("warnThresholdPercent", proc.stderr)

    def test_fails_when_warn_threshold_weakened(self) -> None:
        bad = dict(VALID_CONFIG, warnThresholdPercent=50)

        with tempfile.TemporaryDirectory() as raw_tmp:
            root = _materialize_fixture(Path(raw_tmp), config=bad, workflow=VALID_WORKFLOW)
            proc = _run(root)
            self.assertNotEqual(proc.returncode, 0)
            self.assertIn("warnThresholdPercent", proc.stderr)

    def test_fails_when_kill_threshold_weakened(self) -> None:
        bad = dict(VALID_CONFIG, killSwitchThresholdPercent=110)

        with tempfile.TemporaryDirectory() as raw_tmp:
            root = _materialize_fixture(Path(raw_tmp), config=bad, workflow=VALID_WORKFLOW)
            proc = _run(root)
            self.assertNotEqual(proc.returncode, 0)
            self.assertIn("killSwitchThresholdPercent", proc.stderr)

    def test_fails_when_workflow_missing(self) -> None:
        with tempfile.TemporaryDirectory() as raw_tmp:
            root = _materialize_fixture(Path(raw_tmp), config=VALID_CONFIG, workflow=None)
            proc = _run(root)
            self.assertNotEqual(proc.returncode, 0)
            self.assertIn("missing workflow", proc.stderr)

    def test_fails_when_workflow_drops_probe_reference(self) -> None:
        bad_workflow = VALID_WORKFLOW.replace("scripts/golden_cohort_budget_probe.py", "echo no-probe")

        with tempfile.TemporaryDirectory() as raw_tmp:
            root = _materialize_fixture(Path(raw_tmp), config=VALID_CONFIG, workflow=bad_workflow)
            proc = _run(root)
            self.assertNotEqual(proc.returncode, 0)
            self.assertIn("kill-switch missing", proc.stderr)

    def test_fails_when_workflow_drops_exit_code_gate(self) -> None:
        bad_workflow = VALID_WORKFLOW.replace("steps.budget.outputs.exit_code", "false")

        with tempfile.TemporaryDirectory() as raw_tmp:
            root = _materialize_fixture(Path(raw_tmp), config=VALID_CONFIG, workflow=bad_workflow)
            proc = _run(root)
            self.assertNotEqual(proc.returncode, 0)
            self.assertIn("kill-switch not enforced", proc.stderr)

    def test_fails_when_probe_script_deleted(self) -> None:
        with tempfile.TemporaryDirectory() as raw_tmp:
            root = _materialize_fixture(Path(raw_tmp), config=VALID_CONFIG, workflow=VALID_WORKFLOW, probe=False)
            proc = _run(root)
            self.assertNotEqual(proc.returncode, 0)
            self.assertIn("missing probe script", proc.stderr)


if __name__ == "__main__":
    unittest.main()
