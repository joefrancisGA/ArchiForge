#!/usr/bin/env python3
"""
CI guard: context-ingestion must stay on explicit connector and document-parser pipelines.

Fails if ArchiForge.Api registers IContextConnector directly (bypasses ordered IEnumerable factory)
or if ServiceCollectionExtensions drops the ordered factory calls.
"""
from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
EXTENSIONS = ROOT / "ArchiForge.Api" / "Startup" / "ServiceCollectionExtensions.cs"


def main() -> int:
    if not EXTENSIONS.is_file():
        print(f"Missing {EXTENSIONS}", file=sys.stderr)
        return 2

    text = EXTENSIONS.read_text(encoding="utf-8")

    if re.search(r"AddSingleton\s*<\s*IContextConnector\b", text):
        print(
            "Forbidden: AddSingleton<IContextConnector> — use concrete connectors + "
            "ContextConnectorPipeline.CreateOrderedContextConnectorPipeline only.",
            file=sys.stderr,
        )
        return 1

    if "CreateOrderedContextConnectorPipeline" not in text:
        print(
            "Expected CreateOrderedContextConnectorPipeline in ServiceCollectionExtensions.",
            file=sys.stderr,
        )
        return 1

    if "CreateOrderedContextDocumentParsers" not in text:
        print(
            "Expected CreateOrderedContextDocumentParsers in ServiceCollectionExtensions.",
            file=sys.stderr,
        )
        return 1

    print("context ingestion DI guards: ok")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
