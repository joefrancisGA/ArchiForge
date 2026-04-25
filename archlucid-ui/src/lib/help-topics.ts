import { DEFAULT_GITHUB_BLOB_BASE } from "./docs-public-base";

/**
 * Static contextual help index for the operator shell. Doc paths are relative to the repository root.
 */
export type HelpTopic = {
  id: string;
  title: string;
  keywords: string[];
  summary: string;
  /** Relative path under repo root (for copy/paste; web URL via getDocHref). */
  docPath: string;
  /** App routes where this topic is most relevant (pathname prefix or exact). */
  routes: string[];
};

export const HELP_TOPICS: HelpTopic[] = [
  {
    id: "first-run",
    title: "First run workflow",
    keywords: ["wizard", "create", "pipeline", "run"],
    summary: "Use New run to submit a request, then track the authority pipeline until the golden manifest is ready.",
    docPath: "docs/FIRST_RUN_WIZARD.md",
    routes: ["/runs/new", "/", "/getting-started"],
  },
  {
    id: "artifacts",
    title: "Reviewing artifacts",
    keywords: ["download", "manifest", "bundle", "zip"],
    summary: "Open a run, then review artifact list, previews, and bundle downloads from run detail.",
    docPath: "docs/operator-shell.md",
    routes: ["/runs"],
  },
  {
    id: "compare",
    title: "Compare two runs",
    keywords: ["diff", "delta", "replay"],
    summary: "Use Compare to diff two runs’ manifests and persisted comparison records.",
    docPath: "docs/COMPARISON_REPLAY.md",
    routes: ["/compare"],
  },
  {
    id: "replay",
    title: "Replay",
    keywords: ["verify", "drift", "comparison"],
    summary: "Replay re-executes stored comparison logic; verify mode detects drift.",
    docPath: "docs/COMPARISON_REPLAY.md",
    routes: ["/replay"],
  },
  {
    id: "graph",
    title: "Architecture graph",
    keywords: ["provenance", "knowledge graph"],
    summary: "Graph shows one run’s provenance or architecture view for a single run ID.",
    docPath: "docs/KNOWLEDGE_GRAPH.md",
    routes: ["/graph"],
  },
  {
    id: "alerts",
    title: "Alerts",
    keywords: ["inbox", "ack", "noise"],
    summary:
      "Alerts hub: inbox, rules, routing, composite, and simulation & tuning on one page (`/alerts?tab=`).",
    docPath: "docs/ALERTS.md",
    routes: ["/alerts", "/alerts?tab=rules", "/alerts?tab=simulation"],
  },
  {
    id: "policy-packs",
    title: "Policy packs",
    keywords: ["governance", "compliance", "pack"],
    summary: "Policy packs bundle rules and defaults; assign scope and inspect effective governance.",
    docPath: "docs/API_CONTRACTS.md",
    routes: ["/policy-packs", "/governance-resolution"],
  },
  {
    id: "system-health",
    title: "System health dashboard",
    keywords: ["ready", "health", "circuit", "diagnostics", "metrics"],
    summary: "In-app readiness checks, circuit breaker gates, and onboarding funnel counters — same signals as CLI doctor without leaving the shell.",
    docPath: "docs/library/OBSERVABILITY.md",
    routes: ["/admin/health"],
  },
  {
    id: "troubleshooting",
    title: "Troubleshooting",
    keywords: ["error", "503", "401", "health", "proxy"],
    summary: "Use health endpoints, CLI doctor, and support bundle for triage.",
    docPath: "docs/TROUBLESHOOTING.md",
    routes: [],
  },
  {
    id: "auth",
    title: "Authentication",
    keywords: ["jwt", "entra", "api key", "bearer"],
    summary: "Match UI auth mode to API ArchLucidAuth; API key is server-side in the Next.js proxy.",
    docPath: "docs/LIVE_E2E_JWT_SETUP.md",
    routes: ["/auth/signin"],
  },
  {
    id: "cli",
    title: "CLI",
    keywords: ["archlucid", "dotnet run", "terminal"],
    summary: "CLI commands call the HTTP API; use doctor and support-bundle for diagnostics.",
    docPath: "docs/CLI_USAGE.md",
    routes: [],
  },
  {
    id: "support-bundle",
    title: "Support bundle",
    keywords: ["zip", "triage", "ticket"],
    summary: "CLI support-bundle collects sanitized health and contract probes for support tickets.",
    docPath: "docs/TROUBLESHOOTING.md",
    routes: [],
  },
  {
    id: "scope",
    title: "Tenant / workspace / project scope",
    keywords: ["headers", "x-tenant-id", "isolation"],
    summary: "Scope headers isolate data; keep the same scope between UI and API integrations.",
    docPath: "docs/GLOSSARY.md",
    routes: [],
  },
  {
    id: "pilot-feedback",
    title: "Pilot feedback",
    keywords: ["58r", "triage", "learning"],
    summary: "Pilot feedback captures human judgments separately from recommendation learning.",
    docPath: "docs/PRODUCT_LEARNING.md",
    routes: ["/product-learning"],
  },
];

/**
 * Full URL to the doc on the web. Uses NEXT_PUBLIC_DOCS_BASE_URL when set; otherwise the public
 * ArchiForge GitHub blob URL for `main`.
 */
export function getDocHref(docPath: string): string | null {
  const relative = docPath?.trim() ?? "";

  if (relative.length === 0) {
    return null;
  }

  const custom = process.env.NEXT_PUBLIC_DOCS_BASE_URL?.trim();
  const base = custom && custom.length > 0 ? custom : DEFAULT_GITHUB_BLOB_BASE;
  const normalized = base.replace(/\/$/, "");
  const path = relative.replace(/^\//, "");

  return `${normalized}/${path}`;
}

export function filterHelpTopics(query: string, pathname: string): HelpTopic[] {
  const q = query.trim().toLowerCase();

  if (q.length === 0) {
    const byRoute = HELP_TOPICS.filter((topic) =>
      topic.routes.some((route) => pathname === route || pathname.startsWith(`${route}/`)),
    );

    return byRoute.length > 0 ? byRoute : HELP_TOPICS;
  }

  const scored = HELP_TOPICS.map((topic) => {
    let score = 0;

    for (const route of topic.routes) {
      if (pathname === route || pathname.startsWith(`${route}/`)) {
        score += 3;
      }
    }

    if (topic.title.toLowerCase().includes(q)) {
      score += 5;
    }

    if (topic.summary.toLowerCase().includes(q)) {
      score += 2;
    }

    for (const kw of topic.keywords) {
      if (kw.includes(q) || q.includes(kw)) {
        score += 2;
      }
    }

    return { topic, score };
  });

  const matched = scored.filter((x) => x.score > 0).sort((a, b) => b.score - a.score);

  if (matched.length > 0) {
    return matched.map((x) => x.topic);
  }

  return HELP_TOPICS.filter(
    (topic) =>
      topic.title.toLowerCase().includes(q) ||
      topic.summary.toLowerCase().includes(q) ||
      topic.keywords.some((k) => k.includes(q)),
  );
}
