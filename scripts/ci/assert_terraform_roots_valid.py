#!/usr/bin/env python3
"""
CI guard: run the SaaS Terraform validation PowerShell suite (init/validate + config consistency).
Requires `terraform` on PATH and `pwsh` (PowerShell 7+). No Azure credentials.
"""
from __future__ import annotations

import os
import subprocess
import sys
from pathlib import Path


def _repo_root() -> Path:
    return Path(__file__).resolve().parent.parent.parent


def _run_pwsh(cwd: Path, script: Path) -> int:
    pwsh = os.environ.get("PWSH", "pwsh")
    r = subprocess.run(
        [pwsh, "-NoProfile", "-NonInteractive", "-File", str(script)],
        cwd=cwd,
        check=False,
    )
    return int(r.returncode)


def main() -> int:
    root = _repo_root()
    scripts = [
        root / "scripts" / "validate-saas-infra.ps1",
        root / "scripts" / "validate-saas-config-consistency.ps1",
    ]
    for s in scripts:
        if not s.is_file():
            print(f"Missing script: {s}", file=sys.stderr)
            return 2
        code = _run_pwsh(root, s)
        if code != 0:
            print(f"FAILED: {s.name} exit {code}", file=sys.stderr)
            return code
    print("OK: all Terraform validation scripts passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
