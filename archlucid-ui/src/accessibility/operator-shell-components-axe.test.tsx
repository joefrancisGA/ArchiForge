import { act, fireEvent, render } from "@testing-library/react";
import { axe, toHaveNoViolations } from "jest-axe";
import { describe, expect, it } from "vitest";

import { ContextualHelp } from "@/components/ContextualHelp";
import { SectionCard } from "@/components/SectionCard";
import { ShortcutHint } from "@/components/ShortcutHint";

expect.extend(toHaveNoViolations);

describe("operator shell components — axe (Vitest)", () => {
  it("SectionCard has no accessibility violations", async () => {
    const { container } = render(
      <SectionCard title="Coverage section">
        <p>Body copy for the section.</p>
      </SectionCard>,
    );

    expect(await axe(container)).toHaveNoViolations();
  });

  it("ShortcutHint has no accessibility violations", async () => {
    const { container } = render(<ShortcutHint shortcut="Ctrl+K" />);

    expect(await axe(container)).toHaveNoViolations();
  });

  it("ContextualHelp has no accessibility violations when closed", async () => {
    const { container } = render(<ContextualHelp helpKey="new-run-wizard" />);

    expect(await axe(container)).toHaveNoViolations();
  });

  it("ContextualHelp has no accessibility violations when the tooltip is open with learn more", async () => {
    const { container, getByLabelText } = render(<ContextualHelp helpKey="commit-manifest" />);

    act(() => {
      fireEvent.click(getByLabelText(/help: commit-manifest/i));
    });

    expect(await axe(container)).toHaveNoViolations();
  });

  it("ContextualHelp (semantic-search) has no accessibility violations when open", async () => {
    const { container, getByLabelText } = render(<ContextualHelp helpKey="semantic-search" />);

    act(() => {
      fireEvent.click(getByLabelText(/help: semantic-search/i));
    });

    expect(await axe(container)).toHaveNoViolations();
  });

  it("ContextualHelp (ask-archlucid) has no accessibility violations when open", async () => {
    const { container, getByLabelText } = render(<ContextualHelp helpKey="ask-archlucid" />);

    act(() => {
      fireEvent.click(getByLabelText(/help: ask-archlucid/i));
    });

    expect(await axe(container)).toHaveNoViolations();
  });
});
