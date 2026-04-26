import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { PolicyPackDiffView } from "./PolicyPackDiffView";
import type { PolicyPackVersion } from "@/types/policy-packs";

function version(overrides: Partial<PolicyPackVersion> & Pick<PolicyPackVersion, "version" | "contentJson">) {
  return {
    policyPackVersionId: overrides.policyPackVersionId ?? "vid-default",
    policyPackId: overrides.policyPackId ?? "pack-1",
    version: overrides.version,
    contentJson: overrides.contentJson,
    createdUtc: overrides.createdUtc ?? "2026-04-01T12:00:00Z",
    isPublished: overrides.isPublished ?? true,
  } satisfies PolicyPackVersion;
}

describe("PolicyPackDiffView", () => {
  it("renders header with version labels and diff labels for added, removed, changed", () => {
    const left = version({
      policyPackVersionId: "left-id",
      version: "1.0.0",
      contentJson: JSON.stringify({ a: 1, drop: true }),
    });
    const right = version({
      policyPackVersionId: "right-id",
      version: "2.0.0",
      contentJson: JSON.stringify({ a: 2, added: "x" }),
    });

    render(<PolicyPackDiffView leftVersion={left} rightVersion={right} />);

    expect(screen.getByText("Content diff")).toBeInTheDocument();
    expect(screen.getByText("1.0.0")).toBeInTheDocument();
    expect(screen.getByText("2.0.0")).toBeInTheDocument();

    const added = document.querySelector('[data-change-type="added"]');
    const removed = document.querySelector('[data-change-type="removed"]');
    const changed = document.querySelector('[data-change-type="changed"]');

    expect(added).not.toBeNull();
    expect(removed).not.toBeNull();
    expect(changed).not.toBeNull();

    expect(added?.getAttribute("data-diff-path")).toBe("added");
    expect(removed?.getAttribute("data-diff-path")).toBe("drop");
    expect(changed?.getAttribute("data-diff-path")).toBe("a");

    expect(added?.querySelector("[data-diff-label]")?.textContent).toContain("Added");
    expect(removed?.querySelector("[data-diff-label]")?.textContent).toContain("Removed");
    expect(changed?.querySelector("[data-diff-label]")?.textContent).toContain("Changed");
  });

  it("marks added and removed cards with green and red tones (data attributes + bg-* classes)", () => {
    const left = version({
      version: "L",
      contentJson: '{"onlyLeft":1}',
    });
    const right = version({
      version: "R",
      contentJson: '{"onlyRight":2}',
    });

    render(<PolicyPackDiffView leftVersion={left} rightVersion={right} />);

    const added = document.querySelector('[data-change-type="added"]') as HTMLElement;
    const removed = document.querySelector('[data-change-type="removed"]') as HTMLElement;

    expect(added?.getAttribute("data-card-tone")).toBe("added");
    expect(removed?.getAttribute("data-card-tone")).toBe("removed");
    expect(added).toHaveClass("bg-emerald-50", "border-emerald-300", "text-emerald-900");
    expect(removed).toHaveClass("bg-red-50", "border-red-300", "text-red-900");
  });

  it("shows changed card with Before/After and yellow tone", () => {
    const left = version({
      version: "L",
      contentJson: '{"k":1}',
    });
    const right = version({
      version: "R",
      contentJson: '{"k":2}',
    });

    render(<PolicyPackDiffView leftVersion={left} rightVersion={right} />);

    expect(screen.getByText("Before")).toBeInTheDocument();
    expect(screen.getByText("After")).toBeInTheDocument();

    const changed = document.querySelector('[data-change-type="changed"]') as HTMLElement;
    expect(changed?.getAttribute("data-card-tone")).toBe("changed");
    expect(changed).toHaveClass("bg-yellow-50", "border-yellow-300", "text-yellow-900");
  });

  it("shows parse error for invalid JSON", () => {
    const left = version({ version: "L", contentJson: "NOT JSON" });
    const right = version({ version: "R", contentJson: "{}" });

    render(<PolicyPackDiffView leftVersion={left} rightVersion={right} />);

    expect(screen.getByRole("alert")).toHaveTextContent(/Could not parse/);
  });
});
