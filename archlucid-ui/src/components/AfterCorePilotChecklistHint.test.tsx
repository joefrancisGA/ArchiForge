import { fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it } from "vitest";

import { AfterCorePilotChecklistHint } from "./AfterCorePilotChecklistHint";

import {
  AFTER_CORE_PILOT_WHATS_NEXT_DISMISSED_KEY,
  CORE_PILOT_STEP_COUNT,
  corePilotStepDoneStorageKey,
} from "@/lib/core-pilot-checklist-storage";

function markAllCoreStepsDone() {
  for (let i = 0; i < CORE_PILOT_STEP_COUNT; i++) {
    localStorage.setItem(corePilotStepDoneStorageKey(i), "1");
  }
}

describe("AfterCorePilotChecklistHint", () => {
  afterEach(() => {
    localStorage.clear();
  });

  it("does not render when core checklist steps are incomplete", () => {
    localStorage.setItem(corePilotStepDoneStorageKey(0), "1");
    render(<AfterCorePilotChecklistHint />);

    expect(screen.queryByTestId("after-core-pilot-whats-next")).toBeNull();
  });

  it("renders suggested next steps when all core steps are done", async () => {
    markAllCoreStepsDone();
    render(<AfterCorePilotChecklistHint />);

    expect(await screen.findByTestId("after-core-pilot-whats-next")).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: /ready for more/i })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Compare two runs" })).toHaveAttribute("href", "/compare");
    expect(screen.getByRole("link", { name: "Explore the architecture graph" })).toHaveAttribute("href", "/graph");
    expect(screen.getByRole("link", { name: "Set up governance alerts" })).toHaveAttribute("href", "/alerts?tab=rules");
    expect(screen.getByRole("link", { name: "Review policy packs" })).toHaveAttribute("href", "/policy-packs");
  });

  it("persists dismiss to localStorage and hides the panel", async () => {
    markAllCoreStepsDone();
    const { unmount } = render(<AfterCorePilotChecklistHint />);
    expect(await screen.findByTestId("after-core-pilot-whats-next")).toBeInTheDocument();

    fireEvent.click(screen.getByTestId("after-core-pilot-whats-next-dismiss"));
    expect(localStorage.getItem(AFTER_CORE_PILOT_WHATS_NEXT_DISMISSED_KEY)).toBe("1");
    expect(screen.queryByTestId("after-core-pilot-whats-next")).toBeNull();

    unmount();
    markAllCoreStepsDone();
    render(<AfterCorePilotChecklistHint />);
    expect(screen.queryByTestId("after-core-pilot-whats-next")).toBeNull();
  });
});
