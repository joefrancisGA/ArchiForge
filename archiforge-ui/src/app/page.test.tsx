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

import HomePage from "./page";

describe("HomePage", () => {
  it("renders Operator Shell heading and summary", () => {
    render(<HomePage />);

    expect(screen.getByRole("heading", { level: 2, name: "Operator Shell" })).toBeInTheDocument();
    expect(
      screen.getByText(/List runs, open run detail \(manifest summary \+ artifacts\)/),
    ).toBeInTheDocument();
  });

  it("renders quick links with expected destinations", () => {
    render(<HomePage />);

    expect(screen.getByText("Quick links:")).toBeInTheDocument();

    const runs = screen.getByRole("link", { name: "Runs" });
    expect(runs).toHaveAttribute("href", "/runs?projectId=default");

    const compare = screen.getByRole("link", { name: "Compare Runs" });
    expect(compare).toHaveAttribute("href", "/compare");

    const replay = screen.getByRole("link", { name: "Replay Run" });
    expect(replay).toHaveAttribute("href", "/replay");
  });
});
