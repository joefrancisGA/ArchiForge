import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { RunsListSkeleton } from "./RunsListSkeleton";

describe("RunsListSkeleton — aria", () => {
  it("marks busy state and label", () => {
    render(<RunsListSkeleton />);
    const root = screen.getByLabelText("Loading runs list");

    expect(root).toHaveAttribute("aria-busy", "true");
  });
});
