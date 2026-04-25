#!/usr/bin/env python3
"""
Brand-category seam guard: enforces that the V1 rebrand workstream
("AI Architecture Intelligence" -> "AI Architecture Review Board") flows
through `archlucid-ui/src/lib/brand-category.ts` rather than via hardcoded
legacy strings on every surface.

Owner Q6 / Q7 (Resolved 2026-04-23 sixth pass — see `docs/PENDING_QUESTIONS.md`)
scheduled the rebrand to V1. The workstream lives in
`docs/architecture/REBRAND_WORKSTREAM_2026_04_23.md` and this script ships in
**WARN mode** while PR-1 through PR-6 land — it prints offenders to stderr but
exits zero. The closing PR-7 flips it to **FAIL mode** by passing `--fail`
(and by adding `--fail` to the wiring in `.github/workflows/ci.yml`).

What it scans:
  - Every file under `archlucid-ui/src/app/`
  - `docs/EXECUTIVE_SPONSOR_BRIEF.md`
  - `docs/go-to-market/COMPETITIVE_LANDSCAPE.md`
  - `docs/trust-center.md`
  - `templates/briefs/**/brief.md`
  - `docs/library/PRODUCT_PACKAGING.md`

Allow-list (per-file):
  - The seam itself (`archlucid-ui/src/lib/brand-category.ts`) is always allowed.
  - Any other in-scope file may legitimately mention the legacy string IF it
    also imports / references `BRAND_CATEGORY_LEGACY` from the seam — that is
    the documented escape hatch for SEO redirect handlers and analytics tag
    mappers (the legacy string still has to appear *somewhere* so external
    inbound links and search-result snippets keep resolving for ~30 days
    post-flip).

Self-test: see `scripts/ci/tests/test_assert_brand_category_seam.py`. The
script accepts a `--repo-root` override so the unit test can point it at a
fixture tree without ever touching the real workspace.
"""

from __future__ import annotations

import argparse
import sys
from pathlib import Path
from typing import Iterable

LEGACY_STRING = "AI Architecture Intelligence"
SEAM_ESCAPE_HATCH = "BRAND_CATEGORY_LEGACY"
SEAM_RELATIVE_PATH = Path("archlucid-ui") / "src" / "lib" / "brand-category.ts"

REPO_ROOT_DEFAULT = Path(__file__).resolve().parents[2]


def _is_text_file(path: Path) -> bool:
    if not path.is_file(): return False

    suffix = path.suffix.lower()
    return suffix in {".ts", ".tsx", ".js", ".jsx", ".md", ".mdx", ".html", ".json"}


def _iter_app_files(app_root: Path) -> Iterable[Path]:
    if not app_root.is_dir(): return

    for path in sorted(app_root.rglob("*")):
        if _is_text_file(path): yield path


def _iter_brief_files(briefs_root: Path) -> Iterable[Path]:
    if not briefs_root.is_dir(): return

    for path in sorted(briefs_root.rglob("brief.md")):
        if path.is_file(): yield path


def collect_in_scope_files(repo_root: Path) -> list[Path]:
    """Build the canonical scan list for the rebrand workstream surfaces."""

    files: list[Path] = []

    files.extend(_iter_app_files(repo_root / "archlucid-ui" / "src" / "app"))

    fixed_doc_paths = [
        repo_root / "docs" / "EXECUTIVE_SPONSOR_BRIEF.md",
        repo_root / "docs" / "go-to-market" / "COMPETITIVE_LANDSCAPE.md",
        repo_root / "docs" / "trust-center.md",
        repo_root / "docs" / "library" / "PRODUCT_PACKAGING.md",
    ]

    for path in fixed_doc_paths:
        if path.is_file(): files.append(path)

    files.extend(_iter_brief_files(repo_root / "templates" / "briefs"))

    return files


def file_is_seam(path: Path, repo_root: Path) -> bool:
    try:
        rel = path.relative_to(repo_root)
    except ValueError: return False

    return Path(*rel.parts) == SEAM_RELATIVE_PATH


def file_is_offender(path: Path, repo_root: Path) -> bool:
    """A file is an offender if it mentions the legacy string AND is neither
    the seam nor a file that imports `BRAND_CATEGORY_LEGACY` (the escape hatch
    for SEO redirect handlers and analytics tag mappers)."""

    if file_is_seam(path, repo_root): return False

    text = path.read_text(encoding="utf-8", errors="ignore")

    if LEGACY_STRING not in text: return False
    if SEAM_ESCAPE_HATCH in text: return False

    return True


def find_offenders(repo_root: Path) -> list[Path]:
    return [p for p in collect_in_scope_files(repo_root) if file_is_offender(p, repo_root)]


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--repo-root",
        type=Path,
        default=REPO_ROOT_DEFAULT,
        help="Repo root to scan (defaults to two levels above this script). Tests override this.",
    )
    parser.add_argument(
        "--fail",
        action="store_true",
        help="Exit non-zero on any offender. Default is WARN mode (stderr message, exit 0). "
             "PR-7 of the rebrand workstream flips this to the default in CI.",
    )
    args = parser.parse_args(argv)

    seam_path = args.repo_root / SEAM_RELATIVE_PATH

    if not seam_path.is_file():
        print(
            f"assert_brand_category_seam: seam file missing at {seam_path} — "
            "PR-1 of the rebrand workstream must ship the seam first.",
            file=sys.stderr,
        )
        return 1

    offenders = find_offenders(args.repo_root)

    if not offenders:
        print(
            f"assert_brand_category_seam: OK — no hardcoded legacy '{LEGACY_STRING}' "
            "occurrences found in the scoped surfaces."
        )
        return 0

    mode_label = "FAIL" if args.fail else "WARN"

    print(
        f"assert_brand_category_seam: {mode_label} — {len(offenders)} file(s) still "
        f"hardcode the legacy phrase '{LEGACY_STRING}'. "
        f"Each follow-on PR in docs/architecture/REBRAND_WORKSTREAM_2026_04_23.md "
        f"replaces these with `BRAND_CATEGORY` imports from "
        f"archlucid-ui/src/lib/brand-category.ts:",
        file=sys.stderr,
    )

    for offender in offenders:
        try:
            rel = offender.relative_to(args.repo_root)
        except ValueError:
            rel = offender
        print(f"  - {rel.as_posix()}", file=sys.stderr)

    if args.fail: return 1

    print(
        "assert_brand_category_seam: returning 0 (WARN mode). "
        "Pass --fail (or wait for PR-7) to make this merge-blocking.",
        file=sys.stderr,
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
