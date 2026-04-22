"""Unit tests for assert_pgp_key_present.py (temp dirs — no repo mutation)."""

from __future__ import annotations

import importlib.util
import sys
import tempfile
import unittest
from pathlib import Path


def _load_guard():
    script_dir = Path(__file__).resolve().parent
    path = script_dir / "assert_pgp_key_present.py"
    spec = importlib.util.spec_from_file_location("assert_pgp_key_present", path)
    if spec is None or spec.loader is None:
        raise RuntimeError("cannot load assert_pgp_key_present")

    mod = importlib.util.module_from_spec(spec)
    sys.modules["assert_pgp_key_present"] = mod
    spec.loader.exec_module(mod)
    return mod


def _minimal_armored_public_key_block() -> str:
    # Valid shape for the guard (BEGIN/END + sufficient body length). Not a real OpenPGP packet set.
    inner = "\n".join(["mDMEZtestEXAMPLE1234567890ABCDEFbase64padding=="] * 4)
    return "\n".join(
        [
            "-----BEGIN PGP PUBLIC KEY BLOCK-----",
            "",
            inner,
            "",
            "-----END PGP PUBLIC KEY BLOCK-----",
            "",
        ],
    )


class TestAssertPgpKeyPresent(unittest.TestCase):
    def setUp(self) -> None:
        self._tmpdir = tempfile.TemporaryDirectory()
        self.root = Path(self._tmpdir.name)
        self.mod = _load_guard()

    def tearDown(self) -> None:
        self._tmpdir.cleanup()

    def test_silent_trust_center_skips_even_when_key_missing(self) -> None:
        trust = self.root / "TRUST_CENTER.md"
        trust.write_text("# Trust\n\nContact the security team via email for coordinated disclosure.\n", encoding="utf-8")

        self.mod.REPO_ROOT = str(self.root)
        self.mod.TRUST_CENTER_REL = "TRUST_CENTER.md"
        self.mod.PGP_KEY_REL = "archlucid-ui/public/.well-known/pgp-key.txt"

        self.assertEqual(self.mod.main(), 0)

    def test_warn_only_when_trust_mentions_pgp_key_path_and_file_missing(self) -> None:
        trust = self.root / "TRUST_CENTER.md"
        trust.write_text(
            "See **/.well-known/pgp-key.txt** for the public key.\n",
            encoding="utf-8",
        )

        self.mod.REPO_ROOT = str(self.root)
        self.mod.TRUST_CENTER_REL = "TRUST_CENTER.md"
        self.mod.PGP_KEY_REL = "archlucid-ui/public/.well-known/pgp-key.txt"

        self.assertEqual(self.mod.main(), 0)

    def test_fails_when_key_file_exists_but_empty(self) -> None:
        trust = self.root / "TRUST_CENTER.md"
        trust.write_text("PGP: `/.well-known/pgp-key.txt`\n", encoding="utf-8")
        key_path = self.root / "archlucid-ui/public/.well-known/pgp-key.txt"
        key_path.parent.mkdir(parents=True)
        key_path.write_text("", encoding="utf-8")

        self.mod.REPO_ROOT = str(self.root)
        self.mod.TRUST_CENTER_REL = "TRUST_CENTER.md"
        self.mod.PGP_KEY_REL = str(key_path.relative_to(self.root))

        self.assertEqual(self.mod.main(), 1)

    def test_fails_when_key_file_missing_end_marker(self) -> None:
        trust = self.root / "TRUST_CENTER.md"
        trust.write_text("PGP: `/.well-known/pgp-key.txt`\n", encoding="utf-8")
        key_path = self.root / "archlucid-ui/public/.well-known/pgp-key.txt"
        key_path.parent.mkdir(parents=True)
        key_path.write_text(
            "-----BEGIN PGP PUBLIC KEY BLOCK-----\n" + ("x" * 120) + "\n",
            encoding="utf-8",
        )

        self.mod.REPO_ROOT = str(self.root)
        self.mod.TRUST_CENTER_REL = "TRUST_CENTER.md"
        self.mod.PGP_KEY_REL = str(key_path.relative_to(self.root))

        self.assertEqual(self.mod.main(), 1)

    def test_passes_when_key_present_with_valid_armor_shape(self) -> None:
        trust = self.root / "TRUST_CENTER.md"
        trust.write_text("PGP: `/.well-known/pgp-key.txt`\n", encoding="utf-8")
        key_path = self.root / "archlucid-ui/public/.well-known/pgp-key.txt"
        key_path.parent.mkdir(parents=True)
        key_path.write_text(_minimal_armored_public_key_block(), encoding="utf-8")

        self.mod.REPO_ROOT = str(self.root)
        self.mod.TRUST_CENTER_REL = "TRUST_CENTER.md"
        self.mod.PGP_KEY_REL = str(key_path.relative_to(self.root))

        self.assertEqual(self.mod.main(), 0)


if __name__ == "__main__":
    unittest.main()
