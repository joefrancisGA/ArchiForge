import { describe, expect, it } from "vitest";

import { buildDefaultWizardValues, wizardFormSchema } from "./wizard-schema";

describe("wizardFormSchema", () => {
  it("accepts buildDefaultWizardValues", () => {
    const v = buildDefaultWizardValues();
    expect(() => wizardFormSchema.parse(v)).not.toThrow();
  });

  it("requires systemName with at least 2 non-whitespace characters", () => {
    const base = buildDefaultWizardValues();
    const r = wizardFormSchema.safeParse({ ...base, systemName: "" });
    expect(r.success).toBe(false);
    if (!r.success) {
      expect(r.error.flatten().fieldErrors.systemName?.join(" ")).toMatch(/System name is required/i);
    }

    const r2 = wizardFormSchema.safeParse({ ...base, systemName: "x" });
    expect(r2.success).toBe(false);
    if (!r2.success) {
      expect(r2.error.flatten().fieldErrors.systemName?.join(" ")).toMatch(/at least 2/i);
    }
  });

  it("requires description with at least 10 non-whitespace characters", () => {
    const base = buildDefaultWizardValues();
    const r = wizardFormSchema.safeParse({ ...base, description: "" });
    expect(r.success).toBe(false);
    if (!r.success) {
      expect(r.error.flatten().fieldErrors.description?.join(" ")).toMatch(/Required/i);
    }

    const r2 = wizardFormSchema.safeParse({ ...base, description: "short" });
    expect(r2.success).toBe(false);
    if (!r2.success) {
      expect(r2.error.flatten().fieldErrors.description?.join(" ")).toMatch(/at least 10/i);
    }
  });

  it("requires environment", () => {
    const base = buildDefaultWizardValues();
    const r = wizardFormSchema.safeParse({ ...base, environment: "" });
    expect(r.success).toBe(false);
    if (!r.success) {
      expect(r.error.flatten().fieldErrors.environment?.join(" ")).toMatch(/Required/i);
    }
  });

  it("trims systemName and description on parse", () => {
    const base = buildDefaultWizardValues();
    const parsed = wizardFormSchema.parse({
      ...base,
      systemName: "  OrderSvc  ",
      description: `  ${"a".repeat(10)}  `,
    });
    expect(parsed.systemName).toBe("OrderSvc");
    expect(parsed.description).toBe("a".repeat(10));
  });
});
