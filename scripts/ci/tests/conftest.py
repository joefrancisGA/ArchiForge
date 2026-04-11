"""
Pytest bootstrap: allow `pytest scripts/ci/tests/` from repo root or `pytest tests/` from scripts/ci.

CI: dotnet-fast-core runs `cd scripts/ci && python3 -m pytest tests/ -v` (see .github/workflows/ci.yml).
"""

from __future__ import annotations

import sys
from pathlib import Path

_CI_DIR = Path(__file__).resolve().parents[1]
if str(_CI_DIR) not in sys.path:
    sys.path.insert(0, str(_CI_DIR))
