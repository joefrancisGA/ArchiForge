"""Tests for assert_rollback_scripts_exist.py."""

from __future__ import annotations

import assert_rollback_scripts_exist as sut


def test_main_succeeds_on_real_repo_layout() -> None:
    assert sut.main() == 0
