/**
 * Condensed qualitative rows for the public **`/why`** page — summarized from **`docs/go-to-market/COMPETITIVE_LANDSCAPE.md`** §2.3 (*AI-native tools*).
 * Not a quantitative scorecard (that remains the deterministic hard-comparison grid + proof pack elsewhere on the page).
 */
export type WhyMarketLandscapeMarketingRow = {
  readonly dimension: string;
  readonly archlucid: string;
  readonly githubCopilotAdHocArchitecture: string;
  readonly manualChatgptClaude: string;
  readonly structurizrWithAssist: string;
};

export const WHY_MARKET_LANDSCAPE_MARKETING_ROWS: readonly WhyMarketLandscapeMarketingRow[] = [
  {
    dimension: "Pricing",
    archlucid: "Consumption-based SaaS (see pricing manifest in-repo).",
    githubCopilotAdHocArchitecture: "~$19–39/seat/month (code assist; not orchestrated architecture runs).",
    manualChatgptClaude: "~$20–25/seat/month (chat; unstructured output).",
    structurizrWithAssist: "OSS + optional SaaS tiers ($5–20/mo) — modeling first; limited automated analysis.",
  },
  {
    dimension: "AI capability",
    archlucid: "Multi-agent architecture pipeline + simulator + routed LLMs (ARCHITECTURE_CONTEXT.md in repo).",
    githubCopilotAdHocArchitecture: "Code completions; lacks manifest-scale architecture orchestration.",
    manualChatgptClaude: "Prompt-defined; brittle repeatability.",
    structurizrWithAssist: "Diagram DSL assist — no agentic findings pipeline comparable to Authority runs.",
  },
  {
    dimension: "Governance & audit posture",
    archlucid: "Committed manifests, segregation-of-duties gates, typed audit envelopes, replay exports (see COMPETITIVE_LANDSCAPE.md §3 for evidence list).",
    githubCopilotAdHocArchitecture: "No enterprise architecture audit trail tied to manifests.",
    manualChatgptClaude: "Conversation history only — not attestable lifecycle evidence.",
    structurizrWithAssist: "Version control over DSL artefacts — lacks governed promotion workflow parity.",
  },
];
