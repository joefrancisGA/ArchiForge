#!/usr/bin/env python3
"""
Month-to-date Azure spend probe for the dedicated golden-cohort Azure OpenAI resource.

Reads ``tests/golden-cohort/budget.config.json``, queries **Azure Cost Management** (management plane REST,
no Azure SDK) using ``requests``, and exits:

* **0** — month-to-date cost is **below** the warn threshold (default **80%** of ``monthlyTokenBudgetUsd``).
* **1** — at or above the warn threshold but **below** the kill threshold (default **95%**) — the workflow
  surfaces a yellow warning and posts an issue, but real-LLM cohort still runs (exit 1 is **not** a hard skip).
* **2** — at or above the kill threshold (default **95%**) — real-LLM cohort is **skipped** for the rest of
  the month, an issue is posted, but the workflow does **not** count as failure.
* **3** — probe could not run (missing credentials, missing resource id, HTTP/API error).

Threshold ratios 0.80 / 0.95 are the **Q15-conditional rule** (PENDING_QUESTIONS Q15) — the budget approval
is conditional on the kill-switch staying in place at these ratios, enforced by
``scripts/ci/assert_golden_cohort_kill_switch_present.py``.

Credentials (in order):

1. Environment variable ``ARCHLUCID_ARM_ACCESS_TOKEN`` or ``AZURE_MANAGEMENT_ACCESS_TOKEN`` (Bearer token for ``https://management.azure.com/``).
2. Otherwise ``az account get-access-token --resource https://management.azure.com/`` (after ``az login`` or ``azure/login`` in CI).

Subscription: ``AZURE_SUBSCRIPTION_ID`` (or parsed from the resource id).

Resource scope: full ARM id of the **Cognitive Services** account hosting the golden-cohort deployment —
``ARCHLUCID_GOLDEN_COHORT_AZURE_OPENAI_RESOURCE_ID`` (required unless simulating).

Local smoke without Azure: set ``ARCHLUCID_GOLDEN_COHORT_BUDGET_PROBE_SIMULATE_MTD_USD`` to a decimal string.
"""

from __future__ import annotations

import argparse
import json
import os
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path
from shutil import which
from typing import Any

try:
    import requests
except ImportError as exc:  # pragma: no cover - exercised when requests missing
    print("golden_cohort_budget_probe: install requests (pip install requests).", file=sys.stderr)
    raise SystemExit(3) from exc

COST_API_VERSION = "2023-11-01"
MANAGEMENT_SCOPE = "https://management.azure.com/"


def _repo_root() -> Path:
    return Path(__file__).resolve().parents[1]


def _load_config(path: Path) -> dict[str, Any]:
    if not path.is_file():
        print(f"golden_cohort_budget_probe: missing config file: {path}", file=sys.stderr)
        raise SystemExit(3)

    with path.open(encoding="utf-8") as handle:
        return json.load(handle)


def _subscription_id_from_resource(resource_id: str) -> str:
    parts = resource_id.split("/")
    try:
        idx = parts.index("subscriptions")
    except ValueError as exc:
        print(
            "golden_cohort_budget_probe: ARCHLUCID_GOLDEN_COHORT_AZURE_OPENAI_RESOURCE_ID must include /subscriptions/{guid}/...",
            file=sys.stderr,
        )
        raise SystemExit(3) from exc

    if idx + 1 >= len(parts):
        raise SystemExit(3)

    return parts[idx + 1]


def _get_management_token() -> str:
    for key in ("ARCHLUCID_ARM_ACCESS_TOKEN", "AZURE_MANAGEMENT_ACCESS_TOKEN"):
        token = os.environ.get(key, "").strip()
        if token:
            return token

    az_path = which("az")
    if az_path is None:
        print(
            "golden_cohort_budget_probe: no management token. Set ARCHLUCID_ARM_ACCESS_TOKEN, "
            "or install Azure CLI and run `az login`, or use `azure/login` in GitHub Actions before this script.",
            file=sys.stderr,
        )
        raise SystemExit(3)

    proc = subprocess.run(
        [az_path, "account", "get-access-token", "--resource", MANAGEMENT_SCOPE.rstrip("/"), "-o", "json"],
        capture_output=True,
        text=True,
        check=False,
    )
    if proc.returncode != 0:
        print(proc.stderr or proc.stdout or "az get-access-token failed", file=sys.stderr)
        raise SystemExit(3)

    try:
        payload = json.loads(proc.stdout)
    except json.JSONDecodeError:
        print("golden_cohort_budget_probe: could not parse az token JSON.", file=sys.stderr)
        raise SystemExit(3)

    token = str(payload.get("accessToken", "")).strip()
    if not token:
        print("golden_cohort_budget_probe: az returned empty accessToken.", file=sys.stderr)
        raise SystemExit(3)

    return token


def _query_mtd_actual_cost_usd(subscription_id: str, resource_id: str, token: str) -> float:
    url = (
        f"https://management.azure.com/subscriptions/{subscription_id}"
        f"/providers/Microsoft.CostManagement/query?api-version={COST_API_VERSION}"
    )
    body: dict[str, Any] = {
        "type": "ActualCost",
        "timeframe": "MonthToDate",
        "dataset": {
            "granularity": "None",
            "aggregation": {
                "totalCost": {
                    "name": "Cost",
                    "function": "Sum",
                },
            },
            "filter": {
                "dimensions": {
                    "name": "ResourceId",
                    "operator": "In",
                    "values": [resource_id],
                },
            },
        },
    }

    response = requests.post(
        url,
        headers={
            "Authorization": f"Bearer {token}",
            "Content-Type": "application/json",
        },
        json=body,
        timeout=120,
    )

    if response.status_code >= 400:
        print(
            f"golden_cohort_budget_probe: Cost Management query failed HTTP {response.status_code}: "
            f"{response.text[:800]}",
            file=sys.stderr,
        )
        raise SystemExit(3)

    data = response.json()
    return _parse_cost_usd_from_query_result(data)


def _parse_cost_usd_from_query_result(data: dict[str, Any]) -> float:
    props = data.get("properties") or {}
    columns = props.get("columns") or []
    rows = props.get("rows") or []
    if not columns or not rows:
        return 0.0

    cost_idx: int | None = None
    for i, col in enumerate(columns):
        if not isinstance(col, dict):
            continue
        name = str(col.get("name", "")).lower()
        if "cost" in name:
            cost_idx = i
            break

    if cost_idx is None:
        cost_idx = len(rows[0]) - 1 if rows and rows[0] else 0

    total = 0.0
    for row in rows:
        if not isinstance(row, (list, tuple)) or cost_idx >= len(row):
            continue
        cell = row[cost_idx]
        try:
            total += float(cell)
        except (TypeError, ValueError):
            continue

    return total


def _compute_exit_code(
    mtd_usd: float,
    monthly_budget_usd: float,
    kill_switch_percent: float,
    warn_percent: float | None = None,
) -> int:
    """Compute the kill-switch exit code given month-to-date spend and threshold ratios.

    Q15-conditional rule (PENDING_QUESTIONS Q15) — the workflow gates real-LLM execution
    on these exit codes:

      * 0 → below ``warn_percent`` of ``monthly_budget_usd``: continue normally.
      * 1 → at-or-above ``warn_percent`` and below ``kill_switch_percent``: warn + post issue,
            but the cohort run still proceeds (this is the "yellow" state the prompt explicitly
            asked for at the 80% mark).
      * 2 → at-or-above ``kill_switch_percent``: SKIP cohort run, post issue, do NOT count
            as workflow failure (this is the "red" state at the 95% mark).

    If ``warn_percent`` is ``None``, falls back to legacy single-threshold behavior so older
    callers (and the stand-alone ``ARCHLUCID_GOLDEN_COHORT_BUDGET_PROBE_SIMULATE_MTD_USD`` smoke
    path) keep working in environments that haven't migrated their config yet.
    """

    kill_threshold_usd = monthly_budget_usd * (kill_switch_percent / 100.0)
    if mtd_usd >= kill_threshold_usd:
        return 2

    if warn_percent is None:
        return 0

    warn_threshold_usd = monthly_budget_usd * (warn_percent / 100.0)
    if mtd_usd >= warn_threshold_usd:
        return 1

    return 0


def _github_run_url() -> str:
    explicit = os.environ.get("GITHUB_RUN_URL", "").strip()
    if explicit:
        return explicit

    server = os.environ.get("GITHUB_SERVER_URL", "").rstrip("/")
    repo = os.environ.get("GITHUB_REPOSITORY", "").strip()
    run = os.environ.get("GITHUB_RUN_ID", "").strip()
    if server and repo and run:
        return f"{server}/{repo}/actions/runs/{run}"

    return ""


def _append_usage_ledger(path: Path, entry: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    data: dict[str, Any]
    if path.is_file():
        with path.open(encoding="utf-8") as handle:
            data = json.load(handle)
    else:
        data = {
            "schemaVersion": 1,
            "description": (
                "Append-only ledger of golden-cohort Azure OpenAI month-to-date probe snapshots "
                "(written by scripts/golden_cohort_budget_probe.py --usage-ledger)."
            ),
            "entries": [],
        }

    entries = data.get("entries")
    if not isinstance(entries, list):
        entries = []
        data["entries"] = entries

    entries.append(entry)

    with path.open("w", encoding="utf-8") as handle:
        json.dump(data, handle, indent=2)
        handle.write("\n")


def _emit_exports(
    mtd: float,
    budget: float,
    kill_pct: float,
    exit_code: int,
    warn_pct: float | None = None,
) -> None:
    kill_threshold = budget * (kill_pct / 100.0)
    remaining = max(budget - mtd, 0.0)
    print(f"Month-to-date cost (USD): {mtd:.2f}")
    print(f"Monthly budget (USD): {budget:.2f}")
    print(f"Kill threshold at {kill_pct:g}%: {kill_threshold:.2f}")

    if warn_pct is not None:
        warn_threshold = budget * (warn_pct / 100.0)
        print(f"Warn threshold at {warn_pct:g}%: {warn_threshold:.2f}")
        print(f"EXPORT_WARN_THRESHOLD_USD={warn_threshold:.4f}")
        print(f"EXPORT_WARN_THRESHOLD_PCT={warn_pct:.4f}")

    print(f"Remaining to cap: {remaining:.2f}")
    print(f"EXPORT_MTD_USD={mtd:.4f}")
    print(f"EXPORT_BUDGET_USD={budget:.4f}")
    print(f"EXPORT_KILL_THRESHOLD_USD={kill_threshold:.4f}")
    print(f"EXPORT_KILL_THRESHOLD_PCT={kill_pct:.4f}")
    print(f"EXPORT_EXIT_CODE={exit_code}")


def main() -> int:
    parser = argparse.ArgumentParser(description="Golden cohort Azure OpenAI MTD cost probe.")
    parser.add_argument(
        "--config",
        type=Path,
        default=_repo_root() / "tests" / "golden-cohort" / "budget.config.json",
        help="Path to budget.config.json",
    )
    parser.add_argument(
        "--usage-ledger",
        type=Path,
        default=None,
        help="Optional path to tests/golden-cohort/usage-mtd.json — appends this probe run as a ledger entry.",
    )
    args = parser.parse_args()

    cfg = _load_config(args.config)
    monthly_budget = float(cfg["monthlyTokenBudgetUsd"])
    kill_pct = float(cfg["killSwitchThresholdPercent"])
    warn_pct_raw = cfg.get("warnThresholdPercent")
    warn_pct = float(warn_pct_raw) if warn_pct_raw is not None else None

    simulate = os.environ.get("ARCHLUCID_GOLDEN_COHORT_BUDGET_PROBE_SIMULATE_MTD_USD", "").strip()
    if simulate:
        mtd = float(simulate)
        code = _compute_exit_code(mtd, monthly_budget, kill_pct, warn_pct)
        _emit_exports(mtd, monthly_budget, kill_pct, code, warn_pct)
        if args.usage_ledger is not None:
            _append_usage_ledger(
                args.usage_ledger,
                {
                    "recordedUtc": datetime.now(timezone.utc).isoformat(),
                    "mtdUsd": mtd,
                    "monthlyBudgetUsd": monthly_budget,
                    "exitCode": code,
                    "source": "simulate",
                    "githubRunId": os.environ.get("GITHUB_RUN_ID", ""),
                    "githubRunUrl": _github_run_url(),
                },
            )

        return code

    resource_id = os.environ.get("ARCHLUCID_GOLDEN_COHORT_AZURE_OPENAI_RESOURCE_ID", "").strip()
    if not resource_id:
        print(
            "golden_cohort_budget_probe: set ARCHLUCID_GOLDEN_COHORT_AZURE_OPENAI_RESOURCE_ID to the full ARM id of the "
            "Cognitive Services account (e.g. /subscriptions/.../resourceGroups/.../providers/Microsoft.CognitiveServices/accounts/...).",
            file=sys.stderr,
        )
        raise SystemExit(3)

    subscription_id = os.environ.get("AZURE_SUBSCRIPTION_ID", "").strip() or _subscription_id_from_resource(resource_id)
    if not subscription_id:
        print("golden_cohort_budget_probe: could not resolve subscription id.", file=sys.stderr)
        raise SystemExit(3)

    token = _get_management_token()
    mtd = _query_mtd_actual_cost_usd(subscription_id, resource_id, token)
    code = _compute_exit_code(mtd, monthly_budget, kill_pct, warn_pct)
    _emit_exports(mtd, monthly_budget, kill_pct, code, warn_pct)
    if args.usage_ledger is not None:
        _append_usage_ledger(
            args.usage_ledger,
            {
                "recordedUtc": datetime.now(timezone.utc).isoformat(),
                "mtdUsd": mtd,
                "monthlyBudgetUsd": monthly_budget,
                "exitCode": code,
                "source": "azure-cost-management",
                "resourceIdSuffix": resource_id.split("/")[-1] if resource_id else "",
                "githubRunId": os.environ.get("GITHUB_RUN_ID", ""),
                "githubRunUrl": _github_run_url(),
            },
        )

    return code


if __name__ == "__main__":
    raise SystemExit(main())
