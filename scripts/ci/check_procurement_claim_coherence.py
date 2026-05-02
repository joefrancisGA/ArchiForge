#!/usr/bin/env python3
"""Check high-risk procurement claim coherence across buyer-facing documents."""

from __future__ import annotations

import sys
from pathlib import Path


def repo_root() -> Path:
    return Path(__file__).resolve().parents[2]


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8", errors="replace")


def main() -> int:
    root = repo_root()
    docs = [
        root / "docs" / "go-to-market" / "TRUST_CENTER.md",
        root / "docs" / "go-to-market" / "CURRENT_ASSURANCE_POSTURE.md",
        root / "docs" / "go-to-market" / "PROCUREMENT_FAQ.md",
        root / "docs" / "go-to-market" / "SOC2_STATUS_PROCUREMENT.md",
    ]

    missing = [d for d in docs if not d.is_file()]
    if missing:
        for m in missing:
            print(f"ERROR missing required doc: {m.relative_to(root).as_posix()}", file=sys.stderr)
        return 1

    errors: list[str] = []
    for d in docs:
        text = read(d)
        rel = d.relative_to(root).as_posix()
        if "ASSURANCE_STATUS_CANONICAL.md" not in text:
            errors.append(f"{rel}: missing ASSURANCE_STATUS_CANONICAL.md reference")
        if "SOC 2 Type II" in text and "Not yet issued" not in text and "not currently issued" not in text:
            errors.append(f"{rel}: SOC2 Type II wording is missing non-issued statement")
        if "third-party" in text.lower() and "in-flight" in text.lower():
            errors.append(f"{rel}: uses 'in-flight' for third-party assurance wording")

    if errors:
        print("Procurement claim coherence FAILED:", file=sys.stderr)
        for error in errors:
            print(f"  - {error}", file=sys.stderr)
        return 1

    print("Procurement claim coherence OK")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

