import { render } from "@testing-library/react";
import { axe, toHaveNoViolations } from "jest-axe";
import { describe, expect, it, vi } from "vitest";

import AskPage from "@/app/(operator)/ask/page";
import SearchPage from "@/app/(operator)/search/page";

vi.mock("@/lib/api", () => ({
  apiGet: vi.fn().mockResolvedValue([]),
}));

vi.mock("@/lib/conversation-api", () => ({
  askArchLucid: vi.fn(),
  getConversationMessages: vi.fn().mockResolvedValue([]),
  listConversationThreads: vi.fn().mockResolvedValue([]),
}));

expect.extend(toHaveNoViolations);

describe("search + ask operator pages — axe (Vitest)", () => {
  it(
    "SearchPage has no serious axe violations",
    async () => {
      const { container } = render(<SearchPage />);

      expect(await axe(container)).toHaveNoViolations();
    },
    20_000,
  );

  it(
    "AskPage has no serious axe violations",
    async () => {
      const { container } = render(<AskPage />);

      expect(await axe(container)).toHaveNoViolations();
    },
    20_000,
  );
});
