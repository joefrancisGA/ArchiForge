import { describe, expect, it } from "vitest";

import { applyWizardPreset, wizardPresets } from "@/lib/wizard-presets";
import { buildDefaultWizardValues, wizardFormSchema } from "@/lib/wizard-schema";

describe("wizardFormSchema", () => {
  it("rejects empty systemName", () => {
    const base = buildDefaultWizardValues();

    const result = wizardFormSchema.safeParse({
      ...base,
      systemName: "",
    });

    expect(result.success).toBe(false);
  });

  it("rejects description shorter than 10 characters", () => {
    const base = buildDefaultWizardValues();

    const result = wizardFormSchema.safeParse({
      ...base,
      description: "ninechars",
    });

    expect(result.success).toBe(false);
  });

  it("accepts a valid full payload", () => {
    const parsed = wizardFormSchema.safeParse({
      requestId: "a1b2c3d4e5f647899a0b1c2d3e4f5067",
      description: "At least ten characters of meaningful architecture context here.",
      systemName: "OrderService",
      environment: "prod",
      cloudProvider: "Azure",
      constraints: ["c1"],
      requiredCapabilities: ["cap1"],
      assumptions: ["assume1"],
      priorManifestVersion: "00000000-0000-0000-0000-000000000001",
      inlineRequirements: ["req1"],
      documents: [{ name: "adr.md", contentType: "text/markdown", content: "# ADR" }],
      policyReferences: ["pack:default"],
      topologyHints: ["hub-spoke"],
      securityBaselineHints: ["tls-1.2+"],
      infrastructureDeclarations: [{ name: "main.tf", format: "terraform", content: "resource \"x\" {}" }],
    });

    expect(parsed.success).toBe(true);
    if (parsed.success) {
      expect(parsed.data.documents).toHaveLength(1);
      expect(parsed.data.infrastructureDeclarations).toHaveLength(1);
    }
  });

  it("buildDefaultWizardValues produces a 32-char hex requestId (Guid N format)", () => {
    const values = buildDefaultWizardValues();

    expect(values.requestId).toMatch(/^[0-9a-f]{32}$/i);
    expect(values.cloudProvider).toBe("Azure");
    expect(values.environment).toBe("staging");
    expect(values.constraints).toEqual([]);
    expect(values.documents).toEqual([]);
  });

  it("presets merge cleanly into defaults via applyWizardPreset", () => {
    const base = buildDefaultWizardValues();
    const greenfield = wizardPresets.find((preset) => preset.id === "greenfield-web-app");
    expect(greenfield).toBeDefined();
    if (!greenfield) {
      return;
    }

    const merged = applyWizardPreset(base, greenfield.values);

    expect(merged.constraints).toEqual(["Must run on Azure", "Budget < $2000/month"]);
    expect(merged.requiredCapabilities).toEqual([
      "HTTPS ingress",
      "Managed database",
      "CI/CD pipeline",
    ]);
    expect(merged.systemName).toBe("CustomerWebApp");
    expect(wizardFormSchema.safeParse(merged).success).toBe(true);
  });

  it("blank preset leaves defaults unchanged aside from parse normalization", () => {
    const base = buildDefaultWizardValues();
    const blank = wizardPresets.find((preset) => preset.id === "blank-advanced");
    expect(blank).toBeDefined();
    if (!blank) {
      return;
    }

    const merged = applyWizardPreset(base, blank.values);

    expect(merged.requestId).toBe(base.requestId);
    expect(merged.environment).toBe("staging");
    expect(merged.constraints).toEqual([]);
  });
});
