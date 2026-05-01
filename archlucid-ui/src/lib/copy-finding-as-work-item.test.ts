import { describe, expect, it } from "vitest";

import { buildInspectFindingWorkItemBody, buildTraceRowWorkItemBody } from "./copy-finding-as-work-item";

describe("buildInspectFindingWorkItemBody", () => {
  const inspectInput = {
    runId: "r1",
    findingId: "f1",
    siteOrigin: "https://demo.example.org",
    severityLabel: "Warning",
    categoryLabel: "Compliance",
    impactedAreaLabel: "Data egress",
    title: "Exposed egress",
    description: "Outbound path not restricted.",
    recommendedAction: "Add firewall rules.",
    decisionRuleId: "rule-x",
    decisionRuleName: "Egress audit",
    evidenceExcerpts: ["subnet-1 (lines 12-14)", "diagram.png"],
  } as const;

  it("produces Markdown with links and headings", () => {
    const text = buildInspectFindingWorkItemBody("markdown", inspectInput);

    expect(text).toContain("## Finding: Compliance — Exposed egress");
    expect(text).toContain("`f1`");
    expect(text).toContain("`r1`");
    expect(text).toContain("- ArchLucid run: https://demo.example.org/runs/r1");
    expect(text).toContain("- Finding (explain page): https://demo.example.org/runs/r1/findings/f1");
  });

  it("uses Jira wiki markers for Jira variant", () => {
    const text = buildInspectFindingWorkItemBody("jiraWiki", inspectInput);

    expect(text).toContain("h2. ArchLucid Finding");
    expect(text).toContain("|ArchLucid finding — explain page)");
  });

  it("shows Not available sections when sparse", () => {
    const sparse = {
      runId: "r",
      findingId: "f",
      siteOrigin: "https://h.example.org",
      severityLabel: null,
      categoryLabel: null,
      impactedAreaLabel: null,
      title: null,
      description: null,
      recommendedAction: null,
      decisionRuleId: null,
      decisionRuleName: null,
      evidenceExcerpts: [],
    };

    const text = buildInspectFindingWorkItemBody("markdown", sparse);
    expect(text.includes("What was flagged")).toBe(true);
    expect(text.includes("Not available")).toBe(true);
  });
});

describe("buildTraceRowWorkItemBody", () => {
  it("lists relative paths for stubs", () => {
    const text = buildTraceRowWorkItemBody("markdown", {
      runId: "run-z",
      findingId: "find-z",
      findingTitle: "Title z",
      ruleId: "R1",
      siteOrigin: "https://demo.example.org",
    });

    expect(text).toContain("## Finding: architecture");
    expect(text).toContain("`find-z`");
    expect(text).toContain(`/reviews/run-z/findings/find-z`);
    expect(text).toContain("aggregate explanation table");
  });
});
