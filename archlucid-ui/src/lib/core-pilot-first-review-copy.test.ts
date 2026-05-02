import { describe, expect, it } from "vitest";

import {
  CORE_PILOT_FIRST_REVIEW_HEADING,
  CORE_PILOT_FIRST_REVIEW_HEADING_COMPACT,
  CORE_PILOT_FIRST_REVIEW_MINIMIZED_BUTTON,
  CORE_PILOT_FIRST_SESSION_GUIDANCE,
  CORE_PILOT_RUN_BRIDGE_LINE,
  CORE_PILOT_WORKFLOW_SUMMARY_LINE,
} from "./core-pilot-first-review-copy";

/**
 * Locks buyer-first Core Pilot chrome (hybrid model: "architecture review" in UI;
 * "run" remains API/ID spine). See docs/CORE_PILOT.md § first-session checklist
 * and QUALITY_ASSESSMENT_2026_05_01_INDEPENDENT_76_76 §9 item 1.
 */
describe("core-pilot-first-review-copy (buyer first-run)", () => {
  it("uses architecture-review language in primary headings", () => {
    expect(CORE_PILOT_FIRST_REVIEW_HEADING).toMatch(/architecture review/i);
    expect(CORE_PILOT_FIRST_REVIEW_HEADING).not.toMatch(/\brun\b/i);
  });

  it("keeps compact checklist label and bridge line for run vs review", () => {
    expect(CORE_PILOT_FIRST_REVIEW_HEADING_COMPACT).toContain("checklist");
    expect(CORE_PILOT_RUN_BRIDGE_LINE).toMatch(/architecture review/);
    expect(CORE_PILOT_RUN_BRIDGE_LINE.toLowerCase()).toContain("run");
  });

  it("summarizes the four-step flow without internal pipeline jargon", () => {
    expect(CORE_PILOT_WORKFLOW_SUMMARY_LINE).toMatch(/create review/i);
    expect(CORE_PILOT_WORKFLOW_SUMMARY_LINE).toMatch(/finalize/i);
    expect(CORE_PILOT_WORKFLOW_SUMMARY_LINE).toMatch(/review package/i);
  });

  it("offers a single-path first-session hint without extra jargon", () => {
    expect(CORE_PILOT_FIRST_SESSION_GUIDANCE.length).toBeGreaterThan(40);
    expect(CORE_PILOT_FIRST_SESSION_GUIDANCE.toLowerCase()).toMatch(/first/);
    expect(CORE_PILOT_FIRST_SESSION_GUIDANCE).toMatch(/review package/i);
  });
});
