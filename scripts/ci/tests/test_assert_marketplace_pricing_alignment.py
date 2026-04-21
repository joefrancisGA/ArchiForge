"""Tests for assert_marketplace_pricing_alignment.py."""

from __future__ import annotations

import assert_marketplace_pricing_alignment as sut


def test_main_succeeds_on_real_repo_layout() -> None:
    assert sut.main() == 0
