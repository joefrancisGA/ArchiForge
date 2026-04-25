#!/usr/bin/env python3
"""
Fail when templates under templates/integrations/ reference a CloudEvent type
not listed in schemas/integration-events/catalog.json, or omit required doc links.

Recipes are expected to cite the catalog and INTEGRATION_EVENTS_AND_WEBHOOKS.md
using the path substrings checked below so links stay repo-relative and greppable.
"""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CATALOG = ROOT / "schemas" / "integration-events" / "catalog.json"
RECIPES_DIR = ROOT / "templates" / "integrations"

# CloudEvent type literals (catalog uses com.archlucid.*).
TYPE_RE = re.compile(r"\bcom\.archlucid\.[a-z0-9][a-z0-9._-]*\b", re.IGNORECASE)

REQUIRED_SUBSTRINGS = (
    "schemas/integration-events/catalog.json",
    "docs/library/INTEGRATION_EVENTS_AND_WEBHOOKS.md",
)


def load_catalog_event_types() -> set[str]:
    data = json.loads(CATALOG.read_text(encoding="utf-8"))
    return {entry["eventType"].lower() for entry in data["events"]}


def scan_recipe_markdown(text: str, known: set[str]) -> list[str]:
    """Return human-readable errors for one markdown file body."""
    errors: list[str] = []

    for hint in REQUIRED_SUBSTRINGS:
        if hint not in text:
            errors.append(f"missing required path substring {hint!r}")

    for match in TYPE_RE.finditer(text):
        token = match.group(0).lower()
        if token not in known:
            errors.append(f"unknown CloudEvent type {match.group(0)!r} (not in catalog.json)")

    return errors


def main() -> int:
    if not CATALOG.is_file():
        print(f"ERROR: missing catalog at {CATALOG}", file=sys.stderr)
        return 2

    if not RECIPES_DIR.is_dir():
        print(f"ERROR: missing recipes directory at {RECIPES_DIR}", file=sys.stderr)
        return 2

    known = load_catalog_event_types()
    failures: list[str] = []

    for md in sorted(RECIPES_DIR.rglob("*.md")):
        text = md.read_text(encoding="utf-8")
        rel = md.relative_to(ROOT).as_posix()
        for err in scan_recipe_markdown(text, known):
            failures.append(f"{rel}: {err}")

    if failures:
        print("Integration recipe alignment failures:", file=sys.stderr)
        for line in failures:
            print(f"  {line}", file=sys.stderr)
        return 1

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
