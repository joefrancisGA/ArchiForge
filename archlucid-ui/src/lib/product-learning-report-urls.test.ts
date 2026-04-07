import { describe, expect, it } from "vitest";

import { buildProductLearningReportFileUrl, buildProductLearningReportJsonUrl } from "./product-learning-report-urls";

describe("product-learning-report-urls (58R)", () => {
  it("buildProductLearningReportFileUrl encodes format and optional since", () => {
    expect(buildProductLearningReportFileUrl("markdown", null)).toBe(
      "/api/proxy/v1/product-learning/report/file?format=markdown",
    );
    expect(buildProductLearningReportFileUrl("json", "2026-01-01T00:00:00.000Z")).toContain("format=json");
    expect(buildProductLearningReportFileUrl("json", "2026-01-01T00:00:00.000Z")).toContain(
      "since=2026-01-01T00%3A00%3A00.000Z",
    );
  });

  it("buildProductLearningReportJsonUrl targets structured report endpoint", () => {
    expect(buildProductLearningReportJsonUrl(null)).toBe("/api/proxy/v1/product-learning/report?format=json");
  });
});
