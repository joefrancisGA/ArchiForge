import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { Skeleton } from "./skeleton";

describe("Skeleton — default classes", () => {
  it("includes animate-pulse", () => {
    render(<Skeleton data-testid="sk" />);
    const el = screen.getByTestId("sk");

    expect(el.className).toMatch(/animate-pulse/);
  });
});

describe("Skeleton — merges className", () => {
  it("applies custom dimensions", () => {
    render(<Skeleton data-testid="sk" className="h-6 w-48" />);
    const el = screen.getByTestId("sk");

    expect(el.className).toMatch(/h-6/);
    expect(el.className).toMatch(/w-48/);
  });
});
