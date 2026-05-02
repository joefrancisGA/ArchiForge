#!/usr/bin/env python3
"""Offline corpus checks: compare recorded findings against expected / unexpected probes.

Optional V1 quality slice: deterministic structural + semantic scores on committed
``agent-results/*.simulator.json`` files (parity with ``AgentOutputEvaluator`` /
``AgentOutputSemanticEvaluator`` / default ``AgentOutputQualityGate`` floors).

Default: informational only (exit 0). Use ``--enforce`` when you want recall /
unexpected probes to block; use ``--enforce-quality-gate`` when rejected gate
outcomes must fail the process (release automation).
"""

from __future__ import annotations

import argparse
import json
import sys
from datetime import UTC, datetime
from pathlib import Path
from typing import Any, Mapping, Sequence


SEVERITY_RANK: dict[str, int] = {
    "critical": 40,
    "high": 30,
    "medium": 20,
    "low": 10,
    "informational": 5,
    "info": 5,
}


def _repo_root() -> Path:
    return Path(__file__).resolve().parents[2]


def _load_json(path: Path) -> object:
    return json.loads(path.read_text(encoding="utf-8"))


def _norm_severity(raw: str | None) -> int:
    if raw is None:
        return 0

    return SEVERITY_RANK.get(str(raw).strip().lower(), 10)


def _combined_text(finding: Mapping[str, Any]) -> str:
    parts = [
        str(finding.get("category") or ""),
        str(finding.get("severity") or ""),
        str(finding.get("title") or ""),
        str(finding.get("detail") or ""),
    ]
    blob = " ".join(parts)
    return blob.strip().lower()


def _category_matches(blob_category: str | None, expected_category: str | None) -> bool:
    if expected_category is None:
        return True

    b = (blob_category or "").strip().lower()
    return b == str(expected_category).strip().lower()


def _meets_expected(actual: Sequence[Mapping[str, Any]], rule: Mapping[str, Any]) -> tuple[bool, str]:
    min_rank = _norm_severity(str(rule.get("minimumSeverity")))
    want_cat = str(rule.get("category") or "").strip() or None
    phrases = [str(p).strip().lower() for p in (rule.get("evidenceMustContain") or []) if str(p).strip()]

    if not phrases:
        return False, "expected rule missing evidenceMustContain"

    for fi in actual:
        cat_ok = _category_matches(str(fi.get("category") or ""), want_cat)

        if not cat_ok:
            continue

        if _norm_severity(str(fi.get("severity") or "")) < min_rank:
            continue

        text = _combined_text(fi)

        if all(p in text for p in phrases):
            return True, str(fi.get("findingId") or "(no id)")

    return False, ""


def _unexpected_triggered(actual: Sequence[Mapping[str, Any]], rule: Mapping[str, Any]) -> tuple[bool, str]:
    want_cat = str(rule.get("category") or "").strip() or None
    needles = [str(p).strip().lower() for p in (rule.get("ifContainsAny") or []) if str(p).strip()]

    if not needles:
        return False, ""

    for fi in actual:
        if want_cat is not None and not _category_matches(str(fi.get("category") or ""), want_cat):
            continue

        text = _combined_text(fi)

        if any(n in text for n in needles):
            return True, str(fi.get("findingId") or "(no id)")

    return False, ""


SHARED_AGENT_RESULT_KEYS: list[str] = [
    "resultId",
    "taskId",
    "runId",
    "agentType",
    "claims",
    "evidenceRefs",
    "confidence",
    "findings",
    "proposedChanges",
    "createdUtc",
]

# Defaults mirror ArchLucid.Core.Configuration.AgentOutputQualityGateOptions (shipped appsettings).
_DEFAULT_GATE: dict[str, Any] = {
    "enabled": True,
    "structural_warn_below": 0.3,
    "semantic_warn_below": 0.2,
    "structural_reject_below": 0.0,
    "semantic_reject_below": 0.0,
}


MIN_FINDING_DESCRIPTION_LEN = 10
MIN_FINDING_RECOMMENDATION_LEN = 5


def _evaluate_claims_block(root: dict[str, Any]) -> tuple[float, int]:
    claims_el = root.get("claims")
    if not isinstance(claims_el, list):
        return 0.0, 0

    total = 0
    with_evidence = 0

    for claim in claims_el:
        total += 1
        if not isinstance(claim, dict):
            continue

        refs = claim.get("evidenceRefs")
        has_refs = isinstance(refs, list) and len(refs) > 0
        ev = claim.get("evidence")
        has_ev = isinstance(ev, str) and len(ev) > 0

        if has_refs or has_ev:
            with_evidence += 1

    if total == 0:
        return 0.0, 0

    return (with_evidence / float(total), total - with_evidence)


def _evaluate_findings_block(root: dict[str, Any]) -> tuple[float, int]:
    findings_el = root.get("findings")
    if not isinstance(findings_el, list):
        return 0.0, 0

    total = 0
    complete = 0

    for finding in findings_el:
        total += 1
        if not isinstance(finding, dict):
            continue

        sev = finding.get("severity")
        has_sev = isinstance(sev, str) and len(sev) > 0

        desc = finding.get("description")
        has_desc = isinstance(desc, str) and len(desc) > MIN_FINDING_DESCRIPTION_LEN

        rec = finding.get("recommendation")
        has_rec = isinstance(rec, str) and len(rec) > MIN_FINDING_RECOMMENDATION_LEN

        if has_sev and has_desc and has_rec:
            complete += 1

    if total == 0:
        return 0.0, 0

    return (complete / float(total), total - complete)


def _compute_overall_semantic(claims_ratio: float, findings_ratio: float, root: dict[str, Any]) -> float:
    c = root.get("claims")
    f = root.get("findings")
    has_claims = isinstance(c, list) and len(c) > 0
    has_findings = isinstance(f, list) and len(f) > 0

    if not has_claims and not has_findings:
        return 0.0

    if has_claims and not has_findings:
        return claims_ratio

    if not has_claims and has_findings:
        return findings_ratio

    return claims_ratio * 0.4 + findings_ratio * 0.6


def _apply_quality_gate(structural: float, semantic: float) -> str:
    g = _DEFAULT_GATE
    if not g["enabled"]:
        return "accepted"

    if structural < g["structural_reject_below"] or semantic < g["semantic_reject_below"]:
        return "rejected"

    if structural < g["structural_warn_below"] or semantic < g["semantic_warn_below"]:
        return "warned"

    return "accepted"


def score_committed_agent_result_json(text: str) -> dict[str, Any]:
    """Score serialized AgentResult-shaped JSON (Web defaults). Returns parse_failure + ratios + gate."""

    try:
        doc = json.loads(text)
    except json.JSONDecodeError:
        return {
            "parse_failure": True,
            "structural_ratio": 0.0,
            "missing_keys": SHARED_AGENT_RESULT_KEYS[:],
            "claims_quality_ratio": 0.0,
            "findings_quality_ratio": 0.0,
            "overall_semantic": 0.0,
            "empty_claim_count": 0,
            "incomplete_finding_count": 0,
            "gate_outcome": _apply_quality_gate(0.0, 0.0),
        }

    if not isinstance(doc, dict):
        return {
            "parse_failure": True,
            "structural_ratio": 0.0,
            "missing_keys": SHARED_AGENT_RESULT_KEYS[:],
            "claims_quality_ratio": 0.0,
            "findings_quality_ratio": 0.0,
            "overall_semantic": 0.0,
            "empty_claim_count": 0,
            "incomplete_finding_count": 0,
            "gate_outcome": _apply_quality_gate(0.0, 0.0),
        }

    present = set(doc.keys())
    missing = [k for k in SHARED_AGENT_RESULT_KEYS if k not in present]
    hit = len(SHARED_AGENT_RESULT_KEYS) - len(missing)
    structural = hit / float(len(SHARED_AGENT_RESULT_KEYS))

    claims_ratio, empty_claims = _evaluate_claims_block(doc)
    findings_ratio, incomplete_findings = _evaluate_findings_block(doc)
    overall = _compute_overall_semantic(claims_ratio, findings_ratio, doc)
    gate = _apply_quality_gate(structural, overall)

    return {
        "parse_failure": False,
        "structural_ratio": structural,
        "missing_keys": missing,
        "claims_quality_ratio": claims_ratio,
        "findings_quality_ratio": findings_ratio,
        "overall_semantic": overall,
        "empty_claim_count": empty_claims,
        "incomplete_finding_count": incomplete_findings,
        "gate_outcome": gate,
    }


def evaluate_quality_evidence_block(corpus_root: Path, scenario_id: str, qe: Mapping[str, Any]) -> dict[str, Any]:
    mode_raw = str(qe.get("mode") or "").strip().lower()
    agent_type = str(qe.get("agentType") or "").strip() or "(unspecified)"

    if mode_raw == "real":
        return {
            "scenario_id": scenario_id,
            "mode": "real",
            "agent_type": agent_type,
            "skipped": True,
            "reason": (
                "Real-mode evidence is not committed. Run with Azure OpenAI using "
                "``GET /v1/architecture/run/{runId}/agent-evaluation`` after execute — "
                "see docs/library/AGENT_EVAL_CORPUS.md."
            ),
        }

    if mode_raw != "simulator":
        return {
            "scenario_id": scenario_id,
            "mode": mode_raw or "(missing)",
            "agent_type": agent_type,
            "error": "qualityEvidence.mode must be 'simulator' or 'real'.",
        }

    rel = qe.get("agentResultPath")
    if not isinstance(rel, str) or not rel.strip():
        return {
            "scenario_id": scenario_id,
            "mode": "simulator",
            "agent_type": agent_type,
            "error": "qualityEvidence.agentResultPath is required for simulator mode.",
        }

    path = (corpus_root / rel.strip()).resolve()
    if not path.is_file():
        return {
            "scenario_id": scenario_id,
            "mode": "simulator",
            "agent_type": agent_type,
            "error": f"Missing agent result file: {rel}",
        }

    scored = score_committed_agent_result_json(path.read_text(encoding="utf-8"))
    scored["scenario_id"] = scenario_id
    scored["mode"] = "simulator"
    scored["agent_type"] = agent_type
    scored["agent_result_path"] = rel.strip()
    return scored


def _quality_remediation(quality: Mapping[str, Any]) -> str:
    if quality.get("skipped"):
        return "N/A (skipped)."

    if quality.get("error"):
        return f"Fix qualityEvidence or restore file — {quality['error']}"

    if quality.get("parse_failure"):
        return (
            "Repair JSON to a single object matching Web-serialized AgentResult; "
            "see docs/library/AGENT_OUTPUT_EVALUATION.md and GoldenAgentResults fixtures."
        )

    gate = str(quality.get("gate_outcome") or "")
    if gate == "rejected":
        parts: list[str] = [
            "Gate rejected: raise structural/semantic scores above shipped reject floors "
            "(ArchLucid:AgentOutput:QualityGate) or fix empty claims / thin findings."
        ]
        missing = quality.get("missing_keys") or []
        if isinstance(missing, list) and missing:
            parts.append(f"Missing keys: {', '.join(str(x) for x in missing)}.")
        return " ".join(parts)

    if gate == "warned":
        return (
            "Gate warned: tighten claims evidence and finding description/recommendation depth; "
            "compare metrics with docs/library/AGENT_OUTPUT_EVALUATION.md."
        )

    return "None (gate accepted)."


def render_markdown_report(
    rows: Sequence[Mapping[str, Any]],
    corpus_root: Path,
    min_recall: float,
    worst_recall: float,
) -> str:
    now = datetime.now(UTC).strftime("%Y-%m-%dT%H:%M:%SZ")
    lines: list[str] = [
        "## Agent eval corpus — offline evidence slice",
        "",
        f"_Generated {now} (UTC). Corpus root: `{corpus_root.as_posix()}`._",
        "",
        "### Evidence paths",
        "",
        "| Path | Meaning |",
        "|------|---------|",
        "| **Simulator** | Committed `agent-results/*.simulator.json` — **no Azure OpenAI**; deterministic scoring only. |",
        "| **Real (AOAI)** | Not stored in-repo; capture via architecture run + `agent-evaluation` API when exercising a **named reference deployment** (see AGENT_EVAL_CORPUS.md). |",
        "",
        "### Release / quality policy",
        "",
        "PR CI uses **simulator** JSON only (meets “no AOAI required”). "
        "**Release candidates** must not rely on warn-only gates: align floors with "
        "`docs/library/AGENT_OUTPUT_EVALUATION.md` "
        "and block promotion when reference-path scores fall **below conservative** thresholds.",
        "",
        "### Findings-recall summary (recorded `*.findings.json`)",
        "",
        f"_Worst-case recall {worst_recall:.2f} vs informational floor {min_recall:.2f} (use `--enforce` to fail on recall / unexpected probes)._",
        "",
        "| Scenario | Recall | Unexpected hits |",
        "|----------|--------|-----------------|",
    ]

    for row in rows:
        uh = row.get("unexpectedHits") or []
        if not isinstance(uh, list):
            uh = []
        lines.append(
            f"| `{row.get('id')}` | {float(row.get('recall') or 0):.2f} | {len(uh)} |",
        )

    lines.extend(
        [
            "",
            "### Simulator AgentResult quality (structural + semantic + gate)",
            "",
            "_“Explanation / trace completeness” proxy: claims-with-evidence ratio and findings field completeness "
            "(same signals as `AgentOutputSemanticEvaluator`; full prompts/traces need real execution — "
            "AGENT_TRACE_FORENSICS)._",
            "",
            "| Scenario | Mode | Agent (brief) | Structural | Semantic | Parse fail | Gate | Claims OK | Findings OK | Remediation |",
            "|----------|------|---------------|------------|----------|------------|------|-----------|------------|-------------|",
        ],
    )

    for row in rows:
        q = row.get("quality")
        if not isinstance(q, dict):
            lines.append(
                f"| `{row.get('id')}` | — | — | — | — | — | — | — | — | "
                "_Quality evidence not configured (recall-only scenario)._ |",
            )
            continue

        if q.get("skipped"):
            lines.append(
                f"| `{row.get('id')}` | real | {q.get('agent_type')} | — | — | — | — | — | — | "
                f"_Manual AOAI path — {q.get('reason', '')}_ |",
            )
            continue

        if q.get("error"):
            lines.append(
                f"| `{row.get('id')}` | simulator | {q.get('agent_type')} | — | — | — | **error** | — | — | "
                f"{_quality_remediation(q)} |",
            )
            continue

        pf = "yes" if q.get("parse_failure") else "no"
        struct = float(q.get("structural_ratio") or 0.0)
        sem = float(q.get("overall_semantic") or 0.0)
        cq = float(q.get("claims_quality_ratio") or 0.0)
        fq = float(q.get("findings_quality_ratio") or 0.0)
        gate = str(q.get("gate_outcome") or "")

        lines.append(
            f"| `{row.get('id')}` | simulator | {q.get('agent_type')} | {struct:.2f} | {sem:.2f} | {pf} | "
            f"{gate} | {cq:.2f} | {fq:.2f} | {_quality_remediation(q)} |",
        )

    lines.append("")
    return "\n".join(lines)


def evaluate_scenario(scenario_path: Path, corpus_root: Path) -> dict[str, Any]:
    scen = _load_json(scenario_path)

    if not isinstance(scen, dict):
        raise ValueError(f"{scenario_path.name} must be an object")

    sid = str(scen.get("id") or scenario_path.stem)
    rec_rel = scen.get("recording")

    if not isinstance(rec_rel, str) or not rec_rel.strip():
        raise ValueError(f"{sid}: recording path required")

    rec_path = (corpus_root / rec_rel).resolve()

    if not rec_path.is_file():
        raise FileNotFoundError(str(rec_path))

    rec = _load_json(rec_path)

    if not isinstance(rec, dict):
        raise ValueError(f"{rec_path.name} must be object")

    raw_findings = rec.get("findings")

    if not isinstance(raw_findings, list):
        raise ValueError(f"{rec_path.name} must contain findings array")

    actual: list[Mapping[str, Any]] = [f for f in raw_findings if isinstance(f, dict)]

    expected_rules = scen.get("expectedFindings") if isinstance(scen.get("expectedFindings"), list) else []
    unexpected_rules = scen.get("unexpectedFindings") if isinstance(scen.get("unexpectedFindings"), list) else []

    hits = 0

    for rule in expected_rules:
        if not isinstance(rule, dict):
            continue

        ok, who = _meets_expected(actual, rule)

        if ok:
            hits += 1

    unexpected_hits: list[str] = []

    for rule in unexpected_rules:
        if not isinstance(rule, dict):
            continue

        bad, who = _unexpected_triggered(actual, rule)

        if bad:
            unexpected_hits.append(who)

    denom = len(expected_rules) if expected_rules else 1
    recall = hits / float(denom)

    row: dict[str, Any] = {
        "id": sid,
        "path": str(scenario_path.relative_to(corpus_root)),
        "expectedRules": len(expected_rules),
        "expectedHits": hits,
        "recall": recall,
        "unexpectedHits": unexpected_hits,
        "actualFindings": len(actual),
    }

    qe = scen.get("qualityEvidence")
    if isinstance(qe, dict):
        row["quality"] = evaluate_quality_evidence_block(corpus_root, sid, qe)
    else:
        row["quality"] = None

    return row


def main() -> int:
    parser = argparse.ArgumentParser(description="Evaluate tests/eval-corpus scenarios (offline recordings).")
    parser.add_argument(
        "--corpus",
        type=Path,
        default=_repo_root() / "tests" / "eval-corpus",
        help="Corpus root containing manifest.json",
    )
    parser.add_argument("--enforce", action="store_true", help="Exit non-zero when thresholds fail")
    parser.add_argument("--min-recall", type=float, default=0.6, help="Minimum recall for expected rules")
    parser.add_argument(
        "--markdown-report",
        type=Path,
        default=None,
        help="Write Markdown summary (simulator vs real paths, metrics, remediation).",
    )
    parser.add_argument(
        "--enforce-quality-gate",
        action="store_true",
        help="Exit non-zero when any simulator quality row gate_outcome is rejected.",
    )
    args = parser.parse_args()

    corpus_root: Path = args.corpus.resolve()

    manifest_path = corpus_root / "manifest.json"

    if not manifest_path.is_file():
        print(f"::error::Missing manifest {manifest_path}", file=sys.stderr)
        return 1

    manifest = _load_json(manifest_path)

    if not isinstance(manifest, dict):
        print("::error::manifest must be object", file=sys.stderr)
        return 1

    scen_list = manifest.get("scenarios")

    if not isinstance(scen_list, list) or not scen_list:
        print("::error::manifest.scenarios[] required", file=sys.stderr)
        return 1

    rows: list[dict[str, Any]] = []
    worst_recall = 1.0

    for rel in scen_list:
        if not isinstance(rel, str) or not rel.strip():
            continue

        scen_path = corpus_root / rel.strip()

        row = evaluate_scenario(scen_path, corpus_root)
        rows.append(row)
        worst_recall = min(worst_recall, float(row["recall"]))

    if not rows:
        print("::error::no scenarios evaluated", file=sys.stderr)
        return 1

    print("scenario\trecall\tunexpected")
    failed = False
    quality_failed = False

    for row in rows:
        print(
            f'{row["id"]}\t{row["recall"]:.2f}\t{len(row["unexpectedHits"])}',
        )

        if float(row["recall"]) + 1e-9 < float(args.min_recall):
            failed = True
            print(f"::warning::recall<{args.min_recall} for {row['id']} ({row['recall']:.2f})", file=sys.stderr)

        if row["unexpectedHits"]:
            failed = True
            print(f"::warning::unexpected triggers on {row['id']}: {row['unexpectedHits']}", file=sys.stderr)

        q = row.get("quality")
        if isinstance(q, dict):
            if q.get("error"):
                quality_failed = True
                print(f"::error::qualityEvidence error for {row['id']}: {q['error']}", file=sys.stderr)

            if args.enforce_quality_gate and q.get("gate_outcome") == "rejected":
                quality_failed = True
                print(
                    f"::error::quality gate rejected for {row['id']} (structural="
                    f"{q.get('structural_ratio')}, semantic={q.get('overall_semantic')})",
                    file=sys.stderr,
                )

    worst_line = f"(worst recall {worst_recall:.2f} vs min {float(args.min_recall):.2f})"
    print(worst_line)

    md = render_markdown_report(rows, corpus_root, float(args.min_recall), worst_recall)
    if args.markdown_report is not None:
        args.markdown_report.parent.mkdir(parents=True, exist_ok=True)
        args.markdown_report.write_text(md, encoding="utf-8")

    if args.enforce and failed:
        print("::error::corpus enforce failed", file=sys.stderr)
        return 1

    if quality_failed:
        return 1

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
