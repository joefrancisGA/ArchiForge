#!/usr/bin/env python3
"""Unit tests for coordinator_parity_probe helpers (stdlib only)."""
from __future__ import annotations

import sys
import tempfile
import unittest
from pathlib import Path

_ROOT = Path(__file__).resolve().parent
if str(_ROOT) not in sys.path:
    sys.path.insert(0, str(_ROOT))

import coordinator_parity_probe as probe  # noqa: E402  (after sys.path)


class CoordinatorParityProbeTests(unittest.TestCase):
    def test_format_markdown_row_uses_dash_when_none(self) -> None:
        row = probe.format_markdown_row("a", "b", None, None)
        self.assertIn("- / -", row)
        self.assertTrue(row.startswith("| "))

    def test_upsert_table_appends_within_markers(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp) / "parity.md"
            path.write_text(
                "head\n"
                f"{probe.MARKER_START}\n"
                "| Window start (UTC) | Window end (UTC) | Tenant sample | Coordinator p95 ms | Authority p95 ms | Audit rows/hr | Replay parity OK? | Notes |\n"
                "|--------------------|------------------|-----------------|----------------------|------------------|-----------------|---------------------|-------|\n"
                f"{probe.MARKER_END}\n"
                "tail\n",
                encoding="utf-8",
            )
            ok = probe.upsert_table(path, "| r1 | r2 | t | — | — | 1 / 2 / 3 | — | test |", max_rows=14)
            self.assertTrue(ok)
            text = path.read_text(encoding="utf-8")
            self.assertIn("1 / 2 / 3", text)
            self.assertIn(probe.MARKER_START, text)


if __name__ == "__main__":
    unittest.main()
