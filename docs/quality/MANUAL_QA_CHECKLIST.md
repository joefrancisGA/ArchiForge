> **Scope:** Manual QA and pilot operators validating scenarios that automated tests cannot cover; it is not a substitute for unit, integration, or Playwright coverage.

# ArchLucid Comprehensive Manual QA Checklist

This checklist focuses **exclusively** on scenarios that are either impossible, extremely brittle, or computationally infeasible to automate. These tests rely on human judgment, subjective evaluation, empathy, and complex out-of-band interactions. Everything else (API contracts, state mutations, RBAC enforcement, standard UI flows) should be handled by automated tests (e.g., unit, integration, Playwright).

**Agent output quality (structural / semantic scores, release bar):** A **key** part of manual QA for AI-backed runs is understanding what those metrics mean, why numeric floors should not be pushed toward 1.0 without calibration, and what you can do in prompts and briefs to keep scores legitimately high. See **§8.4** (and canonical technical detail in [`docs/library/AGENT_OUTPUT_EVALUATION.md`](../library/AGENT_OUTPUT_EVALUATION.md)).

Each test includes a justification for why human intervention is strictly necessary.

---

## 1. User Experience & Cognitive Load (The "Architect" Persona)

### 1.1. "Run Rationale" Comprehension
- **Test:** Execute an architecture run that generates complex findings (e.g., multiple accepted/rejected findings, custom notes, and a generated Golden Manifest). Read the resulting `RunRationale` summary.
- **Justification:** Automation can verify that the rationale is generated and contains the expected strings. However, **only a human** can determine if the generated summary is comprehensible, logically formatted, and intuitively helpful to an architect. If the summary is technically correct but confusing to read, the feature fails its business purpose.

### 1.2. Graph Snapshot Visualization and Orientation
- **Test:** Open an architecture run with a highly complex `GraphSnapshot` containing dozens of interconnected nodes. Attempt to find the "root cause" node of a specific security finding visually.
- **Justification:** Automated visual tests only check pixel changes. A human must verify if the visual layout algorithm (e.g., directed graph rendering) results in an incomprehensible "spaghetti" mess, or if the visual hierarchy naturally guides the eye to the most critical architectural components.

### 1.3. Finding Resolution Context
- **Test:** Review a finding that requires a decision (Accept/Reject). Read the `Rationale`, `Category`, and `RecommendedActions`. Make a decision based *only* on the information presented on the screen.
- **Justification:** Automation cannot judge if the provided context is actually sufficient to make a confident architectural governance decision. If the user has to constantly context-switch to external tools or codebases to understand the finding, the UX has failed.

---

## 2. Onboarding & The "Day One" Experience

### 2.1. Azure Marketplace & Procurement Flow (The "Trial Funnel")
- **Test:** Go through the Azure Marketplace SaaS offer purchase and trial signup flow end-to-end as a new user. Review the landing pages, the initial onboarding emails, and the "First 30 Minutes" Pilot experience.
- **Justification:** While the underlying webhooks and SCIM provisioning APIs are automated, the subjective experience of moving from the Azure Portal to the ArchLucid UI requires human empathy. A human needs to verify if the welcome emails feel spammy, if the instructions are clear, and if the psychological friction of the onboarding flow is acceptable.

### 2.2. SCIM Provisioning and First Login
- **Test:** Log in as a newly provisioned user via Entra ID (SCIM) who has no assigned projects yet. Evaluate the "empty state" of the dashboard.
- **Justification:** Automation verifies the user can log in. A human verifies if the empty state feels like a "dead end" or if there are clear, encouraging calls to action (e.g., "Create your first project" or "View the Operator Guide").

### 2.3. The "Day One SRE" Guide
- **Test:** Give a junior engineer the `day-one-sre.md` document and ask them to complete the basic operational tasks. Observe where they get stuck without offering help.
- **Justification:** Documentation validation requires a naive human perspective. Automation cannot tell you if a sentence in a runbook is ambiguously phrased or assumes domain knowledge the reader lacks.

---

## 3. Complex Integrations & Out-of-Band Workflows

### 3.1. Pull Request Decoration (Azure DevOps / GitHub)
- **Test:** Trigger the GitHub Action or AzDO pipeline that generates a manifest delta. Go to the PR in the respective platform and read the decorated comment left by ArchLucid.
- **Justification:** The pipeline script can be automated, but the subjective usefulness of the PR comment cannot. A human must decide: "Is this comment too noisy? Does it drown out human code review? Does the formatting look broken in the GitHub/AzDO UI?"

### 3.2. MS Teams / Slack Alert Fatigue
- **Test:** Trigger a burst of 20 architectural violations within a 5-minute window. Observe the resulting notifications in MS Teams.
- **Justification:** Automation verifies the webhooks fire 20 times. A human evaluates whether receiving 20 separate messages causes immediate "alert fatigue," or if the system appropriately rolls them up into a digest that is easy to read.

### 3.3. Operator Shell CLI Usability
- **Test:** Open the terminal and use the `Operator Shell` to execute a manual `RuleAudit` over a specific tenant, using only the CLI `--help` flags for guidance.
- **Justification:** CLI automation tests exit codes and stdout. A human evaluates if the CLI flags are intuitive, if the error messages for missing arguments are helpful, and if the output table is readable on a standard 80-column terminal.

---

## 4. Pathological Data & Edge-Case UI States

### 4.1. The "Wall of Text" Rule Description
- **Test:** Create an architecture run or `RuleSet` with an intentionally massive, unformatted description, a title with 500+ characters, and 50+ recommended actions. View this in the ArchLucid UI across different window sizes.
- **Justification:** Automation doesn't know if a layout "looks ugly" or "feels claustrophobic." A human must determine if pathological text wrapping, bizarre scrollbars, or text truncations render the screen practically unusable.

### 4.2. Deeply Nested JSON Manifests
- **Test:** View a `GoldenManifest` in the UI where the JSON payload is 15+ levels deep.
- **Justification:** While the JSON parser handles it fine, a human must determine if the UI's tree-viewer component becomes un-navigable or if the indentation pushes the text entirely off the right side of the screen.

### 4.3. The 10,000 Node Architecture
- **Test:** Load a project with an absurdly large architecture graph. Attempt to scroll and zoom.
- **Justification:** Playwright can assert the canvas renders, but a human must evaluate the subjective "feel" of the framerate, input lag, and browser memory pressure. Does the app feel sluggish and broken?

---

## 5. Graceful Degradation & Network Failures

### 5.1. The "Subway Tunnel" Test (Intermittent Connectivity)
- **Test:** Rapidly click between "Accept Finding" and "Reject Finding" on an architecture run while throttling the network to "Slow 3G" or toggling "Offline" in browser dev tools.
- **Justification:** Human exploratory testing uncovers missing loading spinners, jarring layout shifts, confusing state rollbacks, and frustrating UX glitches that rigid scripted tests always miss.

### 5.2. In-Flight Mutation Failures
- **Test:** Attempt to accept a finding, and simulate an HTTP 500 error (via proxy like Charles/Fiddler or dev tools). Try to navigate away, use the browser's back button, or close the modal.
- **Justification:** How does the UI recover? Does it silently fail, leaving the user thinking they accepted the risk? A human must verify that the failure is communicated clearly and safely.

---

## 6. Accessibility & Inclusive Design (Beyond Automated Scans)

### 6.1. Screen Reader Context (NVDA / VoiceOver)
- **Test:** Disconnect your mouse and turn off your monitor. Navigate the `ArchitectureRunDetail` findings, drill down into `DecisionTraceEntries`, and resolve findings using *only* the keyboard and a screen reader.
- **Justification:** Automated tools (like Axe) verify if ARIA attributes are technically present. They cannot tell you if the tab order is logically confusing, or if the screen reader's read-out actually makes sense to a visually impaired user (e.g., knowing *which* specific finding you are currently accepting).

### 6.2. Keyboard Traps & Focus Management
- **Test:** Open a complex modal (e.g., "Finding Rationale Details"). Press `Tab` repeatedly. Then press `Escape`.
- **Justification:** Ensure focus remains trapped inside the modal while it's open, and that dismissing it returns focus to the logical previous element. Automation often misses the "natural flow" of focus management.

### 6.3. High-Contrast & Color Blindness Simulation
- **Test:** Turn on the OS-level High Contrast mode, or use a color blindness simulator (e.g., Deuteranopia). Look at the Severity indicators (High, Medium, Low).
- **Justification:** If Severity is indicated *only* by red/yellow/green colors, automation won't care, but a color-blind user won't be able to distinguish them. A human verifies if there are accompanying icons or text labels.

---

## 7. Error Messaging & Troubleshooting

### 7.1. Intentional Misconfiguration (Bad Tokens)
- **Test:** Follow the in-app UI guide to set up Azure DevOps or GitHub integration, acting as a user who has never done it before. Intentionally use an expired or invalid token.
- **Justification:** Automation validates the 401 response. A human validates if the setup instructions are clear, and if the error messages for invalid tokens actually guide the user to a solution (e.g., "Your token has expired, please generate a new one here: [link]") rather than a generic "401 Unauthorized."

### 7.2. "Dead End" Analysis
- **Test:** Try to view a run detail for a `RunId` that has been deleted, or that the current tenant does not have RBAC access to.
- **Justification:** Does the UI crash with a white screen? Does it show a generic error? A human needs to ensure the user is gently guided back to safety (e.g., "This run could not be found. Return to the dashboard.").

---

## 8. Explainability & AI Trust

### 8.1. Explainability Trace Completeness "Gut Check"
- **Test:** Review a finding that has a `CompletenessRatio` of around 50-60%. Look at what data is missing (e.g., missing GraphNodeIds, missing Alternative Paths).
- **Justification:** The `ExplainabilityTraceCompletenessAnalyzer` calculates a mathematical score. A human needs to do a subjective "gut check" to see if a 60% score accurately reflects the perceived usefulness of the trace. If a trace scores highly but still feels useless to an auditor, the scoring algorithm's weighting needs subjective tuning.

### 8.2. LLM Hallucination / Tone Check (If Applicable)
- **Test:** If AI generates the finding rationale or summaries, read 10-15 generated outputs.
- **Justification:** Automation can check for restricted keywords, but only a human can tell if the AI is being overly pedantic, condescending, or hallucinates architectural components that don't exist in the manifest.

### 8.3. Real-LLM / agent output quality (manual “Q7” gate)
- **Test:** On **staging** (or another non-simulator host configured for **real** Azure OpenAI — see `docs/library/FIRST_REAL_VALUE.md` and your deployment secrets), run **at least one** full authority run through **create → execute → commit** with a **realistic** architecture brief (not the bare minimum one-liner). Then:
  - Skim every agent-backed finding: are claims **plausible** given the manifest and context, or obvious nonsense?
  - Open **agent execution trace** (prompts/response) for at least one agent (e.g. Topology) and confirm the model is **answering the actual request**, not a template mismatch.
  - If your environment exposes it (UI, diagnostics, or Grafana): note **structural completeness** and **semantic**-style signals so you are not flying blind compared to the **simulator** on the same shape of request.
  - Optionally: run a **second** run with **simulator** mode on the **same** or equivalent request to **feel** the difference in usefulness (simulator = deterministic for CI; real LLM = what pilots experience).
- **Record (for you):** date, **environment URL**, model/deployment id if known, and one line: **“acceptable for pilot” / “not yet”** and why. This is your **manual** answer to the independent assessment **Q7** (agent eval / “last green” narrative) when you do **not** yet rely on a **published** 30-day rollup from `agent-eval-datasets-nightly` (`.github/workflows/agent-eval-datasets-nightly.yml`) or other automation. **Structured template:** [`REAL_LLM_RUN_EVIDENCE_TEMPLATE.md`](REAL_LLM_RUN_EVIDENCE_TEMPLATE.md).
- **Release check-in stub:** [`REAL_MODE_EVIDENCE_RELEASE_CHECKIN.md`](REAL_MODE_EVIDENCE_RELEASE_CHECKIN.md) (steps + pointers — optional committed summary path is owner-defined).
- **Justification:** Scheduled jobs can assert JSON shape and thresholds; they **cannot** stand in for a human judgment that **this** pipeline output is **credible and safe to show a sponsor** on a real engagement. This step turns anxiety into a **finite, repeatable** session you control.

### 8.4. Agent output scores — layperson meaning, threshold discipline, and keeping scores high (key)

Use this subsection whenever you run **real** Azure OpenAI paths (§8.3) or interpret **`archlucid_agent_output_*`** telemetry. Product stance: a **conservative** release bar for buyers who design on Azure and for AI systems — **block** releases on insufficient **reference** evidence, not **warn-only**; see [`AGENT_OUTPUT_EVALUATION.md`](../library/AGENT_OUTPUT_EVALUATION.md) § **Quality gate** and **Release credibility posture**.

#### What the numbers mean (plain English)

| Metric | Range | Lay meaning |
|--------|-------|-------------|
| **Structural completeness** | 0–1 | “Did the model fill the **expected JSON fields**?” **1.0** ≈ all expected keys present and non-empty; **~0.55** ≈ about half; **~0.35** ≈ mostly empty or broken shape. |
| **Semantic score** | 0–1 | “Is the **content** substantive, not hollow?” Built from **claims** (fraction with real **evidence** refs or evidence text) and **findings** (each needs severity, description **>** 10 chars, recommendation **>** 5 chars). Formula: **Claims × 0.4 + Findings × 0.6** (see [`AGENT_OUTPUT_EVALUATION.md`](../library/AGENT_OUTPUT_EVALUATION.md)). |

**Quality gate outcomes** (same config family as `ArchLucid:AgentOutput:QualityGate`): **accepted** = above warn floors; **warned** = soft fail band; **rejected** = catastrophic band. Shipped **defaults** in code use **warn below ~0.55** and **reject below ~0.35** on structural and semantic — those are **starting calibration points**, not the final “Azure architect credibility” bar if you tighten for release.

#### Why you should **not** only “raise everything toward 1.0”

- **LLMs are variable.** Even good models occasionally emit a claim without a citation or a slightly short finding. Floors in the **0.90+** range on **real** briefs often create **noise** (blocked releases for **non-regressions**) rather than signal.
- **The checks are minimal by design.** “Description > 10 characters” is a **floor**, not proof of depth. A high numeric **target** does not, by itself, mean prose is **good** — only that it cleared a cheap detector.
- **Simulator vs real differ.** Simulator runs may score **very high** deterministically; **real AOAI** is the distribution that matters for pilots. Calibrate thresholds on **reference deployment + realistic corpus**, not on wishful 1.0.
- **Credibility is process + published bar**, not a magic number. Buyers care that you **name a reference model**, **run the gate**, and **do not ship when it fails** — not that your internal reject line is 0.92.

#### Practical starting calibration (manual QA + release planning)

After the **reference Azure OpenAI deployment** is fixed and you have **~10–20** real-mode runs on **realistic** briefs:

- Consider **release-blocking** floors around **0.70 / 0.70** (structural / semantic) as a **first** tight bar — meaningfully **above** the shipped **reject** default (**0.35**), which is really a **catastrophic** detector.
- **Tighten** toward **0.80+** only when **measured** pilot runs routinely sit there (prompt and corpus improvements landed).

Record in your pilot notes: **date**, **deployment id**, **brief id**, and whether scores **passed your chosen bar**.

#### What **you** can do to help scores stay **legitimately** high (not gaming the metric)

- **Prompts:** Require **every claim** to cite evidence; require **every finding** to include a **concrete** recommendation. That directly feeds **semantic** scoring.
- **Architecture briefs:** Well-structured briefs (named components, constraints, technologies) yield **better** structure and citations than one-line asks.
- **Eval corpus:** Keep synthetic / pilot-safe briefs **realistic and well-formed** (see assessment prompt on eval corpus). Garbage-in produces garbage-out and unstable scores.
- **Model & temperature:** Prefer **lower temperature** on reference / release paths for **repeatable** completeness; align **golden cohort real-LLM** with the same deployment you advertise.
- **Future refinement:** **Per-`agent_type` floors** (Topology vs Compliance, etc.) allow a **true** bar per agent without forcing one average to satisfy every shape.

#### Manual QA checklist actions (tie to §8.3)

On each **real-LLM** validation session (§8.3), verify **at least**:

- [ ] You can **explain** structural and semantic scores to a **non-engineer** using the table above.
- [ ] You are **not** treating “warn” as “ship anyway” if your **release policy** says **block** — open a defect or defer the release.
- [ ] You logged **one** concrete improvement when scores dipped (prompt gap, thin brief, wrong deployment, or agent-specific issue).

---

## 9. Runbook & Incident Response Validation

### 9.1. The "3 AM On-Call" Test
- **Test:** Open the `MIGRATION_ROLLBACK.md` or `ALERT_DELIVERY_FAILURES.md` runbook. Read through it rapidly as if you have been woken up at 3 AM by PagerDuty.
- **Justification:** A runbook must be incredibly concise, copy-paste friendly, and free of unnecessary prose. A human must evaluate if the runbook induces anxiety or if it clearly and calmly leads the operator to mitigation. Automation cannot test human panic.