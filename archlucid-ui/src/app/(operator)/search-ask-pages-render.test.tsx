import { render, screen } from "@testing-library/react";
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

describe("SearchPage (operator shell)", () => {
  it("renders heading, query field, and search control", () => {
    render(<SearchPage />);

    expect(screen.getByRole("heading", { name: /semantic search/i })).toBeInTheDocument();
    expect(screen.getByLabelText(/^query$/i)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /^search$/i })).toBeInTheDocument();
  });
});

describe("AskPage (operator shell)", () => {
  it("renders heading, question field, and ask control", () => {
    render(<AskPage />);

    expect(screen.getByRole("heading", { name: /ask about a review/i })).toBeInTheDocument();
    expect(screen.getByLabelText(/^question$/i)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /^ask$/i })).toBeInTheDocument();
  });
});
