"""Unit tests for assert_layered_audit_wiring_echo.py."""

from __future__ import annotations

import contextlib
import io
import pathlib
import tempfile
import unittest

import assert_layered_audit_wiring_echo as sut


class AssertLayeredAuditWiringEchoTests(unittest.TestCase):
    def test_script_ok_on_real_repo(self) -> None:

        stderr = io.StringIO()
        stdout = io.StringIO()

        with contextlib.redirect_stderr(stderr), contextlib.redirect_stdout(stdout):

            code = sut.main([])

        self.assertEqual(code, 0, msg=stderr.getvalue())

    def test_repo_with_no_targets_fails(self) -> None:

        stderr = io.StringIO()
        stdout = io.StringIO()

        with tempfile.TemporaryDirectory() as tmp:

            root = pathlib.Path(tmp)

            with contextlib.redirect_stderr(stderr), contextlib.redirect_stdout(stdout):

                code = sut.main(["--root", str(root)])

        self.assertEqual(code, 1, msg=stderr.getvalue())


if __name__ == "__main__":
    unittest.main()
