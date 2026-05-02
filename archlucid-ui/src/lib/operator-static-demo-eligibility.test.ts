import { describe, expect, it } from "vitest";

import {
  isStaticDemoPayloadFallbackActiveForManifest,
  isStaticDemoPayloadFallbackActiveForRun,
  tryStaticDemoRunDetail,
  tryStaticDemoRunSummariesPaged,
} from "@/lib/operator-static-demo";
import { SHOWCASE_STATIC_DEMO_MANIFEST_ID, SHOWCASE_STATIC_DEMO_RUN_ID } from "@/lib/showcase-static-demo";

describe("operator-static-demo — showcase eligibility without demo env vars", () => {
  it("ActiveForRun is true for canonical showcase id and legacy alias targets", () => {
    expect(isStaticDemoPayloadFallbackActiveForRun(SHOWCASE_STATIC_DEMO_RUN_ID)).toBe(true);
    expect(isStaticDemoPayloadFallbackActiveForRun("claims-intake-modernization-run")).toBe(true);
    expect(isStaticDemoPayloadFallbackActiveForRun("claims-intake-run-v1")).toBe(true);
    expect(isStaticDemoPayloadFallbackActiveForRun("not-a-demo-run")).toBe(false);
  });

  it("ActiveForManifest is true only for the showcase manifest UUID", () => {
    expect(isStaticDemoPayloadFallbackActiveForManifest(SHOWCASE_STATIC_DEMO_MANIFEST_ID)).toBe(true);
    expect(isStaticDemoPayloadFallbackActiveForManifest("00000000-0000-0000-0000-000000000001")).toBe(false);
  });

  it("tryStaticDemoRunDetail returns payload for showcase run id", () => {
    const d = tryStaticDemoRunDetail(SHOWCASE_STATIC_DEMO_RUN_ID);

    expect(d).not.toBeNull();
    expect(d?.run.runId).toBe(SHOWCASE_STATIC_DEMO_RUN_ID);
  });

  it("tryStaticDemoRunSummariesPaged returns null without env when afterAuthorityListFailure is omitted", () => {
    const paged = tryStaticDemoRunSummariesPaged("default");

    if (process.env.NEXT_PUBLIC_DEMO_MODE === "true" || process.env.NEXT_PUBLIC_DEMO_STATIC_OPERATOR === "true") {
      expect(paged).not.toBeNull();
    } else {
      expect(paged).toBeNull();
    }
  });

  it("tryStaticDemoRunSummariesPaged returns a row after authority list failure even when demo env is unset", () => {
    const originalDemo = process.env.NEXT_PUBLIC_DEMO_MODE;
    const originalStatic = process.env.NEXT_PUBLIC_DEMO_STATIC_OPERATOR;

    delete process.env.NEXT_PUBLIC_DEMO_MODE;
    delete process.env.NEXT_PUBLIC_DEMO_STATIC_OPERATOR;

    const paged = tryStaticDemoRunSummariesPaged("default", { afterAuthorityListFailure: true });

    expect(paged).not.toBeNull();
    expect(paged?.items.length).toBeGreaterThan(0);
    expect(paged?.items[0]?.runId).toBe(SHOWCASE_STATIC_DEMO_RUN_ID);

    if (originalDemo !== undefined) {
      process.env.NEXT_PUBLIC_DEMO_MODE = originalDemo;
    }

    if (originalStatic !== undefined) {
      process.env.NEXT_PUBLIC_DEMO_STATIC_OPERATOR = originalStatic;
    }
  });

  it("tryStaticDemoRunSummariesPaged returns a row after empty live list even when demo env is unset", () => {
    const originalDemo = process.env.NEXT_PUBLIC_DEMO_MODE;
    const originalStatic = process.env.NEXT_PUBLIC_DEMO_STATIC_OPERATOR;

    delete process.env.NEXT_PUBLIC_DEMO_MODE;
    delete process.env.NEXT_PUBLIC_DEMO_STATIC_OPERATOR;

    const paged = tryStaticDemoRunSummariesPaged("default", { afterEmptyLiveList: true });

    expect(paged).not.toBeNull();
    expect(paged?.items.length).toBeGreaterThan(0);
    expect(paged?.items[0]?.runId).toBe(SHOWCASE_STATIC_DEMO_RUN_ID);

    if (originalDemo !== undefined) {
      process.env.NEXT_PUBLIC_DEMO_MODE = originalDemo;
    }

    if (originalStatic !== undefined) {
      process.env.NEXT_PUBLIC_DEMO_STATIC_OPERATOR = originalStatic;
    }
  });
});
