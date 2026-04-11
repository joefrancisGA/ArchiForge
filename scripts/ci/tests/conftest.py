"""Pytest configuration: ensure sibling CI scripts are importable as top-level modules."""

from __future__ import annotations

import sys
from pathlib import Path

_CI_ROOT = Path(__file__).resolve().parent.parent
if str(_CI_ROOT) not in sys.path:
    sys.path.insert(0, str(_CI_ROOT))
