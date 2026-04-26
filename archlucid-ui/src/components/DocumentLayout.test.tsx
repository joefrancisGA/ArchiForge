import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { DocumentLayout } from "./DocumentLayout";

describe("DocumentLayout", () => {
  it("renders children inside a max-width article", () => {
    render(
      <DocumentLayout>
        <p>Hello</p>
      </DocumentLayout>,
    );
    const article = screen.getByTestId("document-layout-article");

    expect(article).toHaveClass("max-w-3xl");
    expect(article).toHaveTextContent("Hello");
  });

  it("renders TOC when three or more section links are provided", () => {
    render(
      <DocumentLayout
        tocItems={[
          { id: "a", label: "Alpha" },
          { id: "b", label: "Beta" },
          { id: "c", label: "Gamma" },
        ]}
      >
        <p>Body</p>
      </DocumentLayout>,
    );

    expect(screen.getByTestId("document-layout-toc")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Beta" })).toHaveAttribute("href", "#b");
  });

  it("omits TOC when fewer than three sections", () => {
    render(
      <DocumentLayout tocItems={[{ id: "x", label: "One" }]}>
        <p>Only</p>
      </DocumentLayout>,
    );

    expect(screen.queryByTestId("document-layout-toc")).toBeNull();
  });

  it("includes print-friendly width utilities on the article", () => {
    render(
      <DocumentLayout>
        <p>x</p>
      </DocumentLayout>,
    );

    expect(screen.getByTestId("document-layout-article").className).toMatch(/print:max-w-none/);
  });
});
