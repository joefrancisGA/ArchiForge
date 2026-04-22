#!/usr/bin/env python3
"""CI: ensure canonical procurement pack sources exist (dry-run of the builder)."""

from __future__ import annotations

import subprocess
import sys
from pathlib import Path


def main() -> int:
    root = Path(__file__).resolve().parents[2]
    script = root / "scripts" / "build_procurement_pack.py"
    r = subprocess.run(
        [sys.executable, str(script), "--dry-run"],
        cwd=str(root),
        check=False,
    )
    return int(r.returncode)


if __name__ == "__main__":
    raise SystemExit(main())
