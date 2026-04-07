import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { SectionCard } from "@/components/SectionCard";

describe("SectionCard", () => {
  it("renders title and children", () => {
    render(
      <SectionCard title="Overview">
        <p>Body</p>
      </SectionCard>,
    );

    expect(screen.getByRole("heading", { name: "Overview" })).toBeInTheDocument();
    expect(screen.getByText("Body")).toBeInTheDocument();
  });
});
