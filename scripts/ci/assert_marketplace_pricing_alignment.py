"""
assert_marketplace_pricing_alignment.py
---------------------------------------
Ensures Azure Marketplace / publication docs use the same **commercial tier names**
as the packaging table in ``docs/go-to-market/PRICING_PHILOSOPHY.md`` (Team /
Professional / Enterprise).

Run: python scripts/ci/assert_marketplace_pricing_alignment.py
Exit 0 = aligned. Exit 1 = drift or missing canonical markers.
"""

from __future__ import annotations

import os
import sys

REPO_ROOT = os.path.normpath(os.path.join(os.path.dirname(__file__), "..", ".."))

PRICING_REL = os.path.normpath("docs/go-to-market/PRICING_PHILOSOPHY.md")
MARKETPLACE_REL = os.path.normpath("docs/go-to-market/MARKETPLACE_PUBLICATION.md")
AZURE_SAAS_REL = os.path.normpath("docs/AZURE_MARKETPLACE_SAAS_OFFER.md")

# Single row from PRICING_PHILOSOPHY.md §3 "Tier overview" — do not drift without updating this script + docs.
CANONICAL_PACKAGING_ROW = "| **Team** | **Professional** | **Enterprise** |"

# Publication checklist step 1 — explicit backtick tier triple (see MARKETPLACE_PUBLICATION.md).
MARKETPLACE_PLAN_TRIPLE = "`Team` / `Professional` / `Enterprise`"


def _read(rel_path: str) -> str:
    path = os.path.join(REPO_ROOT, rel_path)
    if not os.path.isfile(path):
        print(f"assert_marketplace_pricing_alignment: missing file: {rel_path}", file=sys.stderr)
        sys.exit(1)

    with open(path, encoding="utf-8") as handle:
        return handle.read()


def main() -> int:
    pricing = _read(PRICING_REL)

    if CANONICAL_PACKAGING_ROW not in pricing:
        print(
            "assert_marketplace_pricing_alignment: PRICING_PHILOSOPHY.md is missing the canonical "
            f"packaging row:\n  {CANONICAL_PACKAGING_ROW}",
            file=sys.stderr,
        )

        return 1

    marketplace_pub = _read(MARKETPLACE_REL)

    if MARKETPLACE_PLAN_TRIPLE not in marketplace_pub:
        print(
            "assert_marketplace_pricing_alignment: MARKETPLACE_PUBLICATION.md must reference "
            f"the tier triple exactly:\n  {MARKETPLACE_PLAN_TRIPLE}",
            file=sys.stderr,
        )

        return 1

    azure_saas = _read(AZURE_SAAS_REL)

    if "`Pro`" in azure_saas:
        print(
            "assert_marketplace_pricing_alignment: AZURE_MARKETPLACE_SAAS_OFFER.md must not use "
            "`Pro` as a tier name; use `Professional` to match PRICING_PHILOSOPHY.md.",
            file=sys.stderr,
        )

        return 1

    if "Professional" not in azure_saas:
        print(
            "assert_marketplace_pricing_alignment: AZURE_MARKETPLACE_SAAS_OFFER.md must mention "
            "Professional (Partner Center plan naming).",
            file=sys.stderr,
        )

        return 1

    print("assert_marketplace_pricing_alignment: OK")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
