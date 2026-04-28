/**
 * Canonical re-export for operator glossary data (implements quality plan “glossary.ts” surface).
 * Source of truth for term copy remains `glossary-terms.ts` — keep anchors aligned with `docs/library/GLOSSARY.md`.
 */
export type { GlossaryTermEntry, GlossaryTermKey } from "./glossary-terms";

export { GLOSSARY_TERMS } from "./glossary-terms";
