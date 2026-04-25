import { afterEach, describe, expect, it, vi } from "vitest";

import { DEFAULT_GITHUB_BLOB_BASE } from "./docs-public-base";
import { getDocHref } from "./help-topics";

describe("getDocHref", () => {
  afterEach(() => {
    vi.unstubAllEnvs();
  });

  it("returns a full URL when NEXT_PUBLIC_DOCS_BASE_URL is set", () => {
    vi.stubEnv("NEXT_PUBLIC_DOCS_BASE_URL", "https://docs.example.com/archlucid");

    expect(getDocHref("docs/FIRST_RUN_WIZARD.md")).toBe(
      "https://docs.example.com/archlucid/docs/FIRST_RUN_WIZARD.md",
    );
  });

  it("returns the public GitHub blob URL when NEXT_PUBLIC_DOCS_BASE_URL is unset", () => {
    vi.stubEnv("NEXT_PUBLIC_DOCS_BASE_URL", undefined);

    expect(getDocHref("docs/ALERTS.md")).toBe(
      `${DEFAULT_GITHUB_BLOB_BASE.replace(/\/$/, "")}/docs/ALERTS.md`,
    );
  });

  it("returns the public GitHub blob URL when NEXT_PUBLIC_DOCS_BASE_URL is empty", () => {
    vi.stubEnv("NEXT_PUBLIC_DOCS_BASE_URL", "");

    expect(getDocHref("docs/ALERTS.md")).toBe(
      `${DEFAULT_GITHUB_BLOB_BASE.replace(/\/$/, "")}/docs/ALERTS.md`,
    );
  });

  it("strips trailing slash from the base and leading slash from the path", () => {
    vi.stubEnv("NEXT_PUBLIC_DOCS_BASE_URL", "https://docs.example.com/root/");

    expect(getDocHref("/docs/X.md")).toBe("https://docs.example.com/root/docs/X.md");
  });

  it("returns null when docPath is empty or whitespace", () => {
    vi.stubEnv("NEXT_PUBLIC_DOCS_BASE_URL", undefined);

    expect(getDocHref("")).toBeNull();
    expect(getDocHref("   ")).toBeNull();
  });
});
