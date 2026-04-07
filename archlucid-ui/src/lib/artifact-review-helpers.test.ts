import { describe, expect, it } from "vitest";

import {
  classifyArtifactView,
  getArtifactTypeDescription,
  getArtifactTypeLabel,
  prepareArtifactBodyText,
} from "./artifact-review-helpers";

describe("classifyArtifactView", () => {
  it("classifies markdown", () => {
    expect(classifyArtifactView("markdown", "X")).toBe("markdown");
  });

  it("classifies mermaid", () => {
    expect(classifyArtifactView("mermaid", "MermaidDiagram")).toBe("mermaid");
  });

  it("classifies json format", () => {
    expect(classifyArtifactView("json", "CostSummary")).toBe("json");
  });

  it("classifies DiagramAst by type", () => {
    expect(classifyArtifactView("txt", "DiagramAst")).toBe("json");
  });
});

describe("getArtifactTypeLabel", () => {
  it("returns friendly label for known type", () => {
    expect(getArtifactTypeLabel("CostSummary")).toContain("Cost");
  });

  it("splits unknown PascalCase", () => {
    expect(getArtifactTypeLabel("FooBar")).toBe("Foo Bar");
  });
});

describe("getArtifactTypeDescription", () => {
  it("returns non-empty for known type", () => {
    expect(getArtifactTypeDescription("Inventory").length).toBeGreaterThan(20);
  });
});

describe("prepareArtifactBodyText", () => {
  it("pretty-prints JSON", () => {
    const result = prepareArtifactBodyText('{"a":1}', "json", "CostSummary");

    expect(result.jsonPrettyFailed).toBe(false);
    expect(result.readableText).toContain("\n");
  });

  it("marks json failure when invalid", () => {
    const result = prepareArtifactBodyText("{", "json", "CostSummary");

    expect(result.jsonPrettyFailed).toBe(true);
  });
});
