"""Self-test for `scripts/ci/assert_buyer_first_30_minutes_in_sync.py`.

Asserts the script catches both divergence cases:

    1. Vertical-picker labels diverge from `templates/briefs/` folder slugs.
    2. A buyer-facing prose paragraph lacks the q35 placeholder marker AND is
       not on the script's small allow-list.

The test materializes minimal fixtures on a `tempfile.TemporaryDirectory`
and points the script at them via the `--md-path`, `--page-path`,
`--verticals-ts`, and `--briefs-dir` overrides — never modifies the real repo.
"""

from __future__ import annotations

import subprocess
import sys
import tempfile
import textwrap
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
SCRIPT = REPO_ROOT / "scripts" / "ci" / "assert_buyer_first_30_minutes_in_sync.py"


def _write(path: Path, contents: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(contents, encoding="utf-8")


def _make_briefs_dir(root: Path, slugs: list[str]) -> Path:
    briefs = root / "briefs"
    briefs.mkdir(parents=True, exist_ok=True)

    for slug in slugs:
        (briefs / slug).mkdir(parents=True, exist_ok=True)
        (briefs / slug / "brief.md").write_text("stub", encoding="utf-8")

    return briefs


def _make_verticals_ts(root: Path, slugs: list[str]) -> Path:
    body = ",\n".join(f'  "{s}"' for s in slugs)
    contents = textwrap.dedent(
        f"""\
        export const BUYER_GET_STARTED_VERTICAL_SLUGS = [
        {body}
        ] as const;
        """
    )
    path = root / "get-started-verticals.ts"
    _write(path, contents)
    return path


VALID_MD = textwrap.dedent(
    """\
    > **Scope:** test fixture.

    # Buyer first 30 minutes (fixture)

    ArchLucid is a SaaS product. You will not install anything to evaluate it.

    Step body. <<placeholder copy — replace before external use>>
    """
)

VALID_PAGE_TSX = textwrap.dedent(
    """\
    export default function Page() {
      const STEPS = [
        { n: 1, body: `Some prose. <<placeholder copy — replace before external use>>` },
      ];

      return (
        <main>
          <h1>Your first 30 minutes with ArchLucid</h1>
          <p>ArchLucid is a SaaS product. Nothing on this page asks you to install Docker, SQL Server, .NET, Node, Terraform, or a CLI.</p>
        </main>
      );
    }
    """
)


def _run_script(
    md_path: Path, page_path: Path, verticals_ts: Path, briefs_dir: Path
) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        [
            sys.executable,
            str(SCRIPT),
            "--md-path",
            str(md_path),
            "--page-path",
            str(page_path),
            "--verticals-ts",
            str(verticals_ts),
            "--briefs-dir",
            str(briefs_dir),
        ],
        capture_output=True,
        text=True,
        check=False,
    )


class TestAssertBuyerFirst30MinutesInSync(unittest.TestCase):
    def test_passes_on_valid_fixtures(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            md_path = root / "BUYER.md"
            page_path = root / "page.tsx"
            slugs = ["financial-services", "healthcare"]
            _write(md_path, VALID_MD)
            _write(page_path, VALID_PAGE_TSX)
            verticals_ts = _make_verticals_ts(root, slugs)
            briefs_dir = _make_briefs_dir(root, slugs)

            result = _run_script(md_path, page_path, verticals_ts, briefs_dir)

            self.assertEqual(result.returncode, 0, msg=result.stdout + result.stderr)

    def test_fails_when_picker_labels_diverge(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            md_path = root / "BUYER.md"
            page_path = root / "page.tsx"
            _write(md_path, VALID_MD)
            _write(page_path, VALID_PAGE_TSX)
            verticals_ts = _make_verticals_ts(root, ["financial-services", "healthcare"])
            briefs_dir = _make_briefs_dir(root, ["financial-services", "healthcare", "retail"])

            result = _run_script(md_path, page_path, verticals_ts, briefs_dir)

            self.assertNotEqual(result.returncode, 0)
            self.assertIn("vertical picker missing slugs", result.stderr)

    def test_fails_when_paragraph_missing_marker_and_not_on_allow_list(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            md_path = root / "BUYER.md"
            page_path = root / "page.tsx"

            bad_md = VALID_MD + "\nThis sentence is brand-new prose that nobody approved and lacks the marker.\n"
            _write(md_path, bad_md)
            _write(page_path, VALID_PAGE_TSX)
            slugs = ["financial-services", "healthcare"]
            verticals_ts = _make_verticals_ts(root, slugs)
            briefs_dir = _make_briefs_dir(root, slugs)

            result = _run_script(md_path, page_path, verticals_ts, briefs_dir)

            self.assertNotEqual(result.returncode, 0)
            self.assertIn("paragraph missing q35 marker and not on allow-list", result.stderr)
