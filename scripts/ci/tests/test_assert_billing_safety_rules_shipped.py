"""Self-test for ``scripts/ci/assert_billing_safety_rules_shipped.py``.

Asserts the guard catches each weakening / removal it is meant to defend
against, using fixture files in a temporary directory (never modifies the
real repo). Verifies:

    * Passing baseline: real BillingProductionSafetyRules wiring is shipped.
    * Failure when the rules file is missing entirely.
    * Failure when one of the three required methods is removed.
    * Failure when the ``sk_live_`` literal is removed (live-key check
      effectively no-ops).
    * Failure when the GA offer-id error message is removed.
    * Failure when ``ArchLucidConfigurationRules`` no longer references the
      class (so the checks would not run on boot).
"""

from __future__ import annotations

import subprocess
import sys
import tempfile
import textwrap
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
SCRIPT = REPO_ROOT / "scripts" / "ci" / "assert_billing_safety_rules_shipped.py"


VALID_RULES_CS = textwrap.dedent(
    """\
    namespace Foo;

    internal static class BillingProductionSafetyRules
    {
        public static void CollectStripeLiveKeyRequiresWebhookSigningSecret(object cfg, System.Collections.Generic.List<string> errors)
        {
            // sk_live_ live-key check
            // Billing:Stripe:WebhookSigningSecret remediation hint
            errors.Add("Billing:Stripe:WebhookSigningSecret missing for sk_live_ key");
        }

        public static void CollectAzureMarketplaceLandingPageUrl(object cfg, System.Collections.Generic.List<string> errors)
        {
            errors.Add("Billing:AzureMarketplace:LandingPageUrl required");
        }

        public static void CollectAzureMarketplaceGaRequiresOfferId(object cfg, System.Collections.Generic.List<string> errors)
        {
            errors.Add("Billing:AzureMarketplace:MarketplaceOfferId required");
        }
    }
    """
)

VALID_CONFIG_RULES_CS = textwrap.dedent(
    """\
    namespace Foo;

    internal static class ArchLucidConfigurationRules
    {
        public static void Run()
        {
            BillingProductionSafetyRules.CollectStripeLiveKeyRequiresWebhookSigningSecret(null!, new());
            BillingProductionSafetyRules.CollectAzureMarketplaceLandingPageUrl(null!, new());
            BillingProductionSafetyRules.CollectAzureMarketplaceGaRequiresOfferId(null!, new());
        }
    }
    """
)


def _write(path: Path, contents: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(contents, encoding="utf-8")


def _run_script(rules_path: Path, config_rules_path: Path) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        [
            sys.executable,
            str(SCRIPT),
            "--rules-path",
            str(rules_path),
            "--config-rules-path",
            str(config_rules_path),
        ],
        capture_output=True,
        text=True,
        check=False,
    )


class TestAssertBillingSafetyRulesShipped(unittest.TestCase):
    def test_passes_against_real_repo(self) -> None:
        result = subprocess.run(
            [sys.executable, str(SCRIPT)],
            capture_output=True,
            text=True,
            check=False,
        )

        self.assertEqual(result.returncode, 0, msg=result.stdout + result.stderr)

    def test_passes_on_valid_fixtures(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            rules = root / "rules.cs"
            cfg = root / "config.cs"
            _write(rules, VALID_RULES_CS)
            _write(cfg, VALID_CONFIG_RULES_CS)

            result = _run_script(rules, cfg)

            self.assertEqual(result.returncode, 0, msg=result.stdout + result.stderr)

    def test_fails_when_rules_file_missing(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            rules = root / "missing.cs"
            cfg = root / "config.cs"
            _write(cfg, VALID_CONFIG_RULES_CS)

            result = _run_script(rules, cfg)

            self.assertNotEqual(result.returncode, 0)
            self.assertIn("BillingProductionSafetyRules source missing", result.stderr)

    def test_fails_when_required_method_removed(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            rules = root / "rules.cs"
            cfg = root / "config.cs"
            weakened = VALID_RULES_CS.replace("CollectAzureMarketplaceGaRequiresOfferId", "DeletedThisCheck")
            _write(rules, weakened)
            _write(cfg, VALID_CONFIG_RULES_CS)

            result = _run_script(rules, cfg)

            self.assertNotEqual(result.returncode, 0)
            self.assertIn("CollectAzureMarketplaceGaRequiresOfferId", result.stderr)

    def test_fails_when_sk_live_literal_removed(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            rules = root / "rules.cs"
            cfg = root / "config.cs"
            weakened = VALID_RULES_CS.replace("sk_live_", "sk_test_")
            _write(rules, weakened)
            _write(cfg, VALID_CONFIG_RULES_CS)

            result = _run_script(rules, cfg)

            self.assertNotEqual(result.returncode, 0)
            self.assertIn("sk_live_", result.stderr)

    def test_fails_when_ga_offer_id_error_removed(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            rules = root / "rules.cs"
            cfg = root / "config.cs"
            weakened = VALID_RULES_CS.replace("Billing:AzureMarketplace:MarketplaceOfferId", "Billing:Other:Field")
            _write(rules, weakened)
            _write(cfg, VALID_CONFIG_RULES_CS)

            result = _run_script(rules, cfg)

            self.assertNotEqual(result.returncode, 0)
            self.assertIn("MarketplaceOfferId", result.stderr)

    def test_fails_when_configuration_rules_no_longer_references_class(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            rules = root / "rules.cs"
            cfg = root / "config.cs"
            _write(rules, VALID_RULES_CS)
            _write(cfg, "namespace Foo; internal static class ArchLucidConfigurationRules { public static void Run() {} }\n")

            result = _run_script(rules, cfg)

            self.assertNotEqual(result.returncode, 0)
            self.assertIn("BillingProductionSafetyRules", result.stderr)
