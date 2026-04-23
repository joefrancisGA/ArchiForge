import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

import { describe, expect, it } from "vitest";

const __dirname = dirname(fileURLToPath(import.meta.url));
const uiRoot = join(__dirname, "..");

describe("deprecated public shims (TSDoc @deprecated)", () => {
  it("documents deprecation on useEnterpriseMutationCapability", () => {
    const path = join(uiRoot, "hooks", "use-enterprise-mutation-capability.ts");
    const src = readFileSync(path, "utf8");

    expect(src).toMatch(/@deprecated/);
    expect(src).toMatch(/useOperateCapability/);
  });

  it("documents deprecation on EnterpriseControlsContextHints re-export barrel", () => {
    const path = join(uiRoot, "components", "EnterpriseControlsContextHints.tsx");
    const src = readFileSync(path, "utf8");

    expect(src).toMatch(/@deprecated/);
    expect(src).toMatch(/OperateCapabilityHints/);
  });

  it("documents deprecation on enterpriseMutationCapabilityFromRank", () => {
    const path = join(uiRoot, "lib", "enterprise-mutation-capability.ts");
    const src = readFileSync(path, "utf8");

    expect(src).toMatch(/@deprecated/);
    expect(src).toMatch(/operateCapabilityFromRank/);
  });
});
