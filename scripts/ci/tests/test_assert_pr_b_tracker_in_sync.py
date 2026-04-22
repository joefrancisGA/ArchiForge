"""Tests for assert_pr_b_tracker_in_sync.py."""

from __future__ import annotations

import importlib.util
import unittest
from pathlib import Path


def _load_module():
    path = Path(__file__).resolve().parents[1] / "assert_pr_b_tracker_in_sync.py"
    spec = importlib.util.spec_from_file_location("assert_pr_b_tracker_in_sync", path)
    if spec is None or spec.loader is None:
        raise RuntimeError("Could not load assert_pr_b_tracker_in_sync.py")
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


class TestPrBTrackerSync(unittest.TestCase):
    def test_compare_equal_lists(self) -> None:
        mod = _load_module()
        a = ["One", "Two"]
        drift, contradictions, bad = mod.compare_checklists(a, a)
        self.assertFalse(drift)
        self.assertFalse(contradictions)
        self.assertFalse(bad)

    def test_compare_whitespace_drift_only(self) -> None:
        mod = _load_module()
        drift, contradictions, bad = mod.compare_checklists(["A  b"], ["A b"])
        self.assertTrue(drift)
        self.assertFalse(contradictions)
        self.assertFalse(bad)

    def test_compare_order_drift(self) -> None:
        mod = _load_module()
        drift, contradictions, bad = mod.compare_checklists(["A", "B"], ["B", "A"])
        self.assertTrue(drift)
        self.assertFalse(contradictions)
        self.assertFalse(bad)

    def test_compare_contradiction_extra_in_tracker(self) -> None:
        mod = _load_module()
        drift, contradictions, bad = mod.compare_checklists(["A"], ["A", "B"])
        self.assertFalse(drift)
        self.assertTrue(contradictions)
        self.assertTrue(bad)

    def test_extract_checklist_under_heading(self) -> None:
        mod = _load_module()
        text = (
            "### Lifecycle\n\n"
            f"{mod.ADR_SECTION_HEADING}\n\n"
            "- [ ] First item.\n"
            "- [x] Second item.\n\n"
            "| Event | Action |\n"
        )
        labels = mod.extract_checklist_under_heading(text, mod.ADR_SECTION_HEADING)
        self.assertEqual(labels, ["First item.", "Second item."])

    def test_main_succeeds_on_repo_files(self) -> None:
        mod = _load_module()
        self.assertEqual(mod.main(), 0)


if __name__ == "__main__":
    unittest.main()
