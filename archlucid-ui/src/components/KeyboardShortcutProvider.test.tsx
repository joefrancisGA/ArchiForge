import { fireEvent, render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { ALERTS_PAGE_SHORTCUTS, SHORTCUTS } from "@/lib/shortcut-registry";

const { routerPush } = vi.hoisted(() => ({
  routerPush: vi.fn(),
}));

vi.mock("next/navigation", () => ({
  useRouter: () => ({ push: routerPush }),
}));

vi.mock("next/link", () => ({
  default: ({
    href,
    children,
    title,
    className,
  }: {
    href: string;
    children: import("react").ReactNode;
    title?: string;
    className?: string;
  }) => (
    <a href={href} title={title} className={className}>
      {children}
    </a>
  ),
}));

import { KeyboardShortcutProvider } from "./KeyboardShortcutProvider";

describe("KeyboardShortcutProvider", () => {
  beforeEach(() => {
    routerPush.mockClear();
  });

  it("renders children without visible shortcut help UI by default", () => {
    render(
      <KeyboardShortcutProvider>
        <div data-testid="child">Shell content</div>
      </KeyboardShortcutProvider>,
    );

    expect(screen.getByTestId("child")).toHaveTextContent("Shell content");
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    expect(screen.queryByText("Keyboard shortcuts")).not.toBeInTheDocument();
  });

  it("opens help dialog on Shift+? with heading and lists every shortcut description", () => {
    render(
      <KeyboardShortcutProvider>
        <span>app</span>
      </KeyboardShortcutProvider>,
    );

    fireEvent.keyDown(window, { key: "?", shiftKey: true });

    expect(screen.getByRole("dialog")).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Keyboard shortcuts" })).toBeInTheDocument();

    // Help content groups shortcuts in collapsible sections; hidden nodes are not matched by getByText.
    fireEvent.click(screen.getByRole("button", { name: "Show all navigation shortcuts" }));
    fireEvent.click(screen.getByRole("button", { name: "Show alerts page shortcuts" }));
    fireEvent.click(screen.getByRole("button", { name: "Show help overlay shortcut" }));

    for (const entry of SHORTCUTS) {
      expect(screen.getByText(entry.description)).toBeInTheDocument();
    }

    expect(screen.getByRole("heading", { name: "Alerts page" })).toBeInTheDocument();

    for (const entry of ALERTS_PAGE_SHORTCUTS) {
      expect(screen.getByText(entry.description)).toBeInTheDocument();
    }
  });

  it("closes the dialog on Escape", () => {
    render(
      <KeyboardShortcutProvider>
        <span>app</span>
      </KeyboardShortcutProvider>,
    );

    fireEvent.keyDown(window, { key: "?", shiftKey: true });
    expect(screen.getByRole("dialog")).toBeInTheDocument();

    fireEvent.keyDown(document, { key: "Escape", code: "Escape" });

    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  });
});
