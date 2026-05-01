import { fireEvent, render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { KeyboardShortcutProvider } from "@/components/KeyboardShortcutProvider";
import { parseKeyCombo } from "@/hooks/useKeyboardShortcuts";
import { SHORTCUTS } from "@/lib/shortcut-registry";

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

function fireCombo(combo: string, target: Window | Document | Element = window): void {
  const parsed = parseKeyCombo(combo);

  fireEvent.keyDown(target, {
    key: parsed.key,
    altKey: parsed.alt,
    ctrlKey: parsed.ctrl,
    metaKey: parsed.meta,
    shiftKey: parsed.shift,
    bubbles: true,
  });
}

describe("keyboard shortcuts global (integration)", () => {
  beforeEach(() => {
    mockPush.mockClear();
  });

  it("renders KeyboardShortcutProvider children", () => {
    render(
      <KeyboardShortcutProvider>
        <div>test</div>
      </KeyboardShortcutProvider>,
    );

    expect(screen.getByText("test")).toBeInTheDocument();
  });

  it("navigates with Alt+N, Alt+C, and Alt+H", () => {
    render(
      <KeyboardShortcutProvider>
        <div>test</div>
      </KeyboardShortcutProvider>,
    );

    fireCombo("alt+n");
    expect(mockPush).toHaveBeenLastCalledWith("/reviews/new");

    mockPush.mockClear();
    fireCombo("alt+c");
    expect(mockPush).toHaveBeenLastCalledWith("/compare");

    mockPush.mockClear();
    fireCombo("alt+h");
    expect(mockPush).toHaveBeenLastCalledWith("/");
  });

  it("opens help on Shift+? and closes the dialog on Escape", () => {
    render(
      <KeyboardShortcutProvider>
        <div>test</div>
      </KeyboardShortcutProvider>,
    );

    fireCombo("shift+?");

    expect(screen.getByRole("dialog")).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Keyboard shortcuts" })).toBeInTheDocument();

    fireEvent.keyDown(document, { key: "Escape", code: "Escape" });

    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  });

  it("calls router.push once per route for every SHORTCUTS entry that defines a route", () => {
    render(
      <KeyboardShortcutProvider>
        <div>test</div>
      </KeyboardShortcutProvider>,
    );

    for (const entry of SHORTCUTS) {
      if (entry.route === undefined || entry.route === "") {
        continue;
      }

      mockPush.mockClear();
      fireCombo(entry.key);

      expect(mockPush, `combo ${entry.key}`).toHaveBeenCalledTimes(1);
      expect(mockPush).toHaveBeenCalledWith(entry.route);
    }
  });

  it("does not call router.push when opening help with Shift+?", () => {
    render(
      <KeyboardShortcutProvider>
        <div>test</div>
      </KeyboardShortcutProvider>,
    );

    fireCombo("shift+?");

    expect(mockPush).not.toHaveBeenCalled();
    expect(screen.getByRole("heading", { name: "Keyboard shortcuts" })).toBeInTheDocument();
  });

});
