import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

vi.mock("next/link", () => ({
  default: ({
    href,
    children,
  }: {
    href: string;
    children: import("react").ReactNode;
  }) => <a href={href}>{children}</a>,
}));

import NotFound from "./not-found";

describe("not-found", () => {
  it("renders operator-facing copy and navigation links", () => {
    render(<NotFound />);

    expect(screen.getByText("Page not found")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Home" })).toHaveAttribute("href", "/");
    expect(screen.getByRole("link", { name: "Runs" })).toHaveAttribute("href", "/reviews?projectId=default");
    expect(screen.getByRole("link", { name: "Findings" })).toHaveAttribute("href", "/governance/findings");
  });
});
