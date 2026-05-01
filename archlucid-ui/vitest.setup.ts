import { cleanup } from "@testing-library/react";
import { afterEach, vi } from "vitest";

import "@testing-library/jest-dom/vitest";

process.env.NEXT_PUBLIC_OPERATOR_NAV_SHOW_PRE_RELEASE_ROUTES = "1";

/** Keep unit tests on the non-demo path unless a test file explicitly stubs demo env (avoids hiding Operate controls). */
delete process.env.NEXT_PUBLIC_DEMO_MODE;
delete process.env.NEXT_PUBLIC_DEMO_STATIC_OPERATOR;

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

afterEach(() => {
  cleanup();
});
