"""Compare PR B checklist items in ADR 0029 vs ``docs/architecture/PHASE_3_PR_B_TODO.md``.

**Default (pre–PR B merge): warn-only.** Always exits ``0`` so unrelated PRs are
not blocked. Prints ``WARNING`` when label text or order drifts between the two
sources; prints ``ERROR`` when one file lists checklist items the other omits
(a contradiction in the tracked set).

Set ``ARCHLUCID_PR_B_TRACKER_STRICT=1`` to fail (exit ``1``) on contradiction
only — intended for tightening CI after at least one PR B iteration has
exercised the workflow.

Drift (same item set, different ordering or minor whitespace) remains warning-only
even in strict mode.
"""

from __future__ import annotations

import os
import re
import sys
from collections import Counter
from pathlib import Path


ADR_REL = Path("docs/adr/0029-coordinator-strangler-acceleration-2026-05-15.md")
TRACKER_REL = Path("docs/architecture/PHASE_3_PR_B_TODO.md")
ADR_SECTION_HEADING = "#### PR B — audit-constant retirement checklist"
TRACKER_SECTION_HEADING = "## Checklist (mirror of ADR 0029 § Lifecycle § PR B)"
CHECKBOX_LINE = re.compile(r"^\s*-\s+\[[ xX]\]\s+(.*)$")


def repo_root_from_script() -> Path:
    """``scripts/ci/assert_pr_b_tracker_in_sync.py`` → repository root."""

    return Path(__file__).resolve().parents[2]


def _normalize_label(label: str) -> str:
    collapsed = " ".join(label.split())
    return collapsed.strip()


def extract_checklist_under_heading(text: str, heading: str) -> list[str]:
    """Return checkbox labels (in order) after ``heading`` until a non-checkbox break."""

    lines = text.splitlines()
    start = -1

    for i, line in enumerate(lines):
        if line.strip() == heading.strip():
            start = i + 1
            break

    if start < 0:
        print(f"WARNING: Heading not found: {heading!r}", file=sys.stderr)
        return []

    labels: list[str] = []
    for line in lines[start:]:
        stripped = line.rstrip()
        if not stripped.strip():
            if labels:
                break
            continue

        m = CHECKBOX_LINE.match(stripped)
        if m is None:
            if labels:
                break
            continue

        labels.append(m.group(1).strip())

    return labels


def compare_checklists(adr_labels: list[str], tracker_labels: list[str]) -> tuple[list[str], list[str], bool]:
    """Return (drift_messages, contradiction_messages, has_contradiction)."""

    drift: list[str] = []
    adr_norm = [_normalize_label(s) for s in adr_labels]
    tr_norm = [_normalize_label(s) for s in tracker_labels]
    ca: Counter[str] = Counter(adr_norm)
    ct: Counter[str] = Counter(tr_norm)
    contradictions: list[str] = []

    if ca != ct:
        only_adr = sorted((ca - ct).elements())
        only_tr = sorted((ct - ca).elements())
        if only_adr:
            contradictions.append(f"Items only in ADR 0029 (by multiset): {only_adr!r}")
        if only_tr:
            contradictions.append(f"Items only in PHASE_3_PR_B_TODO.md (by multiset): {only_tr!r}")
        return drift, contradictions, True

    if adr_norm == tr_norm:
        if adr_labels != tracker_labels:
            drift.append("Checklist labels match when normalized but raw Markdown differs (whitespace).")
        return drift, contradictions, False

    drift.append("Checklist item order differs between ADR 0029 and PHASE_3_PR_B_TODO.md (same items when normalized).")
    return drift, contradictions, False


def main() -> int:
    root = repo_root_from_script()
    adr_path = root / ADR_REL
    tracker_path = root / TRACKER_REL

    if not adr_path.is_file():
        print(f"ERROR: Missing {adr_path.relative_to(root)}", file=sys.stderr)
        return 0

    if not tracker_path.is_file():
        print(f"ERROR: Missing {tracker_path.relative_to(root)}", file=sys.stderr)
        return 0

    adr_text = adr_path.read_text(encoding="utf-8")
    tracker_text = tracker_path.read_text(encoding="utf-8")
    adr_labels = extract_checklist_under_heading(adr_text, ADR_SECTION_HEADING)
    tracker_labels = extract_checklist_under_heading(tracker_text, TRACKER_SECTION_HEADING)

    if not adr_labels:
        print("WARNING: No checklist items parsed under ADR 0029 PR B heading.", file=sys.stderr)
        return 0

    if not tracker_labels:
        print("WARNING: No checklist items parsed under PHASE_3_PR_B_TODO.md checklist heading.", file=sys.stderr)
        return 0

    drift, contradictions, has_contradiction = compare_checklists(adr_labels, tracker_labels)

    for msg in drift:
        print(f"WARNING: PR B tracker sync — {msg}", file=sys.stderr)

    for msg in contradictions:
        print(f"ERROR: PR B tracker sync — {msg}", file=sys.stderr)

    strict = os.environ.get("ARCHLUCID_PR_B_TRACKER_STRICT", "").strip().lower() in ("1", "true", "yes")

    if has_contradiction and strict:
        return 1

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
