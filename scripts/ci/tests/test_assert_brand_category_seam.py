"""Self-test for `scripts/ci/assert_brand_category_seam.py`.

Asserts the script:

    1. Passes (exit 0) on a fixture tree that contains:
       - the seam file,
       - a scoped file with the legacy string AND `BRAND_CATEGORY_LEGACY` import
         (the SEO escape hatch),
       - a scoped file that uses BRAND_CATEGORY only (no legacy string).
    2. WARN-mode (default) exits 0 even when offenders exist, but lists each
       offender on stderr.
    3. FAIL-mode (--fail) exits non-zero when offenders exist.
    4. Exits non-zero in any mode when the seam file itself is missing.

Self-test materializes minimal fixtures on a `tempfile.TemporaryDirectory` and
points the script at them via the `--repo-root` override — never modifies the
real repo.
"""

from __future__ import annotations

import subprocess
import sys
import tempfile
import textwrap
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
SCRIPT = REPO_ROOT / "scripts" / "ci" / "assert_brand_category_seam.py"


SEAM_CONTENTS = textwrap.dedent(
    """\
    export const BRAND_CATEGORY = "AI Architecture Review Board";
    export const BRAND_CATEGORY_LEGACY = "AI Architecture Intelligence";
    """
)


def _write(path: Path, contents: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(contents, encoding="utf-8")


def _make_seam(repo_root: Path) -> None:
    _write(
        repo_root / "archlucid-ui" / "src" / "lib" / "brand-category.ts",
        SEAM_CONTENTS,
    )


def _run(repo_root: Path, *extra_args: str) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        [sys.executable, str(SCRIPT), "--repo-root", str(repo_root), *extra_args],
        capture_output=True,
        text=True,
        check=False,
    )


class TestAssertBrandCategorySeam(unittest.TestCase):
    def test_passes_when_no_offenders(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            _make_seam(root)
            _write(
                root / "archlucid-ui" / "src" / "app" / "(marketing)" / "why" / "page.tsx",
                'import { BRAND_CATEGORY } from "@/lib/brand-category";\nexport default function Page() { return <p>{BRAND_CATEGORY} platform</p>; }\n',
            )

            result = _run(root)

            self.assertEqual(result.returncode, 0, msg=result.stdout + result.stderr)
            self.assertIn("OK", result.stdout)

    def test_passes_when_legacy_used_with_escape_hatch(self) -> None:
        """A scoped file may legitimately reference the legacy string IF it
        also imports `BRAND_CATEGORY_LEGACY` (the SEO redirect escape hatch)."""

        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            _make_seam(root)
            _write(
                root / "archlucid-ui" / "src" / "app" / "(marketing)" / "why" / "page.tsx",
                'import { BRAND_CATEGORY_LEGACY } from "@/lib/brand-category";\n'
                'export const meta = { other: { "x-legacy": BRAND_CATEGORY_LEGACY } };\n'
                "// historical phrase: AI Architecture Intelligence\n",
            )

            result = _run(root)

            self.assertEqual(result.returncode, 0, msg=result.stdout + result.stderr)

    def test_warn_mode_exits_zero_but_lists_offenders(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            _make_seam(root)
            _write(
                root / "archlucid-ui" / "src" / "app" / "(marketing)" / "welcome" / "page.tsx",
                'export const metadata = { description: "ArchLucid AI Architecture Intelligence — trial signup." };\n',
            )

            result = _run(root)

            self.assertEqual(result.returncode, 0)
            self.assertIn("WARN", result.stderr)
            self.assertIn("(marketing)/welcome/page.tsx", result.stderr)

    def test_fail_mode_exits_nonzero_on_offender(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            _make_seam(root)
            _write(
                root / "docs" / "go-to-market" / "COMPETITIVE_LANDSCAPE.md",
                "# Landscape\n\nArchLucid is an AI Architecture Intelligence platform.\n",
            )

            result = _run(root, "--fail")

            self.assertNotEqual(result.returncode, 0)
            self.assertIn("FAIL", result.stderr)
            self.assertIn("docs/go-to-market/COMPETITIVE_LANDSCAPE.md", result.stderr)

    def test_brief_md_under_templates_is_in_scope(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            _make_seam(root)
            _write(
                root / "templates" / "briefs" / "financial-services" / "brief.md",
                "# FS brief\n\nArchLucid is an AI Architecture Intelligence platform for FS.\n",
            )

            result = _run(root, "--fail")

            self.assertNotEqual(result.returncode, 0)
            self.assertIn("templates/briefs/financial-services/brief.md", result.stderr)

    def test_seam_itself_is_always_allow_listed(self) -> None:
        """The seam file MUST contain the legacy string (it's the export site).
        It must not be flagged as an offender."""

        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            _make_seam(root)

            result = _run(root, "--fail")

            self.assertEqual(result.returncode, 0, msg=result.stdout + result.stderr)

    def test_exits_nonzero_when_seam_missing(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)

            result = _run(root)

            self.assertNotEqual(result.returncode, 0)
            self.assertIn("seam file missing", result.stderr)
