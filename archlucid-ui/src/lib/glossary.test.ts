import { describe, expect, it } from "vitest";

import { GLOSSARY_TERMS, type GlossaryTermKey } from "@/lib/glossary";

describe("glossary", () => {
  it("re-exports stable operator term keys from glossary-terms", () => {
    const keys: GlossaryTermKey[] = Object.keys(GLOSSARY_TERMS) as GlossaryTermKey[];

    expect(keys.length).toBeGreaterThan(10);
    expect(keys).toContain("run");
    expect(keys).toContain("policy_pack");
    expect(GLOSSARY_TERMS.run.term.length).toBeGreaterThan(3);
  });
});
