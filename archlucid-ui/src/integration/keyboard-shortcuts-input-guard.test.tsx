import { fireEvent, render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { KeyboardShortcutProvider } from "@/components/KeyboardShortcutProvider";
import { parseKeyCombo } from "@/hooks/useKeyboardShortcuts";

const { mockPush } = vi.hoisted(() => ({
  mockPush: vi.fn(),
}));

vi.mock("next/navigation", () => ({
  useRouter: () => ({ push: mockPush }),
  useSearchParams: () => new URLSearchParams(),
}));

vi.mock("next/link", () => ({
  default: ({ href, children }: { href: string; children: import("react").ReactNode }) => (
    <a href={href}>{children}</a>
  ),
}));

function fireComboOn(el: Element, combo: string): void {
  const parsed = parseKeyCombo(combo);

  fireEvent.keyDown(el, {
    key: parsed.key,
    altKey: parsed.alt,
    ctrlKey: parsed.ctrl,
    metaKey: parsed.meta,
    shiftKey: parsed.shift,
    bubbles: true,
  });
}

function FormFixture() {
  return (
    <form>
      <input aria-label="Test input" data-testid="shortcut-guard-input" />
      <textarea aria-label="Test textarea" data-testid="shortcut-guard-textarea" />
      <select aria-label="Test select" data-testid="shortcut-guard-select">
        <option value="a">A</option>
      </select>
      <div tabIndex={0} data-testid="shortcut-guard-plain">
        Plain focus target
      </div>
    </form>
  );
}

describe("keyboard shortcuts input guard (integration)", () => {
  beforeEach(() => {
    mockPush.mockClear();
  });

  it("does not navigate when focus is in an input or textarea", () => {
    render(
      <KeyboardShortcutProvider>
        <FormFixture />
      </KeyboardShortcutProvider>,
    );

    const input = screen.getByTestId("shortcut-guard-input");
    input.focus();
    fireComboOn(input, "alt+n");
    expect(mockPush).not.toHaveBeenCalled();

    const textarea = screen.getByTestId("shortcut-guard-textarea");
    textarea.focus();
    fireComboOn(textarea, "alt+c");
    expect(mockPush).not.toHaveBeenCalled();
  });

  it("does not navigate when focus is in a select", () => {
    render(
      <KeyboardShortcutProvider>
        <FormFixture />
      </KeyboardShortcutProvider>,
    );

    const select = screen.getByTestId("shortcut-guard-select");
    select.focus();
    fireComboOn(select, "alt+r");

    expect(mockPush).not.toHaveBeenCalled();
  });

  it("navigates when focus is on a non-editable element", () => {
    render(
      <KeyboardShortcutProvider>
        <FormFixture />
      </KeyboardShortcutProvider>,
    );

    const plain = screen.getByTestId("shortcut-guard-plain");
    plain.focus();
    fireComboOn(plain, "alt+n");

    expect(mockPush).toHaveBeenCalledTimes(1);
    expect(mockPush).toHaveBeenCalledWith("/reviews/new");
  });

  it("does not open help or navigate when Shift+? is pressed while focus is in an input", () => {
    render(
      <KeyboardShortcutProvider>
        <FormFixture />
      </KeyboardShortcutProvider>,
    );

    const input = screen.getByTestId("shortcut-guard-input");
    input.focus();
    fireComboOn(input, "shift+?");

    expect(mockPush).not.toHaveBeenCalled();
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  });
});
