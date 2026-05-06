import { describe, expect, it } from "vitest";

import {
  CORE_PILOT_FIRST_REVIEW_HEADING,
  CORE_PILOT_FIRST_REVIEW_HEADING_COMPACT,
  CORE_PILOT_FIRST_REVIEW_MINIMIZED_BUTTON,
  CORE_PILOT_FIRST_SESSION_GUIDANCE,
  CORE_PILOT_WORKFLOW_SUMMARY_LINE,
} from "./core-pilot-first-review-copy";

/**
 * Locks buyer-first Core Pilot chrome (“architecture review” in UI). See docs/CORE_PILOT.md (first-session checklist).
 */
describe("core-pilot-first-review-copy (buyer first-run)", () => {
  it("uses outcome-first governed packaging language in primary heading", () => {
    expect(CORE_PILOT_FIRST_REVIEW_HEADING).toMatch(/governed/i);
    expect(CORE_PILOT_FIRST_REVIEW_HEADING).toMatch(/architecture review package/i);
    expect(CORE_PILOT_FIRST_REVIEW_HEADING).not.toMatch(/\brun\b/i);
  });

  it("keeps compact checklist label", () => {
    expect(CORE_PILOT_FIRST_REVIEW_HEADING_COMPACT).toContain("checklist");
  });

  it("summarizes the four-step flow without internal pipeline jargon", () => {
    expect(CORE_PILOT_WORKFLOW_SUMMARY_LINE).toMatch(/new review/i);
    expect(CORE_PILOT_WORKFLOW_SUMMARY_LINE).toMatch(/finalize/i);
    expect(CORE_PILOT_WORKFLOW_SUMMARY_LINE).toMatch(/manifest summary/i);
  });

  it("labels the minimized first-review control consistently", () => {
    expect(CORE_PILOT_FIRST_REVIEW_MINIMIZED_BUTTON.toLowerCase()).toContain("checklist");
    expect(CORE_PILOT_FIRST_REVIEW_MINIMIZED_BUTTON.toLowerCase()).toContain("first");
  });

  it("offers a single-path first-session hint without extra jargon", () => {
    expect(CORE_PILOT_FIRST_SESSION_GUIDANCE.length).toBeGreaterThan(40);
    expect(CORE_PILOT_FIRST_SESSION_GUIDANCE.toLowerCase()).toMatch(/first/);
    expect(CORE_PILOT_FIRST_SESSION_GUIDANCE).toMatch(/manifest summary/i);
  });
});
