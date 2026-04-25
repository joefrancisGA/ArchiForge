#!/usr/bin/env python3
"""Merge-blocking guard: the golden-cohort kill-switch must stay shipped at 0.80 / 0.95.

PENDING_QUESTIONS Q15 (2026-04-23 sixth pass) approved the $50/month Azure OpenAI budget for the
golden-cohort real-LLM gate **conditional on the kill-switch being shipped**. If the kill-switch is
bypassed (e.g., a future change to the nightly workflow), real-LLM execution must revert to disabled
until the kill-switch is restored.

This guard refuses to merge any PR that:

1. Removes ``tests/golden-cohort/budget.config.json`` or weakens its threshold ratios away from
   ``warnThresholdPercent: 80`` / ``killSwitchThresholdPercent: 95``.
2. Removes the ``Golden cohort budget probe`` step from
   ``.github/workflows/golden-cohort-nightly.yml`` — recognised by:

      * the inline reference to ``scripts/golden_cohort_budget_probe.py``, AND
      * a guarded gate (``if:`` referencing ``steps.budget.outputs.exit_code``) on the cohort tests.

3. Removes ``scripts/golden_cohort_budget_probe.py`` itself (the probe is the only Cost Management
   caller — without it the workflow has no MTD signal to gate on).

Run locally:

    python scripts/ci/assert_golden_cohort_kill_switch_present.py

CI wiring: ``.github/workflows/ci.yml`` ``doc-markdown-links`` job, immediately after
``assert_billing_safety_rules_shipped.py`` (same pattern — both are Q-decision-conditional guards).
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]

BUDGET_CONFIG_REL = Path("tests/golden-cohort/budget.config.json")
WORKFLOW_REL = Path(".github/workflows/golden-cohort-nightly.yml")
PROBE_REL = Path("scripts/golden_cohort_budget_probe.py")

REQUIRED_WARN_PERCENT = 80
REQUIRED_KILL_PERCENT = 95


def _check_budget_config(repo_root: Path) -> list[str]:
    errors: list[str] = []
    config_path = repo_root / BUDGET_CONFIG_REL

    if not config_path.is_file():
        errors.append(f"missing config file: {BUDGET_CONFIG_REL}")
        return errors

    try:
        cfg = json.loads(config_path.read_text(encoding="utf-8"))
    except json.JSONDecodeError as ex:
        errors.append(f"{BUDGET_CONFIG_REL}: invalid JSON ({ex})")
        return errors

    warn_raw = cfg.get("warnThresholdPercent")
    kill_raw = cfg.get("killSwitchThresholdPercent")

    if warn_raw is None:
        errors.append(
            f"{BUDGET_CONFIG_REL}: missing 'warnThresholdPercent' (Q15-conditional rule requires {REQUIRED_WARN_PERCENT})"
        )
    elif _as_int(warn_raw) != REQUIRED_WARN_PERCENT:
        errors.append(
            f"{BUDGET_CONFIG_REL}: 'warnThresholdPercent' = {warn_raw}; Q15-conditional rule requires {REQUIRED_WARN_PERCENT}"
        )

    if kill_raw is None:
        errors.append(
            f"{BUDGET_CONFIG_REL}: missing 'killSwitchThresholdPercent' (Q15-conditional rule requires {REQUIRED_KILL_PERCENT})"
        )
    elif _as_int(kill_raw) != REQUIRED_KILL_PERCENT:
        errors.append(
            f"{BUDGET_CONFIG_REL}: 'killSwitchThresholdPercent' = {kill_raw}; Q15-conditional rule requires {REQUIRED_KILL_PERCENT}"
        )

    return errors


def _as_int(raw: object) -> int | None:
    """Coerces JSON numeric (int or float that is whole) to int — anything else returns None."""
    if isinstance(raw, bool):
        return None

    if isinstance(raw, int):
        return raw

    if isinstance(raw, float) and raw.is_integer():
        return int(raw)

    return None


def _check_probe_script(repo_root: Path) -> list[str]:
    probe_path = repo_root / PROBE_REL

    if not probe_path.is_file():
        return [f"missing probe script: {PROBE_REL} — workflow has no MTD signal without it"]

    return []


def _check_workflow(repo_root: Path) -> list[str]:
    errors: list[str] = []
    workflow_path = repo_root / WORKFLOW_REL

    if not workflow_path.is_file():
        errors.append(f"missing workflow: {WORKFLOW_REL}")
        return errors

    text = workflow_path.read_text(encoding="utf-8")

    if "scripts/golden_cohort_budget_probe.py" not in text:
        errors.append(
            f"{WORKFLOW_REL}: no step references scripts/golden_cohort_budget_probe.py — kill-switch missing"
        )

    if "steps.budget.outputs.exit_code" not in text:
        errors.append(
            f"{WORKFLOW_REL}: no downstream step gates on steps.budget.outputs.exit_code — kill-switch not enforced"
        )

    return errors


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--repo-root",
        type=Path,
        default=REPO_ROOT,
        help="Repo root to scan (defaults to the repo containing this script).",
    )
    args = parser.parse_args(argv)

    errors: list[str] = []
    errors.extend(_check_budget_config(args.repo_root))
    errors.extend(_check_probe_script(args.repo_root))
    errors.extend(_check_workflow(args.repo_root))

    if errors:
        print(
            "error: golden-cohort kill-switch guard failed (Q15-conditional rule):",
            file=sys.stderr,
        )

        for line in errors:
            print(f"  - {line}", file=sys.stderr)

        return 1

    print(
        f"OK: golden-cohort kill-switch shipped (warn={REQUIRED_WARN_PERCENT}%, kill={REQUIRED_KILL_PERCENT}%) "
        f"in {WORKFLOW_REL} + {BUDGET_CONFIG_REL}."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
