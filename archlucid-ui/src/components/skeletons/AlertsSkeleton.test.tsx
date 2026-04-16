import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { AlertsSkeleton } from "./AlertsSkeleton";

describe("AlertsSkeleton — aria", () => {
  it("marks busy state and label", () => {
    render(<AlertsSkeleton />);
    const root = screen.getByLabelText("Loading alerts");

    expect(root).toHaveAttribute("aria-busy", "true");
  });
});
