> **Scope:** Single-session record for real Azure OpenAI (non-simulator) authority runs — supports manual QA §8.3, golden cohort posture, and pilot credibility; **not** a substitute for automated eval jobs.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md).

# Real-LLM run evidence — session template

**Audience:** Operators and release owners documenting **one** real-mode validation session.

## Session record (copy per run)

| Field | Value |
|-------|--------|
| **Date (UTC)** | |
| **Environment** | Staging / pilot stack — URL pattern only |
| **Agent mode** | Real / real-with-fallback (as configured) |
| **Model or deployment id** | If policy allows |
| **Brief / scenario id** | Internal id or short description (no customer PII) |
| **Run id** | Authority run GUID |
| **Outcome** | Commit succeeded / blocked / aborted — note |
| **Human verdict** | **acceptable for pilot** / **not yet** — 1–3 sentences |
| **Structural / semantic scores** | If surfaced (UI, diagnostics, export) |
| **Follow-ups** | Prompt, brief quality, agent-specific defects |

## Checklist (align with [MANUAL_QA_CHECKLIST.md](MANUAL_QA_CHECKLIST.md) §8.3)

- [ ] Skimmed agent-backed findings for plausible claims vs manifest.
- [ ] Opened at least one execution trace; model addressed the request shape.
- [ ] Compared to simulator on a similar brief if feasible (optional).

## Links

- Golden cohort gate: [GOLDEN_COHORT_REAL_LLM_GATE.md](../runbooks/GOLDEN_COHORT_REAL_LLM_GATE.md)
- Release check-in stub: [REAL_MODE_EVIDENCE_RELEASE_CHECKIN.md](REAL_MODE_EVIDENCE_RELEASE_CHECKIN.md)
- Pilot ROI / sponsor narrative: [PILOT_ROI_MODEL.md](../library/PILOT_ROI_MODEL.md)
