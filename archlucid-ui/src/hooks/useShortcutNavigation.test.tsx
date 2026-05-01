import { fireEvent, renderHook } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

const { routerPush } = vi.hoisted(() => ({
  routerPush: vi.fn(),
}));

vi.mock("next/navigation", () => ({
  useRouter: () => ({ push: routerPush }),
}));

import { useShortcutNavigation } from "./useShortcutNavigation";

describe("useShortcutNavigation", () => {
  beforeEach(() => {
    routerPush.mockClear();
  });

  it("returns SHORTCUTS for display", () => {
    const { result } = renderHook(() => useShortcutNavigation());

    expect(result.current.shortcuts.length).toBeGreaterThan(0);
    expect(result.current.shortcuts.some((s) => s.key === "alt+n")).toBe(true);
  });

  it("calls router.push with /runs/new when Alt+N is pressed", () => {
    renderHook(() => useShortcutNavigation());

    fireEvent.keyDown(window, { key: "n", altKey: true });

    expect(routerPush).toHaveBeenCalledWith("/reviews/new");
  });

  it("invokes onHelpRequested for Shift+?", () => {
    const onHelpRequested = vi.fn();

    renderHook(() => useShortcutNavigation({ onHelpRequested }));

    fireEvent.keyDown(window, { key: "?", shiftKey: true });

    expect(onHelpRequested).toHaveBeenCalledTimes(1);
  });
});
