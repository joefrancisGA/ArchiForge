#!/usr/bin/env python3
"""
Merge-blocking guard: buyer-facing first-30-minutes path stays in sync.

Two invariants enforced when either buyer surface is touched:

1. The vertical-picker labels rendered by `archlucid-ui/src/app/(marketing)/get-started/get-started-verticals.ts`
   must match the on-disk slug set under `templates/briefs/` exactly (Owner Q2,
   Resolved 2026-04-23 sixth pass — see `docs/PENDING_QUESTIONS.md`).

2. Any prose paragraph in the buyer-facing files
   (`docs/BUYER_FIRST_30_MINUTES.md` and `archlucid-ui/src/app/(marketing)/get-started/page.tsx`)
   that lacks the q35 placeholder marker `<<placeholder copy — replace before external use>>`
   must appear in the small allow-list this script ships. The allow-list
   exists so the consultative scaffolding sentences (intros, audience banner,
   no-install footer) can ship as canonical copy without an owner-approval marker.

Both invariants are pure-Python so this script runs in any CI environment that
already has Python 3.12 (no Node / tsx required).

Self-test: see `scripts/ci/tests/test_assert_buyer_first_30_minutes_in_sync.py`.
The script supports `--md-path` and `--page-path` overrides so the unit test
can point it at fixture files.
"""

from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]
DEFAULT_MD_PATH = REPO_ROOT / "docs" / "BUYER_FIRST_30_MINUTES.md"
DEFAULT_PAGE_PATH = REPO_ROOT / "archlucid-ui" / "src" / "app" / "(marketing)" / "get-started" / "page.tsx"
DEFAULT_VERTICALS_TS = REPO_ROOT / "archlucid-ui" / "src" / "app" / "(marketing)" / "get-started" / "get-started-verticals.ts"
DEFAULT_BRIEFS_DIR = REPO_ROOT / "templates" / "briefs"

PLACEHOLDER_MARKER = "<<placeholder copy — replace before external use>>"

# Allow-list of consultative scaffolding paragraphs that may appear without the
# placeholder marker. Compared against a normalized form (markdown chrome
# stripped, whitespace collapsed). Keep this list small — every entry is
# canonical copy that the assistant deliberately chose to ship without an
# owner-approval marker.
ALLOWED_PARAGRAPHS_NORMALIZED: frozenset[str] = frozenset(
    {
        # Buyer doc — body paragraphs (after markdown stripping)
        "ArchLucid is a SaaS product. You will not install anything to evaluate it.",
        "You found ArchLucid on GitHub. The repository is open so engineers can read the source, the architecture decisions, and the security posture before talking to us. Evaluating the product itself happens on the hosted SaaS at archlucid.com — there is no Docker, SQL, .NET, Node, Terraform, or CLI on the buyer path.",
        "For the same five steps with screenshots and links, open archlucid.com/get-started.",
        "Five steps. Roughly thirty minutes end-to-end on a normal connection.",
        # Buyer doc — steps 1–5 (numbered list); link targets normalized (archlucid.com, templates/briefs/, docs/CORE_PILOT.md)
        "Sign in. Open archlucid.com and sign in with your work identity (Microsoft Entra ID or a Google Workspace account). The sign-in flow uses your existing identity provider — there is no separate account to create and no credit card is required to start. You will land on a clean workspace ready for your first architecture run.",
        "Pick a vertical. A short picker asks which industry profile to start from. The defaults match the briefs in templates/briefs/ — financial-services, healthcare, public-sector, public-sector-us, retail, saas. Choose the closest match; you can change it later. The vertical sets default compliance rules, terminology, and analysis priorities so the first run produces findings relevant to your domain. You are not locked in — the vertical can be changed at any time, and you can run against multiple verticals from the same workspace.",
        "Run a sample. ArchLucid pre-populates a sample architecture request shaped for the vertical you picked, then runs the analysis pipeline. No upload required for the first run. Within a few seconds the pipeline runs topology, cost, and compliance analysis against the sample request and produces a committed manifest with structured findings and downloadable artifacts. You do not need to prepare any inputs or upload any files for this first pass — the goal is to see the shape of the output before investing your own data.",
        "Read your first finding. Open the committed run and read the first typed finding — what was flagged, why it was flagged, what evidence backs it. This is the smallest unit of value the product produces. Each finding carries a category (topology, cost, compliance, or quality), a severity level, a plain-language explanation of why it matters, and the evidence the analysis used to reach the conclusion. This is how ArchLucid communicates reviewable, defensible architecture observations — structured enough to act on, transparent enough to challenge.",
        "Decide what to do next. Either invite a colleague and run a second sample, or hand off to a guided pilot. If you want a second opinion, invite a colleague to sign in and run the same sample or a different vertical — no configuration is needed, and they will see results in their own workspace within minutes. If you are ready to move beyond the sample, the guided pilot path in docs/CORE_PILOT.md walks through creating a request with your own inputs, committing a manifest, and reviewing the artifacts that a real pilot would produce.",
        "Nothing on this page asks you to install Docker, SQL Server, .NET, Node, Terraform, or a CLI. If a document tells you to install one of those, you are reading contributor material — engineering docs that live under docs/engineering/ for ArchLucid contributors only.",
        # Marketing page — body paragraphs (after JSX stripping)
        "ArchLucid is a SaaS product. Nothing on this page asks you to install Docker, SQL Server, .NET, Node, Terraform, or a CLI.",
        "Five steps. Roughly thirty minutes end-to-end on a normal connection.",
        "Defaults mirror the existing briefs in templates/briefs/.",
        "For the operator path after the sample run, see pricing or talk to your account team via the Request a quote button on the pricing page.",
        "For the sponsor-facing narrative, see the executive sponsor brief in the public repository at docs/EXECUTIVE_SPONSOR_BRIEF.md.",
        # Marketing page — single-word JSX text nodes for the picker (heading + tiny labels)
        "Pick a vertical to start",
        "Where to go next",
        "pricing",
        "Your first 30 minutes with ArchLucid",
        # Buyer doc — "Where to go next" link bullets (consultative scaffolding)
        "Screenshots and the same five steps with the live UI: archlucid.com/get-started.",
        "Operator path (after the sample run, when you are ready for a real pilot): docs/CORE_PILOT.md.",
        "What the product is and is not, in plain language: docs/EXECUTIVE_SPONSOR_BRIEF.md.",
        "Pricing: archlucid.com/pricing.",
    }
)


def _normalize(sentence: str) -> str:
    return re.sub(r"\s+", " ", sentence).strip()


def _strip_markdown_chrome(text: str) -> str:
    """Strip code blocks, headings, list markers, blockquotes, link syntax, emphasis. Keep visible prose."""

    text = re.sub(r"```.*?```", "", text, flags=re.DOTALL)

    text = re.sub(r"!\[[^\]]*\]\([^)]*\)", "", text)
    text = re.sub(r"\[([^\]]+)\]\([^)]*\)", lambda m: re.sub(r"`([^`]*)`", r"\1", m.group(1)), text)

    text = re.sub(r"`([^`]*)`", r"\1", text)

    text = re.sub(r"\*\*([^*]+)\*\*", r"\1", text)
    text = re.sub(r"(?<!\*)\*([^*\n]+)\*(?!\*)", r"\1", text)

    out_lines: list[str] = []

    for raw in text.splitlines():
        line = raw.rstrip()
        stripped = line.lstrip()
        is_list_item = False

        if stripped.startswith("#"):
            continue
        if stripped.startswith(">"):
            continue
        if stripped.startswith(("- ", "* ", "+ ")):
            stripped = stripped[2:]
            is_list_item = True
        if re.match(r"^\d+\.\s", stripped):
            stripped = re.sub(r"^\d+\.\s+", "", stripped)
            is_list_item = True

        if is_list_item:
            out_lines.append("")
            out_lines.append(stripped)
            out_lines.append("")
            continue

        out_lines.append(stripped)

    return "\n".join(out_lines)


def _extract_paragraphs_from_tsx(tsx: str) -> list[str]:
    """Extract paragraph-equivalent prose units from the marketing page.

    Two sources contribute paragraphs:
      1. JSX text nodes between `>` and `<` (the inline scaffolding sentences).
      2. The `body:` string literals on each step record (template literal or "...").
         Template-literal `${PLACEHOLDER_MARKER}` substitutions are expanded so
         the script sees the same prose that ships at runtime.
    """

    out: list[str] = []

    for raw in re.findall(r">([^<>{}\n][^<>{}]*)<", tsx):
        candidate = raw.strip()

        if candidate:
            out.append(candidate)

    for match in re.finditer(r"body:\s*`([^`]+)`", tsx):
        body = match.group(1).replace("${PLACEHOLDER_MARKER}", PLACEHOLDER_MARKER)
        out.append(body)
    for match in re.finditer(r'body:\s*"((?:[^"\\]|\\.)*)"', tsx):
        out.append(match.group(1))

    return out


def _check_prose(source_label: str, paragraphs: list[str]) -> list[str]:
    failures: list[str] = []

    for paragraph in paragraphs:
        normalized = _normalize(paragraph)

        if len(normalized) < 20:
            continue
        if PLACEHOLDER_MARKER in normalized:
            continue
        if normalized in ALLOWED_PARAGRAPHS_NORMALIZED:
            continue

        failures.append(f"{source_label}: paragraph missing q35 marker and not on allow-list: {normalized!r}")

    return failures


def _read_verticals_from_ts(verticals_ts: Path) -> list[str]:
    text = verticals_ts.read_text(encoding="utf-8")
    match = re.search(r"BUYER_GET_STARTED_VERTICAL_SLUGS\s*=\s*\[(.*?)\]", text, re.DOTALL)

    if match is None:
        raise RuntimeError(f"could not find BUYER_GET_STARTED_VERTICAL_SLUGS array in {verticals_ts}")

    body = match.group(1)
    return [m.group(1) for m in re.finditer(r'"([^"]+)"', body)]


def _read_brief_slugs(briefs_dir: Path) -> list[str]:
    if not briefs_dir.is_dir():
        raise RuntimeError(f"templates/briefs directory missing: {briefs_dir}")

    return sorted(p.name for p in briefs_dir.iterdir() if p.is_dir())


def check_vertical_picker_in_sync(verticals_ts: Path, briefs_dir: Path) -> list[str]:
    rendered = sorted(_read_verticals_from_ts(verticals_ts))
    on_disk = _read_brief_slugs(briefs_dir)

    if rendered == on_disk:
        return []

    missing_in_picker = [s for s in on_disk if s not in rendered]
    extra_in_picker = [s for s in rendered if s not in on_disk]
    msgs: list[str] = []

    if missing_in_picker:
        msgs.append(f"vertical picker missing slugs present under templates/briefs/: {missing_in_picker}")
    if extra_in_picker:
        msgs.append(f"vertical picker contains slugs not present under templates/briefs/: {extra_in_picker}")

    return msgs


def check_md_prose(md_path: Path) -> list[str]:
    text = md_path.read_text(encoding="utf-8")
    stripped = _strip_markdown_chrome(text)
    paragraphs = [p for p in re.split(r"\n\s*\n", stripped) if p.strip()]
    return _check_prose(str(md_path.relative_to(REPO_ROOT)) if md_path.is_absolute() and REPO_ROOT in md_path.parents else str(md_path), paragraphs)


def check_page_prose(page_path: Path) -> list[str]:
    text = page_path.read_text(encoding="utf-8")
    chunks = _extract_paragraphs_from_tsx(text)
    label = str(page_path.relative_to(REPO_ROOT)) if page_path.is_absolute() and REPO_ROOT in page_path.parents else str(page_path)
    return _check_prose(label, chunks)


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--md-path", type=Path, default=DEFAULT_MD_PATH)
    parser.add_argument("--page-path", type=Path, default=DEFAULT_PAGE_PATH)
    parser.add_argument("--verticals-ts", type=Path, default=DEFAULT_VERTICALS_TS)
    parser.add_argument("--briefs-dir", type=Path, default=DEFAULT_BRIEFS_DIR)
    args = parser.parse_args(argv)

    failures: list[str] = []

    if not args.md_path.is_file():
        failures.append(f"missing buyer doc: {args.md_path}")
    if not args.page_path.is_file():
        failures.append(f"missing marketing page: {args.page_path}")
    if not args.verticals_ts.is_file():
        failures.append(f"missing verticals source-of-truth: {args.verticals_ts}")

    if failures:
        for msg in failures:
            print(f"assert_buyer_first_30_minutes_in_sync: {msg}", file=sys.stderr)
        return 1

    failures.extend(check_vertical_picker_in_sync(args.verticals_ts, args.briefs_dir))
    failures.extend(check_md_prose(args.md_path))
    failures.extend(check_page_prose(args.page_path))

    if not failures:
        return 0

    for msg in failures:
        print(f"assert_buyer_first_30_minutes_in_sync: {msg}", file=sys.stderr)

    return 1


if __name__ == "__main__":
    raise SystemExit(main())
