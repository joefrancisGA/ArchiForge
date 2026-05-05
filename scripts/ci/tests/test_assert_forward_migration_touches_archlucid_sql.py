"""Tests for assert_forward_migration_touches_archlucid_sql."""

from __future__ import annotations

import sys
import unittest
from pathlib import Path

_CI_DIR = Path(__file__).resolve().parents[1]

if str(_CI_DIR) not in sys.path:
    sys.path.insert(0, str(_CI_DIR))

import assert_forward_migration_touches_archlucid_sql as sut


class TestForwardMigrationTouchesArchlucidSql(unittest.TestCase):
    def test_evaluate_no_migrations_skips(self) -> None:
        code, msg = sut.evaluate_changed_paths(
            [
                "ArchLucid.Persistence/Migrations/Rollback/R140_X.sql",
                "foo.cs",
            ],
        )

        self.assertEqual(code, 0)
        self.assertIn("skipped", msg.lower())

    def test_evaluate_forward_without_sql_fails(self) -> None:
        code, msg = sut.evaluate_changed_paths(
            [
                "ArchLucid.Persistence/Migrations/142_PilotCloseouts.sql",
            ],
        )

        self.assertEqual(code, 1)
        self.assertIn("142_PilotCloseouts.sql", msg)
        self.assertIn(sut.ARCHLUCID_SQL_PATH, msg)

    def test_evaluate_forward_with_sql_ok(self) -> None:
        code, msg = sut.evaluate_changed_paths(
            [
                "ArchLucid.Persistence/Migrations/142_PilotCloseouts.sql",
                sut.ARCHLUCID_SQL_PATH,
            ],
        )

        self.assertEqual(code, 0)
        self.assertIn("142_PilotCloseouts.sql", msg)

    def test_should_skip_github_new_branch_push_before(self) -> None:
        self.assertTrue(
            sut._should_skip_push_range(
                "0000000000000000000000000000000000000000...abc123def4567890abc123def4567890abc123de",
            ),
        )

    def test_should_not_skip_normal_range(self) -> None:
        self.assertFalse(
            sut._should_skip_push_range(
                "a000000000000000000000000000000000000000...b000000000000000000000000000000000000000",
            ),
        )

    def test_normalize_path_and_pattern(self) -> None:
        self.assertTrue(sut.is_forward_dbup_migration_path(r"ArchLucid.Persistence\Migrations\099_Foo.sql"))
        self.assertFalse(sut.is_forward_dbup_migration_path("ArchLucid.Persistence/Migrations/Baseline/000_Baseline.sql"))
        self.assertFalse(sut.is_forward_dbup_migration_path("ArchLucid.Persistence/Migrations/Rollback/R099_Foo.sql"))
