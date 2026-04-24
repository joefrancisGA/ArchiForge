#!/usr/bin/env python3
"""
Merge-blocking guard: BillingProductionSafetyRules must keep its three production-only checks.

ArchLucid is on the path to V1 with a sales-led commercial motion (live keys
deferred to V1.1 per owner Q17, 2026-04-23). The
``BillingProductionSafetyRules`` class is the last line of defence that
prevents the API from booting in Production with an unsafe billing
configuration — most importantly, it refuses to start when:

    1. ``Billing:Stripe:SecretKey`` starts with ``sk_live_`` and there is no
       Stripe webhook signing secret (unsigned webhooks → unverifiable
       events → revenue + audit risk).
    2. ``Billing:Provider`` is ``AzureMarketplace`` without a public HTTPS
       Marketplace landing page (Partner Center cannot reach loopback /
       missing landing pages → broken checkout).
    3. ``Billing:AzureMarketplace:GaEnabled=true`` is set without a Partner
       Center offer id (GA mutations against a missing offer → support
       escalations + revenue leakage).

If any of those checks were silently removed or weakened — even by a
well-intentioned refactor — the Production boot would succeed in unsafe
states. The cost of that failure mode is enough to keep an explicit static
guard around the file. This script enforces:

    A. The file exists.
    B. All three method names are present (``CollectStripeLiveKeyRequiresWebhookSigningSecret``,
       ``CollectAzureMarketplaceLandingPageUrl``, ``CollectAzureMarketplaceGaRequiresOfferId``).
    C. The `sk_live_` prefix string literal is present.
    D. The Marketplace landing-page error messages are present.
    E. The GA offer-id error message is present.
    F. The class is referenced by ``ArchLucidConfigurationRules`` (so the
       checks actually run during boot validation).

The check only inspects source text — no compilation needed. This keeps the
guard runnable from any CI environment that has Python 3.12.

Self-test: ``scripts/ci/tests/test_assert_billing_safety_rules_shipped.py``.
The script supports ``--rules-path`` and ``--config-rules-path`` overrides so
the unit test can point it at a fixture tree.
"""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]
DEFAULT_RULES_PATH = (
    REPO_ROOT / "ArchLucid.Host.Core" / "Startup" / "Validation" / "Rules" / "BillingProductionSafetyRules.cs"
)
DEFAULT_CONFIG_RULES_PATH = (
    REPO_ROOT / "ArchLucid.Host.Core" / "Startup" / "Validation" / "ArchLucidConfigurationRules.cs"
)

REQUIRED_METHOD_NAMES: tuple[str, ...] = (
    "CollectStripeLiveKeyRequiresWebhookSigningSecret",
    "CollectAzureMarketplaceLandingPageUrl",
    "CollectAzureMarketplaceGaRequiresOfferId",
)

REQUIRED_LITERALS: tuple[tuple[str, str], ...] = (
    ("sk_live_", "Stripe live-key prefix check (live keys without webhook signing secret) is missing."),
    (
        "Billing:Stripe:WebhookSigningSecret",
        "Stripe live-key check no longer mentions WebhookSigningSecret in its error message — the operator-facing remediation hint was lost.",
    ),
    (
        "Billing:AzureMarketplace:LandingPageUrl",
        "Marketplace landing-page check is missing — Partner Center cannot reach a loopback host without it.",
    ),
    (
        "Billing:AzureMarketplace:MarketplaceOfferId",
        "Marketplace GA offer-id check is missing — GA-enabled offers without a Partner Center offer id will silently break checkout.",
    ),
)


def check_rules_file(rules_path: Path) -> list[str]:
    failures: list[str] = []

    if not rules_path.is_file():
        failures.append(f"BillingProductionSafetyRules source missing at {rules_path}")
        return failures

    text = rules_path.read_text(encoding="utf-8")

    for method in REQUIRED_METHOD_NAMES:
        if method not in text:
            failures.append(f"BillingProductionSafetyRules is missing required method '{method}'.")

    for literal, message in REQUIRED_LITERALS:
        if literal not in text:
            failures.append(f"BillingProductionSafetyRules is missing required literal '{literal}': {message}")

    return failures


def check_wired_into_configuration_rules(config_rules_path: Path) -> list[str]:
    if not config_rules_path.is_file():
        return [f"ArchLucidConfigurationRules source missing at {config_rules_path}"]

    text = config_rules_path.read_text(encoding="utf-8")

    if "BillingProductionSafetyRules" not in text:
        return [
            "ArchLucidConfigurationRules no longer references BillingProductionSafetyRules — "
            "the production-only Stripe live-key + Marketplace checks would silently stop running on boot."
        ]

    return []


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--rules-path", type=Path, default=DEFAULT_RULES_PATH)
    parser.add_argument("--config-rules-path", type=Path, default=DEFAULT_CONFIG_RULES_PATH)
    args = parser.parse_args(argv)

    failures: list[str] = []
    failures.extend(check_rules_file(args.rules_path))
    failures.extend(check_wired_into_configuration_rules(args.config_rules_path))

    if not failures:
        return 0

    for msg in failures:
        print(f"assert_billing_safety_rules_shipped: {msg}", file=sys.stderr)

    return 1


if __name__ == "__main__":
    raise SystemExit(main())
