import { fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";

import { DRAFT_TOUR_STEPS, OptInTour, TOUR_DISMISSED_LOCAL_STORAGE_KEY } from "./OptInTour";
import { OptInTourLauncher } from "./OptInTourLauncher";

afterEach(() => {
  window.localStorage.clear();
  vi.restoreAllMocks();
});

describe("OptInTour script", () => {
  it("contains exactly five steps (PENDING_QUESTIONS.md item 38, owner Q8 — 2026-04-23)", () => {
    expect(DRAFT_TOUR_STEPS).toHaveLength(5);
  });

  it("every step body is non-empty", () => {
    for (const step of DRAFT_TOUR_STEPS) {
      expect(step.title.length).toBeGreaterThan(0);
      expect(step.body.length).toBeGreaterThan(0);
    }
  });
});

describe("OptInTour rendering (controlled)", () => {
  it("renders nothing when isOpen=false (no auto-launch on mount per owner Q9)", () => {
    render(<OptInTour isOpen={false} onClose={() => {}} />);

    expect(screen.queryByTestId("opt-in-tour-dialog")).toBeNull();
  });

  it("renders step 0 when isOpen=true", () => {
    render(<OptInTour isOpen={true} onClose={() => {}} />);

    expect(screen.getByTestId("opt-in-tour-dialog")).not.toBeNull();
    expect(screen.getByTestId("opt-in-tour-step-0")).not.toBeNull();
  });

  it("close button persists the dismissal LocalStorage flag", () => {
    const onClose = vi.fn();
    render(<OptInTour isOpen={true} onClose={onClose} />);

    fireEvent.click(screen.getByTestId("opt-in-tour-close"));

    expect(onClose).toHaveBeenCalledTimes(1);
    expect(window.localStorage.getItem(TOUR_DISMISSED_LOCAL_STORAGE_KEY)).not.toBeNull();
  });

  it("Next advances through every step then shows Finish on the last step", () => {
    render(<OptInTour isOpen={true} onClose={() => {}} />);

    expect(screen.getByTestId("opt-in-tour-step-0")).not.toBeNull();

    for (let i = 1; i < DRAFT_TOUR_STEPS.length; i++) {
      fireEvent.click(screen.getByTestId("opt-in-tour-next"));
      expect(screen.getByTestId(`opt-in-tour-step-${i}`)).not.toBeNull();
    }

    expect(screen.queryByTestId("opt-in-tour-next")).toBeNull();
    expect(screen.getByTestId("opt-in-tour-finish")).not.toBeNull();
  });

  it("renders approved step 0 copy as plain text", () => {
    render(<OptInTour isOpen={true} onClose={() => {}} />);

    const step0 = screen.getByTestId("opt-in-tour-step-0");
    expect(step0.textContent).toContain("Your starting point");
    expect(step0.textContent).not.toContain("<<tour");
  });

  it("renders approved title and body for all five steps (no pending-approval placeholders)", () => {
    render(<OptInTour isOpen={true} onClose={() => {}} />);

    for (let i = 0; i < DRAFT_TOUR_STEPS.length; i++) {
      const step = DRAFT_TOUR_STEPS[i]!;
      const el = screen.getByTestId(`opt-in-tour-step-${i}`);

      expect(el.textContent).toContain(step.title);
      expect(el).toHaveTextContent(step.body);
      expect(el.textContent).not.toMatch(/pending approval|TourStepPendingApproval/i);

      if (i < DRAFT_TOUR_STEPS.length - 1) fireEvent.click(screen.getByTestId("opt-in-tour-next"));
    }
  });
});

describe("OptInTourLauncher (owner Q9 — never auto-launch)", () => {
  it("does NOT render the tour dialog on mount", () => {
    render(<OptInTourLauncher />);

    expect(screen.getByTestId("opt-in-tour-launcher")).not.toBeNull();
    expect(screen.queryByTestId("opt-in-tour-dialog")).toBeNull();
  });

  it("renders the tour dialog only after the 'Show me around' button is clicked", () => {
    render(<OptInTourLauncher />);

    fireEvent.click(screen.getByTestId("opt-in-tour-launcher"));

    expect(screen.getByTestId("opt-in-tour-dialog")).not.toBeNull();
    expect(screen.getByTestId("opt-in-tour-step-0")).not.toBeNull();
  });

  it("re-opens the tour even after a previous dismissal flag is present", () => {
    window.localStorage.setItem(TOUR_DISMISSED_LOCAL_STORAGE_KEY, "2026-04-24T10:00:00.000Z");

    render(<OptInTourLauncher />);

    fireEvent.click(screen.getByTestId("opt-in-tour-launcher"));

    expect(screen.getByTestId("opt-in-tour-dialog")).not.toBeNull();
  });
});
