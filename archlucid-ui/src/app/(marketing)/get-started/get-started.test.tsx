import { readFileSync, readdirSync, statSync } from "node:fs";
import { join, resolve } from "node:path";

import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import GetStartedPage from "./page";
import { BUYER_GET_STARTED_VERTICAL_SLUGS } from "./get-started-verticals";

function readBriefSlugs(): readonly string[] {
  const briefsRoot = resolve(__dirname, "../../../../../templates/briefs");
  return readdirSync(briefsRoot)
    .filter((entry) => statSync(join(briefsRoot, entry)).isDirectory())
    .sort();
}

describe("BUYER_GET_STARTED_VERTICAL_SLUGS", () => {
  it("matches the on-disk templates/briefs/ folder slugs exactly", () => {
    const onDisk = readBriefSlugs();
    const rendered = [...BUYER_GET_STARTED_VERTICAL_SLUGS].sort();

    expect(rendered).toEqual(onDisk);
  });
});

describe("GetStartedPage", () => {
  it("renders all five steps with placeholder image slots", () => {
    render(<GetStartedPage />);

    for (let n = 1; n <= 5; n++) {
      expect(screen.getByTestId(`get-started-step-${n}`)).toBeInTheDocument();
      const img = screen.getByTestId(`get-started-step-${n}-image`);
      expect(img.getAttribute("src") ?? "").toMatch(new RegExp(`step-${n}-placeholder\\.png`));
    }
  });

  it("renders the vertical picker with one button per templates/briefs slug", () => {
    render(<GetStartedPage />);

    const picker = screen.getByTestId("get-started-vertical-picker");
    expect(picker).toBeInTheDocument();

    for (const slug of BUYER_GET_STARTED_VERTICAL_SLUGS) {
      const button = screen.getByTestId(`get-started-vertical-${slug}`);
      expect(button).toHaveAttribute("data-vertical-slug", slug);
    }
  });

  it("does not render a 'talk to a human' CTA (Q5 — V1.1 deferred)", () => {
    render(<GetStartedPage />);

    const all = document.body.textContent ?? "";
    expect(/talk to a human/i.test(all)).toBe(false);
  });
});

describe("docs/BUYER_FIRST_30_MINUTES.md", () => {
  it("ships with q35 placeholder markers on owner-blocked sentences", () => {
    const repoRoot = resolve(__dirname, "../../../../..");
    const docPath = join(repoRoot, "docs", "BUYER_FIRST_30_MINUTES.md");
    const text = readFileSync(docPath, "utf-8");

    expect(text).toContain("<<placeholder copy — replace before external use>>");
    expect(text).toContain("Audience banner:");
    expect(text).toContain("docs/engineering/FIRST_30_MINUTES.md");
  });
});
