import * as React from "react";
import { vi } from "vitest";

/**
 * GlossaryTooltip and other Radix tooltips require a provider. Wrapping every test `render()` avoids
 * repeating TooltipProvider in each suite (see operate-authority-ui-shaping, wizard tests, render gate).
 * Loaded before `vitest.setup.ts` so the setup file never caches the unmocked `@testing-library/react` module.
 */
vi.mock("@testing-library/react", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@testing-library/react")>();
  const { TooltipProvider } = await import("@/components/ui/tooltip");

  return {
    ...actual,
    render: (ui: Parameters<typeof actual.render>[0], options?: Parameters<typeof actual.render>[1]) => {
      const { wrapper: W, ...rest } = options ?? {};
      const Wrapper = ({ children }: { children: React.ReactNode }) =>
        React.createElement(
          TooltipProvider,
          { delayDuration: 0 },
          W ? React.createElement(W as React.ComponentType<{ children: React.ReactNode }>, null, children) : children,
        );
      return actual.render(ui, { ...rest, wrapper: Wrapper });
    },
  };
});
