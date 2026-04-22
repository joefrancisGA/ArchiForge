import { describe, expect, it } from "vitest";

import { applySecondRunPasteToWizard, buildWizardDefaultsForSecondRunPaste, normalizeEnvironmentForWizard } from "./second-run-paste";

describe("second-run-paste", () => {
  it("parses TOML and maps datastore lines plus capabilities", () => {
    const toml = `
name = "Acme.Api"
description = "Handles orders with at least ten chars."
components = ["api", "worker"]
data_stores = ["Cosmos"]
public_endpoints = ["https://api.acme.test"]
compliance_posture = ["SOC2"]
environment = "prod"
`;
    const defaults = buildWizardDefaultsForSecondRunPaste();
    const result = applySecondRunPasteToWizard(toml, defaults);

    expect(result.ok).toBe(true);
    if (!result.ok) {
      return;
    }

    expect(result.values.systemName).toBe("Acme.Api");
    expect(result.values.requiredCapabilities).toEqual(["api", "worker"]);
    expect(result.values.inlineRequirements).toContain("Datastore: Cosmos");
    expect(result.values.inlineRequirements).toContain("Public endpoint: https://api.acme.test");
    expect(result.values.securityBaselineHints).toEqual(["SOC2"]);
    expect(result.values.environment).toBe("production");
  });

  it("parses JSON and rejects short description", () => {
    const defaults = buildWizardDefaultsForSecondRunPaste();
    const bad = applySecondRunPasteToWizard('{"name":"x","description":"short"}', defaults);
    expect(bad.ok).toBe(false);
  });

  it("normalizeEnvironmentForWizard maps prod to production", () => {
    expect(normalizeEnvironmentForWizard("prod")).toBe("production");
  });
});
