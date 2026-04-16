import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it } from "vitest";

import { WelcomeBanner } from "./WelcomeBanner";

const STORAGE_KEY = "archlucid_welcome_dismissed";

afterEach(() => {
  localStorage.clear();
});

describe("WelcomeBanner — renders heading and CTAs", () => {
  it("shows welcome heading and both CTAs when not dismissed", async () => {
    render(<WelcomeBanner />);

    await waitFor(() => {
      expect(screen.getByRole("banner", { name: "Welcome" })).toBeInTheDocument();
    });

    expect(screen.getByRole("heading", { name: "Welcome to ArchLucid" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Create your first run" })).toHaveAttribute("href", "/runs/new");
    expect(screen.getByRole("link", { name: "Explore demo data" })).toHaveAttribute("href", "/runs?projectId=default");
  });
});

describe("WelcomeBanner — dismiss hides banner", () => {
  it("hides after dismiss click", async () => {
    render(<WelcomeBanner />);

    await waitFor(() => {
      expect(screen.getByRole("banner", { name: "Welcome" })).toBeInTheDocument();
    });

    fireEvent.click(screen.getByRole("button", { name: "Dismiss welcome banner" }));

    expect(screen.queryByRole("banner", { name: "Welcome" })).not.toBeInTheDocument();
    expect(localStorage.getItem(STORAGE_KEY)).toBe("1");
  });
});

describe("WelcomeBanner — localStorage respected on re-render", () => {
  it("stays hidden when dismissed flag is set", async () => {
    localStorage.setItem(STORAGE_KEY, "1");
    render(<WelcomeBanner />);

    await waitFor(() => {
      expect(screen.queryByRole("banner", { name: "Welcome" })).not.toBeInTheDocument();
    });
  });
});
