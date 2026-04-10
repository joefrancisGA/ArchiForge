import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { OperatorApiProblem } from "./OperatorApiProblem";

describe("OperatorApiProblem", () => {
  it("renders heading and body from problem", () => {
    render(
      <OperatorApiProblem
        problem={{ title: "Not found", detail: "Missing resource." }}
        fallbackMessage="fallback"
      />,
    );

    expect(screen.getByText("Not found")).toBeInTheDocument();
    expect(screen.getByText("Missing resource.")).toBeInTheDocument();
  });

  it("renders support hint when present", () => {
    render(
      <OperatorApiProblem
        problem={{ title: "T", detail: "D", supportHint: "Contact support with the reference below." }}
        fallbackMessage="f"
      />,
    );

    expect(screen.getByText("Contact support with the reference below.")).toBeInTheDocument();
  });

  it("shows correlation id line when provided", () => {
    render(
      <OperatorApiProblem problem={null} fallbackMessage="Plain error" correlationId="abc-123" />,
    );

    expect(
      screen.getByText(/Reference \(correlation ID — use with API logs and support bundle\): abc-123/),
    ).toBeInTheDocument();
  });

  it("uses warning callout when variant is warning", () => {
    render(
      <OperatorApiProblem
        problem={{ title: "Secondary", detail: "Soft failure" }}
        fallbackMessage="f"
        variant="warning"
      />,
    );

    const status = screen.getByRole("status");
    expect(status).toHaveTextContent("Secondary");
    expect(status).toHaveTextContent("Soft failure");
  });

  it("uses error callout by default", () => {
    render(
      <OperatorApiProblem problem={{ title: "Err", detail: "Bad" }} fallbackMessage="f" />,
    );

    expect(screen.getByRole("alert")).toHaveTextContent("Err");
  });
});
