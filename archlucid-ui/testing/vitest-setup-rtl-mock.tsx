import * as React from "react";
import { vi } from "vitest";

import { TooltipProvider } from "@/components/ui/tooltip";

/**
 * GlossaryTooltip and other Radix tooltips require a provider. Wrapping every test `render()` avoids
 * repeating TooltipProvider in each suite (see operate-authority-ui-shaping, wizard tests, render gate).
 * Loaded before `vitest.setup.ts` so the setup file never caches the unmocked `@testing-library/react` module.
 *
 * Lives under `testing/` so `next build` does not typecheck this file (see root `tsconfig.json` exclude).
 */
vi.mock("@testing-library/react", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@testing-library/react")>();

  return {
    ...actual,
    render: (ui: Parameters<typeof actual.render>[0], options?: Parameters<typeof actual.render>[1]) => {
      const { wrapper: W, ...rest } = options ?? {};

      const Wrapper = ({ children }: { children: React.ReactNode }) => (
        <TooltipProvider delayDuration={0}>
          {W ? React.createElement(W as React.ComponentType<{ children: React.ReactNode }>, null, children) : children}
        </TooltipProvider>
      );

      return actual.render(ui, { ...rest, wrapper: Wrapper });
    },
  };
});
