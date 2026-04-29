#!/usr/bin/env python3
"""CI guard: spot-check that critical audit wiring strings remain in source.

Layer 1 planned durable-audit touch points are spread across Application, Api, and Persistence.Runtime.
This script is a cheap regression tripwire when a refactor drops an ``AuditEventTypes.*`` constant reference
by accident (it does not replace behavioral tests or the Application pairing test).
"""

from __future__ import annotations

import argparse
import sys
from pathlib import Path


FILES_AND_MARKERS: list[tuple[str, tuple[str, ...]]] = [
    (
        "ArchLucid.Application/Runs/Orchestration/ArchitectureRunCreateOrchestrator.cs",
        ("AuditEventTypes.RequestCreated", "AuditEventTypes.RequestLocked"),
    ),
    (
        "ArchLucid.Application/Runs/Orchestration/AuthorityDrivenArchitectureRunCommitOrchestrator.cs",
        ("AuditEventTypes.RequestReleased",),
    ),
    (
        "ArchLucid.Application/Runs/Orchestration/ArchitectureRunExecuteOrchestrator.cs",
        ("AuditEventTypes.Run.RetryRequested",),
    ),
    (
        "ArchLucid.Application/Governance/FindingReview/FindingReviewTrailAppendService.cs",
        (
            "AuditEventTypes.FindingReviewApproved",
            "AuditEventTypes.FindingReviewRejected",
            "AuditEventTypes.FindingReviewOverridden",
        ),
    ),
    (
        "ArchLucid.Api/Services/Admin/AdminDiagnosticsService.cs",
        ("AuditEventTypes.ManifestArchived",),
    ),
    (
        "ArchLucid.Persistence.Runtime/Orchestration/Pipeline/AuthorityPipelineStagesExecutor.cs",
        (
            "AuditEventTypes.FindingsSnapshotSealed",
            "AuditEventTypes.ArtifactSynthesisFailed",
            "AuditEventTypes.ArtifactSynthesisPartial",
        ),
    ),
]


def _repo_root() -> Path:
    return Path(__file__).resolve().parents[2]


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser(description="Layered audit wiring echo surface check.")
    parser.add_argument("--print-violations", action="store_true")
    parser.add_argument("--root", type=Path, default=None, help="Repo root override (defaults to ../../../ from this script).")

    args = parser.parse_args(argv)
    repo = args.root or _repo_root()

    missing: list[str] = []
    for rel, markers in FILES_AND_MARKERS:
        target = repo / rel
        if not target.is_file():
            missing.append(f"Missing file reference: '{rel}' (expected CI guard target to exist)")
            continue

        text = target.read_text(encoding="utf-8")
        for marker in markers:
            if marker not in text:
                missing.append(f"{rel}:{marker}")

    if args.print_violations:
        for m in sorted(missing):
            print(m)
        return 0

    if missing:
        print(
            "ERROR: audit wiring regression — expected marker snippets not found:\n" + "\n".join(sorted(missing)),
            file=sys.stderr,
        )
        return 1

    print("OK: layered audit wiring echo markers present.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
