import { describe, expect, it } from "vitest";
import {
  buildLearningPlanningReportFileUrl,
  buildLearningPlanningReportJsonUrl,
} from "./learning-planning-report-urls";

describe("learning-planning-report-urls (59R)", () => {
  it("builds markdown file URL", () => {
    expect(buildLearningPlanningReportFileUrl("markdown")).toBe(
      "/api/proxy/v1/learning/report/file?format=markdown",
    );
  });

  it("builds json file URL", () => {
    expect(buildLearningPlanningReportFileUrl("json")).toBe("/api/proxy/v1/learning/report/file?format=json");
  });

  it("builds inline JSON report URL", () => {
    expect(buildLearningPlanningReportJsonUrl()).toBe("/api/proxy/v1/learning/report?format=json");
  });
});
