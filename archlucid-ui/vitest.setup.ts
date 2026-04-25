import { afterEach, vi } from "vitest";

import "@testing-library/jest-dom/vitest";

/** Radix Select uses pointer capture APIs not implemented in jsdom. */
if (typeof Element !== "undefined") {
  Element.prototype.hasPointerCapture = function () {
    return false;
  };
  Element.prototype.releasePointerCapture = function () {
    /* no-op */
  };
  Element.prototype.scrollIntoView = vi.fn();
}

afterEach(async () => {
  const { cleanup } = await import("@testing-library/react");
  cleanup();
});
