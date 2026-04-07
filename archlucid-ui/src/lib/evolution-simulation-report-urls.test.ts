import { describe, expect, it } from "vitest";
import { buildEvolutionSimulationReportFileUrl } from "./evolution-simulation-report-urls";

describe("buildEvolutionSimulationReportFileUrl", () => {
  it("builds markdown and json proxy paths with encoded id and format", () => {
    const id = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";

    expect(buildEvolutionSimulationReportFileUrl(id, "markdown")).toBe(
      "/api/proxy/v1/evolution/results/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee/export?format=markdown",
    );
    expect(buildEvolutionSimulationReportFileUrl(id, "json")).toBe(
      "/api/proxy/v1/evolution/results/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee/export?format=json",
    );
  });
});
