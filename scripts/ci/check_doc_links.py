#!/usr/bin/env python3
"""
Validate relative markdown links in docs/, archlucid-ui/docs/, and root README.md.
External https?://, mailto:, tel:, and fragment-only (#anchor) targets are skipped.
Exit 1 if any target file is missing.
"""

from __future__ import annotations

import re
import sys
from pathlib import Path

LINK_RE = re.compile(r"(?<!\!)\[[^\]]*\]\(([^)]+)\)")


def repo_root() -> Path:
    return Path(__file__).resolve().parents[2]


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

    if t.startswith("vscode:") or t.startswith("javascript:"):
        return True

    return False


def resolve_target(md_file: Path, target: str) -> Path | None:
    """Return filesystem path for link target, or None if not file-relative."""
    t = target.strip()
    pos = t.find("#")

    if pos >= 0:
        t = t[:pos].strip()

    if not t:
        return None

    if should_skip_target(t):
        return None

    base = md_file.parent
    resolved = (base / t).resolve()

    try:
        resolved.relative_to(repo_root())
    except ValueError:
        return None

    return resolved


def main() -> int:
    root = repo_root()
    scan_dirs = [
        root / "docs",
        root / "archlucid-ui" / "docs",
    ]
    files: list[Path] = [root / "README.md"]

    for d in scan_dirs:
        if d.is_dir():
            files.extend(sorted(d.rglob("*.md")))

    broken: list[str] = []

    for md in files:
        if not md.is_file():
            continue

        try:
            rel_md = md.relative_to(root).as_posix()
        except ValueError:
            continue

        if rel_md.startswith("docs/archive/"):
            continue

        text = md.read_text(encoding="utf-8", errors="replace")

        for m in LINK_RE.finditer(text):
            raw = m.group(1).strip().strip('"').strip("'")
            path_only = resolve_target(md, raw)

            if path_only is None:
                continue

            if path_only.is_file() or path_only.is_dir():
                continue

            if path_only.suffix.lower() != ".md":
                md_candidate = path_only.with_suffix(".md")

                if md_candidate.is_file():
                    continue

            idx = md.relative_to(root).as_posix()
            broken.append(f"{idx}:{raw} -> missing path {path_only.relative_to(root).as_posix()}")

    if broken:
        print("Broken relative markdown links:", file=sys.stderr)

        for line in broken[:200]:
            print(line, file=sys.stderr)

        if len(broken) > 200:
            print(f"... and {len(broken) - 200} more", file=sys.stderr)

        return 1

    print(f"check_doc_links: OK ({len(files)} markdown files scanned)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
