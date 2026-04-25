import { act, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";

import { ContextualHelp } from "./ContextualHelp";

function resetEnv() {
  return process.env.NEXT_PUBLIC_ARCHLUCID_DOCS_BLOB_BASE;
}

describe("ContextualHelp", () => {
  afterEach(() => {
    vi.unstubAllEnvs();
  });

  it("renders tooltip on click and toggles on second click", async () => {
    const { getByLabelText, queryByRole, getByText } = render(
      <ContextualHelp helpKey="new-run-wizard" />,
    );

    const button = getByLabelText(/help: new-run-wizard/i);
    expect(queryByRole("tooltip")).toBeNull();

    act(() => {
      fireEvent.click(button);
    });

    expect(await screen.findByRole("tooltip")).toBeInTheDocument();
    expect(getByText(/create an architecture request/i)).toBeInTheDocument();

    act(() => {
      fireEvent.click(button);
    });

    await waitFor(() => {
      expect(screen.queryByRole("tooltip")).toBeNull();
    });
  });

  it("renders learn-more link when the entry defines learnMoreUrl", () => {
    vi.stubEnv("NEXT_PUBLIC_ARCHLUCID_DOCS_BLOB_BASE", "https://example.com/prefix");

    render(<ContextualHelp helpKey="commit-manifest" />);
    const button = screen.getByLabelText(/help: commit-manifest/i);

    act(() => {
      fireEvent.click(button);
    });

    const more = screen.getByRole("link", { name: /learn more/i });
    expect(more.getAttribute("href")).toBe("https://example.com/prefix/docs/CORE_PILOT.md#commit");
    expect(more).toHaveAttribute("target", "_blank");
  });

  it("associates the trigger with the tooltip for assistive technology when open", () => {
    const { getByLabelText, getByRole } = render(<ContextualHelp helpKey="manifest-review" />);
    const button = getByLabelText(/help: manifest-review/i);

    act(() => {
      fireEvent.click(button);
    });

    const tooltip = getByRole("tooltip");
    const tid = tooltip.getAttribute("id");

    expect(tid).toBeTruthy();
    expect(button).toHaveAttribute("aria-describedby", tid as string);
    expect(button).toHaveAttribute("aria-expanded", "true");
    expect(button).toHaveAttribute("aria-controls", tid as string);
  });

  it("is keyboard accessible: Enter and Space toggle", () => {
    const { getByLabelText, queryByRole } = render(<ContextualHelp helpKey="governance-gate" />);
    const button = getByLabelText(/help: governance-gate/i) as HTMLButtonElement;

    act(() => {
      button.focus();
      fireEvent.keyDown(button, { key: " ", code: "Space" });
    });

    expect(queryByRole("tooltip")).toBeInTheDocument();

    act(() => {
      fireEvent.keyDown(button, { key: " ", code: "Space" });
    });

    expect(queryByRole("tooltip")).toBeNull();
  });
});
