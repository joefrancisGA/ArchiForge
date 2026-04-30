> **Scope:** Two-minute product demo video script and recording notes for the core pilot path — not a promise of marketing artifacts already produced.

# Demo video script (≈2 minutes)

**Audience:** prospects and executive sponsors who cannot self-host the API before a call. **Grounding:** [V1_SCOPE.md](../library/V1_SCOPE.md) Pilot layer only; no V1.1-only connectors.

---

## Storyboard (timing)

| Time | Scene | Operator UI route(s) | VO (voiceover, ~300 words total) | Visual |
|------|--------|----------------------|-----------------------------------|--------|
| 0:00–0:15 | Opening | Marketing or operator home | "Enterprise architecture review is still slow, inconsistent, and hard to prove. ArchLucid turns a structured request into governed, auditable outputs you can diff and replay." | Split: messy wiki slide vs clean manifest table (static slide ok). |
| 0:15–0:35 | Create run — wizard | `/runs/new` | "An operator starts from a guided flow: system name, constraints, and requirement lines that feed the ingestion pipeline—no mystery prompts." | Highlight wizard steps; paste short description. |
| 0:35–0:50 | Execute | Run detail → timeline / status | "Execution runs the multi-stage authority pipeline: ingestion, graph, findings, decisioning, artifacts—visible in the UI." | Show pipeline progressing (simulator or seeded env). |
| 0:50–1:15 | Findings + explainability | `/runs/{runId}/findings/{findingId}` or finding panel on run | "Findings aren't a chat paragraph. Each item carries structured traces you can inspect for what was checked and why." | Expand Explainability / trace fields. |
| 1:15–1:30 | Commit + manifest | Run detail → commit + Artifacts | "When ready, commit produces a golden manifest and downloadable artifacts—the reviewable package for your program." | Commit button → manifest summary + one artifact row. |
| 1:30–1:45 | Governance (optional if enabled) | Policy packs or governance queue | "Policy packs and pre-commit gates can block promotion when severities exceed thresholds—segregation-of-duties workflows sit on the Operate layer." | Brief policy or approval screen; skip if not configured. |
| 1:45–1:55 | Compare | `/compare` (two run ids) | "When designs iterate, compare two runs with structured deltas—not just a text diff." | Select two runs; show delta highlights. |
| 1:55–2:00 | Close | `/why-archlucid` if available | "Every recommendation traced. Every decision governed. Start a pilot on your terms." | Logo + CTA (contact / signup). |

Trim governance/compare if time budget is tight—core story is **wizard → execute → findings → commit**.

---

## Recording instructions

1. **Stack:** Prefer `scripts/demo-start.ps1` / compose **full-stack** with **Simulator** agents so the timeline stays deterministic; use **DevelopmentBypass** locally per [CORE_PILOT.md](../CORE_PILOT.md).
2. **Browser:** Chromium, 1440×900 or 1920×1080, **100%** zoom; hide bookmark bar; dark or light shell consistent throughout.
3. **Data:** Use the Contoso Retail demo tenant so run ids and manifests look realistic ([DEMO_QUICKSTART.md](./DEMO_QUICKSTART.md)).
4. **Audio:** Narrate at ~150 wpm; total VO above is ~260 words → ~1:45; pad with transitions or shorten scenes.
5. **Tools:** OBS Studio or similar; capture **browser** only unless you show CLI; no secrets on screen.

---

## Acceptance checklist

- Routes exist in **`archlucid-ui`** (App Router segments under `(operator)` / `(marketing)`).
- Claims match **Pilot** capabilities in **[V1_SCOPE.md](../library/V1_SCOPE.md)** §2.
- If a capability is gated (commercial tier / feature flag), voiceover states "when enabled for your tenant" rather than implying universal availability.

