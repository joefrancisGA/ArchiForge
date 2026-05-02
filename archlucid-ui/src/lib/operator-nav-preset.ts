/** Role-task presets: UI shaping only — server authorization remains authoritative. */

export type OperatorShellPresetId = "full" | "pilot_operator" | "governance_reviewer" | "analytics_investigator";

export const OPERATOR_SHELL_PRESET_STORAGE_KEY = "archlucid-nav-preset-id";

export const OPERATOR_SHELL_PRESET_ORDER: readonly OperatorShellPresetId[] = [
  "full",
  "pilot_operator",
  "governance_reviewer",
  "analytics_investigator",
];

/** Route path prefixes matched after stripping `?query` — longest wins when checking startswith semantics. */

const PREFIX_RULES: Record<Exclude<OperatorShellPresetId, "full">, readonly string[]> = {
  pilot_operator: ["/reviews", "/", "/help", "/onboarding"],
  governance_reviewer: ["/reviews", "/", "/help", "/governance", "/policy-packs", "/audit", "/alerts"],
  analytics_investigator: ["/graph", "/compare", "/replay", "/ask", "/search", "/", "/reviews", "/advisory", "/value-report"],
};


export function isOperatorShellPresetId(value: unknown): value is OperatorShellPresetId {
  return (
    value === "full"
    || value === "pilot_operator"
    || value === "governance_reviewer"
    || value === "analytics_investigator"
  );
}


/** True when preset allows sidebar href visibility (pathname + optional query discarded). */


export function operatorShellPresetAllowsHref(preset: OperatorShellPresetId, href: string): boolean {
  if (preset === "full") {


    return true;
  }


  const pathOnly = (href.split("?", 1)[0] ?? "").trim();

  const rules = PREFIX_RULES[preset];


  for (const prefix of rules) {

    if (pathOnly === prefix || pathOnly.startsWith(`${prefix}/`)) {

      return true;
    }
  }



  return false;
}
