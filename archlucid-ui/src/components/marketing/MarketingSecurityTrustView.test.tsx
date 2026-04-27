import { render, screen, within } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { MarketingSecurityTrustView } from "./MarketingSecurityTrustView";
import { securityTrustEngagementRows } from "@/lib/security-trust-content";

describe("MarketingSecurityTrustView", () => {
  it("renders all four engagement rows from the content lib", () => {
    render(<MarketingSecurityTrustView />);

    expect(securityTrustEngagementRows).toHaveLength(4);

    for (const row of securityTrustEngagementRows) {
      const rowEl = screen.getByTestId(`assurance-row-${row.id}`);
      expect(within(rowEl).getByText(row.engagement)).toBeInTheDocument();
      expect(within(rowEl).getByText(row.vendor)).toBeInTheDocument();
      expect(within(rowEl).getByText(row.scope)).toBeInTheDocument();
      expect(within(rowEl).getByText(row.completedUtc)).toBeInTheDocument();
    }
  });

  it("does not expose redacted content or customer names", () => {
    const { container } = render(<MarketingSecurityTrustView />);
    const text = container.textContent ?? "";

    expect(text.toLowerCase()).not.toMatch(/cve-\d{4}-\d+/);
    expect(text.toLowerCase()).not.toMatch(/cvss[:\s]*\d/);
    expect(text.toLowerCase()).not.toMatch(/severity[:\s]+(critical|high|medium|low)/);
    expect(text.toLowerCase()).not.toMatch(/customer:\s*[a-z]/);
  });

  it("surfaces the NDA notice and points reviewers at security@archlucid.net", () => {
    render(<MarketingSecurityTrustView />);

    expect(screen.getAllByText(/NDA[- ]only/i).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/security@archlucid\.com/i).length).toBeGreaterThan(0);
  });

  it("flags the staging-chaos row as production-out-of-scope", () => {
    render(<MarketingSecurityTrustView />);

    const chaosRow = screen.getByTestId(
      "assurance-row-chaos-game-day-quarterly-staging-2026",
    );
    expect(
      within(chaosRow).getByText(/production chaos out-of-scope/i),
    ).toBeInTheDocument();
  });
});
