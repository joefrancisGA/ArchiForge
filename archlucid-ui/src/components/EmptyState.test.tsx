import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { Bell } from "lucide-react";

import { EmptyState } from "./EmptyState";

describe("EmptyState — renders core content", () => {
  it("shows title, description, and icon", () => {
    render(
      <EmptyState
        icon={Bell}
        title="Test title"
        description="Test description body."
        actions={[{ label: "Go", href: "/runs" }]}
      />,
    );

    expect(screen.getByRole("heading", { name: "Test title" })).toBeInTheDocument();
    expect(screen.getByText("Test description body.")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Go" })).toHaveAttribute("href", "/runs");
  });
});

describe("EmptyState — help link", () => {
  it("renders learn more when helpTopicPath is set", () => {
    render(
      <EmptyState title="T" description="D" actions={[]} helpTopicPath="alerts" />,
    );

    expect(screen.getByRole("link", { name: "Learn more" })).toHaveAttribute("href", "/getting-started#alerts");
  });

  it("omits learn more when helpTopicPath is absent", () => {
    render(<EmptyState title="T" description="D" actions={[]} />);

    expect(screen.queryByRole("link", { name: "Learn more" })).toBeNull();
  });
});

describe("EmptyState — actions", () => {
  it("uses outline for second action", () => {
    render(
      <EmptyState
        title="T"
        description="D"
        actions={[
          { label: "Primary", href: "/a" },
          { label: "Secondary", href: "/b", variant: "outline" },
        ]}
      />,
    );

    const primary = screen.getByRole("link", { name: "Primary" });
    const secondary = screen.getByRole("link", { name: "Secondary" });

    expect(primary.className).toMatch(/teal-700/);
    expect(secondary.className).toMatch(/border/);
  });
});

describe("EmptyState — status role", () => {
  it("exposes status landmark with aria-label", () => {
    render(<EmptyState title="Empty here" description="Nothing to see." actions={[]} />);

    expect(screen.getByRole("status", { name: "Empty here" })).toBeInTheDocument();
  });
});
