/** Short definitions for operator UI glossary tooltips (keep in sync with docs tone; no second buyer story). */
export const GLOSSARY_TERMS = {
  governance_workflow:
    "Structured path to promote a committed manifest between environments: request → approvals → activation. Evidence stays tied to the run and manifest version.",
  golden_manifest:
    "Versioned manifest from an architecture run—the committed design record used for artifacts, comparisons, and governance checks.",
  policy_pack:
    "Versioned bundle of governance rules (severity, categories) applied when evaluating manifests for a scope.",
} as const;

export type GlossaryTermId = keyof typeof GLOSSARY_TERMS;
