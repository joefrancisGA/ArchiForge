import toml from "toml";

import { buildDefaultWizardValues, wizardFormSchema, type WizardFormValues } from "@/lib/wizard-schema";

export type SecondRunPasteResult =
  | { ok: true; values: WizardFormValues }
  | { ok: false; error: string };

function readString(record: Record<string, unknown>, ...keys: string[]): string | undefined {
  for (const key of keys) {
    const v = record[key];
    if (typeof v === "string" && v.trim().length > 0) {
      return v.trim();
    }
  }

  return undefined;
}

function readStringList(record: Record<string, unknown>, ...keys: string[]): string[] {
  for (const key of keys) {
    const v = record[key];
    if (Array.isArray(v)) {
      return v
        .filter((item): item is string => typeof item === "string")
        .map((s) => s.trim())
        .filter(Boolean);
    }
  }

  return [];
}

/** Maps SECOND_RUN `environment` to a wizard `<Select>` value (identity step options). */
export function normalizeEnvironmentForWizard(raw: string | undefined): string {
  const e = (raw ?? "staging").trim().toLowerCase();

  if (e === "prod" || e === "production") {
    return "production";
  }

  if (e === "staging") {
    return "staging";
  }

  if (e === "dev" || e === "development") {
    return "development";
  }

  if (e === "sandbox") {
    return "sandbox";
  }

  return "staging";
}

/**
 * Parses pasted SECOND_RUN.toml or JSON and merges into wizard defaults.
 * Mirrors the CLI `SecondRunInputParser` mapping closely enough for UI pre-fill.
 */
export function applySecondRunPasteToWizard(raw: string, defaults: WizardFormValues): SecondRunPasteResult {
  const trimmed = raw.trim();

  if (trimmed.length === 0) {
    return { ok: false, error: "Paste is empty." };
  }

  let record: Record<string, unknown>;

  try {
    if (trimmed.startsWith("{")) {
      record = JSON.parse(trimmed) as Record<string, unknown>;
    } else {
      record = toml.parse(trimmed) as Record<string, unknown>;
    }
  } catch (e) {
    const message = e instanceof Error ? e.message : "Could not parse TOML/JSON.";
    return { ok: false, error: message };
  }

  const name = readString(record, "name", "Name");

  if (!name) {
    return { ok: false, error: "Missing required field: name" };
  }

  const description = readString(record, "description", "Description");

  if (!description) {
    return { ok: false, error: "Missing required field: description" };
  }

  if (description.length < 10) {
    return { ok: false, error: "description must be at least 10 characters." };
  }

  const components = readStringList(record, "components", "Components");
  const dataStores = readStringList(record, "data_stores", "dataStores");
  const publicEndpoints = readStringList(record, "public_endpoints", "publicEndpoints");
  const compliance = readStringList(record, "compliance_posture", "compliancePosture");
  const constraints = readStringList(record, "constraints", "Constraints");
  const assumptions = readStringList(record, "assumptions", "Assumptions");
  const inlineExtra = readStringList(record, "inline_requirements", "inlineRequirements");

  const derivedInline = [
    ...dataStores.map((s) => `Datastore: ${s}`),
    ...publicEndpoints.map((s) => `Public endpoint: ${s}`),
  ];

  const requestIdRaw = readString(record, "request_id", "requestId");
  let requestId = defaults.requestId;

  if (requestIdRaw) {
    const compact = requestIdRaw.replace(/-/g, "").trim();

    if (compact.length === 0) {
      return { ok: false, error: "request_id is empty after trimming." };
    }

    if (compact.length > 64) {
      return { ok: false, error: "request_id must be at most 64 characters (without dashes)." };
    }

    requestId = compact;
  }

  const merged: WizardFormValues = {
    ...defaults,
    requestId,
    systemName: name,
    description,
    environment: normalizeEnvironmentForWizard(readString(record, "environment", "Environment")),
    cloudProvider: "Azure",
    requiredCapabilities: components,
    constraints,
    assumptions,
    inlineRequirements: [...inlineExtra, ...derivedInline],
    securityBaselineHints: compliance,
  };

  const parsed = wizardFormSchema.safeParse(merged);

  if (!parsed.success) {
    const first = parsed.error.issues[0];
    return { ok: false, error: first?.message ?? "Validation failed." };
  }

  return { ok: true, values: parsed.data };
}

export function buildWizardDefaultsForSecondRunPaste(): WizardFormValues {
  return buildDefaultWizardValues();
}
