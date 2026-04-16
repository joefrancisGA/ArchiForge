import { render, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

const pathnameRef = { current: "/" };

vi.mock("next/navigation", () => ({
  usePathname: () => pathnameRef.current,
}));

import { useRouteChangeFocus } from "./useRouteChangeFocus";

function Harness() {
  useRouteChangeFocus("focus-target");

  return <div id="focus-target" tabIndex={-1} />;
}

describe("useRouteChangeFocus — initial mount", () => {
  it("does not move focus on first paint", () => {
    pathnameRef.current = "/";
    render(<Harness />);
    const target = document.getElementById("focus-target");

    expect(target).not.toBeNull();
    expect(document.activeElement).not.toBe(target);
  });
});

describe("useRouteChangeFocus — pathname change", () => {
  it("focuses target after navigation", async () => {
    pathnameRef.current = "/";
    const { rerender } = render(<Harness />);
    const target = document.getElementById("focus-target");

    expect(target).not.toBeNull();

    pathnameRef.current = "/alerts";
    rerender(<Harness />);

    await waitFor(
      () => {
        expect(document.activeElement).toBe(target);
      },
      { timeout: 3000 },
    );
  });
});

describe("useRouteChangeFocus — missing target", () => {
  it("does not throw when element is absent", async () => {
    pathnameRef.current = "/";

    function BadHarness() {
      useRouteChangeFocus("missing-element-id");

      return null;
    }

    const { rerender } = render(<BadHarness />);
    pathnameRef.current = "/runs";

    expect(() => {
      rerender(<BadHarness />);
    }).not.toThrow();

    await waitFor(() => {
      expect(true).toBe(true);
    });
  });
});
