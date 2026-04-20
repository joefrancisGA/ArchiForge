"""Unit tests for scripts/ci/backfill_doc_scope_headers.py."""

from __future__ import annotations

import tempfile
import unittest
from pathlib import Path

import backfill_doc_scope_headers as subject


class BackfillDocScopeHeadersTests(unittest.TestCase):
    def test_build_scope_line_uses_first_heading(self) -> None:
        rel = Path("API_CONTRACTS.md")
        content = "# API contracts\n\nBody.\n"
        line = subject.build_scope_line(rel, content)

        self.assertTrue(line.startswith("> **Scope:**"))
        self.assertIn("API contracts", line)

    def test_build_scope_line_fallback_when_no_heading(self) -> None:
        rel = Path("foo") / "bar.md"
        content = "no heading here\n"
        line = subject.build_scope_line(rel, content)

        self.assertIn("foo", line)
        self.assertIn("bar", line)

    def test_prepend_scope_preserves_bom(self) -> None:
        scope = "> **Scope:** Test."
        inner = "# Title\n"
        content = "\ufeff" + inner

        out = subject.prepend_scope(content, scope)

        self.assertTrue(out.startswith("\ufeff" + scope))

    def test_prepend_scope_no_bom(self) -> None:
        scope = "> **Scope:** Test."
        content = "# Title\n"

        out = subject.prepend_scope(content, scope)

        self.assertTrue(out.startswith(scope + "\n\n# Title"))


if __name__ == "__main__":
    unittest.main()
