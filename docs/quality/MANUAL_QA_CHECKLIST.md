> **Scope:** For QA leads and release owners who need human-only validation scenarios that automation cannot reliably cover—**not** a substitute for automated unit, integration, or E2E tests.

# ArchLucid Manual QA Checklist

This checklist focuses **exclusively** on scenarios that are either impossible, extremely brittle, or computationally infeasible to automate. These tests rely on human judgment, subjective evaluation, or complex out-of-band interactions. Everything else (API contracts, state mutations, RBAC enforcement, standard UI flows) should be handled by automated unit, integration, or Playwright tests.

## 1. Subjective Usability and "Run Rationale" Comprehension
- **Test:** Execute an architecture run that generates complex findings (e.g., multiple accepted/rejected findings, custom notes, and a generated Golden Manifest). Read the resulting `RunRationale` summary.
- **Justification:** Automation can verify that the rationale is generated and contains the expected strings. However, **only a human** can determine if the generated summary is actually comprehensible, logically formatted, and intuitively helpful to an architect reviewing the architecture. If the summary is technically correct but confusing to read, the feature fails its business purpose.

## 2. Azure Marketplace & Onboarding Handoff (The "Trial Funnel")
- **Test:** Go through the Azure Marketplace SaaS offer purchase and trial signup flow end-to-end as a new user. Review the landing pages, the initial onboarding emails, and the Day One Pilot experience.
- **Justification:** While the underlying webhooks and SCIM provisioning APIs are automated, the subjective experience of moving from the Azure Portal to the ArchLucid UI requires human empathy. A human needs to verify if the welcome emails feel spammy, if the instructions are clear, and if the psychological friction of the onboarding flow is acceptable.

## 3. Visual Layout with Pathological Data (Stress Testing the UI)
- **Test:** Create an architecture run or `RuleSet` with intentionally massive, unformatted descriptions, a title with 500+ characters, 50+ recommended actions, and deeply nested graph snapshots. View this in the ArchLucid UI across different window sizes.
- **Justification:** Automated visual regression tools detect changes from a baseline, but a human is needed to determine if pathological text wrapping, bizarre scrollbars, or text truncations render the screen practically unusable. Automation doesn't know if a layout "looks ugly" or "feels claustrophobic."

## 4. Exploratory "Abuse" & Edge-Case UI State Transitions
- **Test:** Rapidly click between "Accept Finding" and "Reject Finding" on an architecture run while throttling the network to "Slow 3G" in browser dev tools. Attempt to navigate away, use the browser's back button, or close the modal during in-flight mutations.
- **Justification:** UI automation follows predictable, happy-path scripts (even for negative tests). Human exploratory testing uncovers race conditions, missing loading spinners, jarring layout shifts, and frustrating UX glitches that rigid scripted tests always miss. 

## 5. Third-Party Integration Configuration UX (Azure DevOps / Teams)
- **Test:** Follow the in-app UI guide to set up Azure DevOps PR Decoration or MS Teams notifications from scratch, acting as a user who has never done it before. Intentionally use an expired or invalid token.
- **Justification:** Automation validates that the integration works when configured correctly. A human validates if the setup instructions are clear, if the tooltip hints make sense, and if the error messages for invalid tokens actually guide the user to a solution rather than just throwing a generic "401 Unauthorized."

## 6. Accessibility (A11y) & Keyboard Navigation "Feel"
- **Test:** Disconnect your mouse. Navigate the `ArchitectureRunDetail` findings, drill down into `DecisionTraceEntries`, and resolve findings using *only* the keyboard and a screen reader (e.g., NVDA/VoiceOver).
- **Justification:** Automated tools (like Axe) can verify if ARIA attributes are technically present. They cannot tell you if the tab order is logically confusing, or if the screen reader's context makes actual sense to a visually impaired user (e.g., knowing *which* specific finding you are currently accepting).

## 7. Explainability Trace Completeness "Gut Check"
- **Test:** Review a finding that has a `CompletenessRatio` of around 50-60%. Look at what data is missing (e.g., missing GraphNodeIds, missing Alternative Paths).
- **Justification:** The `ExplainabilityTraceCompletenessAnalyzer` calculates a mathematical score. A human needs to do a subjective "gut check" to see if a 60% score accurately reflects the perceived usefulness of the trace. If a trace scores highly but still feels useless to an auditor, the scoring algorithm's weighting needs subjective tuning.