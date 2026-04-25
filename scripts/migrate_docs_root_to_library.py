#!/usr/bin/env python3
"""
One-time (2026-04-23) documentation layout migration:

- Move superseded quality / Cursor prompt packs under ``docs/archive/quality/``.
- Move most ``docs/*.md`` root files to ``docs/library/`` while keeping a small
  canonical root set (buyer spine + CI-pinned paths).
- Rewrite relative markdown links across **markdown only** so post-move paths
  resolve (uses post-move source + target directories to avoid ``library/library``).

Run from repo root::

    python scripts/migrate_docs_root_to_library.py --dry-run
    python scripts/migrate_docs_root_to_library.py
"""

from __future__ import annotations

import argparse
import os
import re
import shutil
from pathlib import Path

UTF8_BOM = "\ufeff"
LINK_RE = re.compile(r"(?<!\!)\[[^\]]*\]\(([^()]*(?:\([^()]*\))*[^()]*)\)")
ARCHIVE_BANNER = (
    "> Archived 2026-04-23 — superseded by [docs/START_HERE.md](../START_HERE.md) "
    "and the current assessment pair under ``docs/``. Kept for audit trail.\n\n"
)


def repo_root() -> Path:
    return Path(__file__).resolve().parents[1]


def strip_anchor(url: str) -> tuple[str, str]:
    pos = url.find("#")
    if pos < 0:
        return url, ""

    return url[:pos], url[pos:]


def should_skip_target(raw: str) -> bool:
    t = raw.strip()
    if not t or t.startswith("#"):
        return True
    if t.startswith("http://") or t.startswith("https://"):
        return True
    if t.startswith("mailto:") or t.startswith("tel:"):
        return True
    if "{" in t or "*" in t:
        return True
    return False


def resolve_target(repo: Path, source: Path, target: str) -> Path | None:
    base, _anchor = strip_anchor(target.strip())
    if should_skip_target(base):
        return None
    try:
        resolved = (source.parent / base).resolve()
    except OSError:
        return None
    try:
        resolved.relative_to(repo)
    except ValueError:
        return None
    return resolved


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args()
    root = repo_root()
    docs = root / "docs"
    library = docs / "library"
    archive_rel = Path("archive") / "quality" / "2026-04-23-doc-depth-reorg"
    archive_q = docs / archive_rel

    keep_root: set[str] = {
        "INSTALL_ORDER.md",
        "FIRST_30_MINUTES.md",
        "CORE_PILOT.md",
        "ARCHITECTURE_ON_ONE_PAGE.md",
        "PENDING_QUESTIONS.md",
        "CONCEPTS.md",
        "V1_REQUIREMENTS_TEST_TRACEABILITY.md",
        "CHANGELOG.md",
        "ARCHLUCID_RENAME_CHECKLIST.md",
        "AZURE_MARKETPLACE_SAAS_OFFER.md",
        "START_HERE.md",
        "EXECUTIVE_SPONSOR_BRIEF.md",
        "ARCHITECTURE_INDEX.md",
        "TROUBLESHOOTING.md",
        "COVERAGE_GAP_ANALYSIS.md",
        "COVERAGE_GAP_ANALYSIS_RECENT.md",
        "FIRST_5_DOCS.md",
        "FIRST_FIVE_DOCS.md",
        "FIRST_RUN_WIZARD.md",
        "FIRST_RUN_WALKTHROUGH.md",
        "QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_68_60.md",
        "CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_21_68_60.md",
    }

    root_md = sorted(p.name for p in docs.glob("*.md") if p.is_file())
    to_library = [n for n in root_md if n not in keep_root]

    quality_archive_names: list[str] = []
    for n in list(to_library):
        if n in {
            "QUALITY_ASSESSMENT_2026_04_21_INDEPENDENT_68_60.md",
            "CURSOR_PROMPTS_QUALITY_ASSESSMENT_2026_04_21_68_60.md",
        }:
            continue
        if n.startswith("QUALITY_ASSESSMENT") or n.startswith("CURSOR_PROMPTS"):
            quality_archive_names.append(n)
            to_library.remove(n)

    moved_library: set[str] = set(to_library)
    quality_set: set[str] = set(quality_archive_names)

    if args.dry_run:
        print(f"dry-run: archive quality/cursor count={len(quality_archive_names)}")
        print(f"dry-run: move to library count={len(to_library)}")
        print(f"dry-run: keep at docs root count={len(keep_root)}")
        return 0

    library.mkdir(parents=True, exist_ok=True)
    archive_q.mkdir(parents=True, exist_ok=True)

    def relocate_resolved(resolved: Path) -> Path:
        try:
            rel = resolved.relative_to(docs)
        except ValueError:
            return resolved
        if len(rel.parts) == 1:
            name = rel.parts[0]
            if name in moved_library:
                return library / name
            if name in quality_set:
                return archive_q / name
        return resolved

    def post_move_parent_dir(source: Path) -> Path:
        try:
            rel = source.relative_to(docs)
        except ValueError:
            return source.parent
        if len(rel.parts) == 1 and rel.parts[-1] in moved_library:
            return library
        return source.parent

    def rewrite_markdown_links(text: str, source: Path) -> str:
        def repl(m: re.Match[str]) -> str:
            url = m.group(1)
            base, anchor = strip_anchor(url.strip())
            if should_skip_target(base):
                return m.group(0)
            if not base.lower().endswith(".md"):
                return m.group(0)
            resolved = resolve_target(root, source, base)
            if resolved is None:
                return m.group(0)
            if not resolved.is_file():
                return m.group(0)
            t_prime = relocate_resolved(resolved)
            p_prime = post_move_parent_dir(source)
            new_base = os.path.relpath(str(t_prime), str(p_prime)).replace("\\", "/")
            old_norm = os.path.normpath(base).replace("\\", "/")
            if new_base == old_norm:
                return m.group(0)
            new_inner = new_base + anchor
            full = m.group(0)
            start1 = m.start(1) - m.start(0)
            end1 = m.end(1) - m.start(0)
            return full[:start1] + new_inner + full[end1:]

        return LINK_RE.sub(repl, text)

    def iter_markdown_files() -> list[Path]:
        paths: list[Path] = []
        for p in root.rglob("*.md"):
            if ".git" in p.parts or "node_modules" in p.parts:
                continue
            paths.append(p)
        return paths

    for path in iter_markdown_files():
        try:
            text = path.read_text(encoding="utf-8", errors="replace")
        except OSError:
            continue
        new_text = rewrite_markdown_links(text, path)
        if new_text != text:
            path.write_text(new_text, encoding="utf-8", newline="\n")

    for n in quality_archive_names:
        src = docs / n
        dst = archive_q / n
        body = src.read_text(encoding="utf-8", errors="replace")
        if body.startswith(UTF8_BOM):
            body = body[len(UTF8_BOM) :]
        dst.write_text(ARCHIVE_BANNER + body, encoding="utf-8", newline="\n")
        src.unlink()

    for n in to_library:
        src = docs / n
        dst = library / n
        shutil.move(str(src), str(dst))

    print(
        f"migrate_docs_root_to_library: archived {len(quality_archive_names)} quality files; "
        f"moved {len(to_library)} to docs/library/; link rewrite applied before moves."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
