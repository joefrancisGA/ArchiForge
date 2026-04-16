import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { GenericPageSkeleton } from "./GenericPageSkeleton";

describe("GenericPageSkeleton — aria", () => {
  it("marks busy state and label", () => {
    render(<GenericPageSkeleton />);
    const root = screen.getByLabelText("Loading page content");

    expect(root).toHaveAttribute("aria-busy", "true");
  });
});
