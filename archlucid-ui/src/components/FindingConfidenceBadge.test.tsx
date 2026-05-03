import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { FindingConfidenceBadge } from "@/components/FindingConfidenceBadge";

describe("FindingConfidenceBadge", () => {
  it.each([
    ["High", "High confidence", "border-emerald-200"],
    ["Medium", "Medium confidence", "border-amber-200"],
    ["Low", "Low confidence", "border-orange-200"],
  ] as const)("renders %s with label and emphasis colour class", (level, label, borderClass) => {
    render(<FindingConfidenceBadge level={level} />);

    const badge = screen.getByRole("status", { name: label });

    expect(badge).toHaveAttribute("data-archlucid-confidence", level);
    expect(badge.className.includes(borderClass)).toBe(true);
  });

  it.each([null, undefined] as const)("renders nothing when level is %s", (level) => {
    const { container } = render(<FindingConfidenceBadge level={level} />);

    expect(container.querySelector(".finding-confidence-badge")).toBeNull();
  });
});
