import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { RunDetailSkeleton } from "./RunDetailSkeleton";

describe("RunDetailSkeleton — aria", () => {
  it("marks busy state and label", () => {
    render(<RunDetailSkeleton />);
    const root = screen.getByLabelText("Loading run detail");

    expect(root).toHaveAttribute("aria-busy", "true");
  });
});
