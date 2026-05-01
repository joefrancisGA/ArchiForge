/** Clipboard targets for pasted work items (Markdown / wiki). */
export type WorkItemClipboardFormat = "markdown" | "jiraWiki" | "githubMarkdown" | "azureDevOpsMarkdown";

/** Fields assembled from inspect payload + UI labels for a full finding work item. */
export type FindingWorkItemBuildInput = {
  runId: string;
  findingId: string;
  /** `window.location.origin` in browser; SSR may pass "". */
  siteOrigin: string;
  severityLabel: string | null;
  categoryLabel: string | null;
  impactedAreaLabel: string | null;
  title: string | null;
  description: string | null;
  recommendedAction: string | null;
  decisionRuleId: string | null;
  decisionRuleName: string | null;
  evidenceExcerpts: string[];
};

function na(value: string | null | undefined): string {
  const t = value?.trim();

  if (t === undefined || t === null || t.length === 0) {
    return "Not available";
  }

  return t;
}

function findingHeading(categoryLabel: string | null, title: string | null): string {
  const parts = [categoryLabel?.trim(), title?.trim()].filter((x) => x !== undefined && x !== null && x.length > 0);

  return parts.join(" — ") || "ArchLucid finding";
}

function ruleSummary(decisionRuleName: string | null, decisionRuleId: string | null): string {
  const name = decisionRuleName?.trim();
  const id = decisionRuleId?.trim();

  const nameOk = name !== undefined && name.length > 0;
  const idOk = id !== undefined && id.length > 0;

  if (nameOk && idOk) {
    return `${name} (${id})`;
  }

  if (nameOk) {
    return name;
  }

  if (idOk) {
    return id;
  }

  return "Not available";
}

/** Minimal block for per-finding table rows (aggregate explanation list). */
export type TraceRowWorkItemInput = {
  runId: string;
  findingId: string;
  findingTitle: string | null;
  ruleId: string | null;
  siteOrigin: string;
};

/** Builds pasted text for [`RunFindingExplainabilityTable`](/components/RunFindingExplainabilityTable) rows. */
export function buildTraceRowWorkItemBody(format: WorkItemClipboardFormat, input: TraceRowWorkItemInput): string {
  const origin = input.siteOrigin.replace(/\/$/, "");
  const runPath = `/reviews/${encodeURIComponent(input.runId)}`;
  const findingPath = `${runPath}/findings/${encodeURIComponent(input.findingId)}`;
  const inspectPath = `${findingPath}/inspect`;
  const title = na(input.findingTitle);
  const rule = na(input.ruleId);

  if (format === "jiraWiki") {
    const lines = [
      `h2. ArchLucid Finding — ${title}`,
      "",
      `*Finding ID:* {{${input.findingId}}}`,
      "",
      `*Rule id:* ${rule}`,
      "",
      "*Links:*",
      `* (${origin || "(origin)"}${runPath}|ArchLucid run)`,
      `* (${origin || "(origin)"}${findingPath}|Finding — explain page)`,
      `* (${origin || "(origin)"}${inspectPath}|Structured inspector — Why?)`,
    ];

    return lines.join("\n");
  }

  return [
    "## Finding: architecture — " + title,
    "",
    `**Severity:** Not available`,
    `**Finding ID:** \`${input.findingId}\``,
    `**Run:** \`${input.runId}\``,
    `**Rule id:** ${rule}`,
    "",
    "### Links",
    `- ArchLucid run: ${origin}${runPath}`,
    `- Finding (explain page): ${origin}${findingPath}`,
    `- Structured inspector: ${origin}${inspectPath}`,
    "",
    "_Generated from aggregate explanation table — open Finding details for full inspect payload._",
  ].join("\n");
}

/**
 * Builds Markdown or wiki text for copying into Jira, GitHub Issues, or Azure Boards.
 */
export function buildInspectFindingWorkItemBody(format: WorkItemClipboardFormat, input: FindingWorkItemBuildInput): string {
  const origin = input.siteOrigin.replace(/\/$/, "");
  const base = `${origin}/runs/${encodeURIComponent(input.runId)}`;
  const explainUrl = `${base}/findings/${encodeURIComponent(input.findingId)}`;
  const inspectUrl = `${explainUrl}/inspect`;
  const heading = findingHeading(input.categoryLabel, input.title);

  const whatFlagged = na(input.description);

  const whyItMatters = na(input.impactedAreaLabel);
  const reco = na(input.recommendedAction);
  const severity = na(input.severityLabel);
  const ruleLine = ruleSummary(input.decisionRuleName, input.decisionRuleId);

  const evidenceBlock =
    input.evidenceExcerpts.length > 0 ? input.evidenceExcerpts.map((e) => `- ${na(e)}`).join("\n") : "- Not available";

  if (format === "jiraWiki") {
    const lines = [
      `h2. ArchLucid Finding — ${heading}`,
      "",
      "*Severity:* " + severity,
      `*Finding ID:* {{${input.findingId}}}`,
      "",
      "*Run:* " + "`" + input.runId + "`",
      `*Decision rule:* ${ruleLine}`,
      "",
      "*What was flagged*",
      whatFlagged,
      "",
      "*Why it matters*",
      whyItMatters,
      "",
      "*Recommended actions*",
      reco,
      "",
      "*Evidence*",
      ...(input.evidenceExcerpts.length > 0
        ? input.evidenceExcerpts.map((e) => `* ${na(e)}`)
        : ["* Not available"]),
      "",
      "*Links*",
      `* (${explainUrl}|ArchLucid finding — explain page)`,
      `* (${inspectUrl}|Structured inspector — Why?)`,
    ];

    return lines.join("\n");
  }

  return [
    `## Finding: ${heading}`,
    "",
    "**Severity:** " + severity,
    "**Finding ID:** `" + input.findingId + "`",
    "**Run:** `" + input.runId + "`",
    "**Decision rule:** " + ruleLine,
    "",
    "### What was flagged",
    whatFlagged,
    "",
    "### Why it matters",
    whyItMatters,
    "",
    "### Recommended actions",
    reco,
    "",
    "### Evidence",
    evidenceBlock,
    "",
    "### Links",
    `- ArchLucid run: ${base}`,
    `- Finding (explain page): ${explainUrl}`,
    `- Structured inspector: ${inspectUrl}`,
  ].join("\n");
}

export async function writeWorkItemBodyToClipboard(text: string): Promise<boolean> {
  if (typeof navigator !== "undefined" && navigator.clipboard?.writeText) {
    try {
      await navigator.clipboard.writeText(text);

      return true;
    } catch {
      /* fall through */
    }
  }

  try {
    if (typeof document === "undefined") {
      return false;
    }

    const ta = document.createElement("textarea");
    ta.value = text;
    ta.setAttribute("aria-hidden", "true");
    document.body.appendChild(ta);
    ta.select();
    const ok = document.execCommand("copy");
    document.body.removeChild(ta);

    return ok;
  } catch {
    return false;
  }
}
