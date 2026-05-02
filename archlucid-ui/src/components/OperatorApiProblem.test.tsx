import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

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

    expect(screen.getByText("Provide correlation ID")).toBeInTheDocument();
    expect(screen.getByText("abc-123")).toBeInTheDocument();
  });

  it("does not surface ERR reference text to the user (logged to console only)", () => {
    const spy = vi.spyOn(console, "info").mockImplementation(() => {});

    render(<OperatorApiProblem problem={null} fallbackMessage="Plain error" />);

    expect(screen.queryByText(/^Reference: ERR-/)).not.toBeInTheDocument();

    expect(spy).toHaveBeenCalled();

    spy.mockRestore();
  });

  it("renders rate limit copy when failure has httpStatus 429", () => {
    render(
      <OperatorApiProblem
        failure={{
          message: "Slow down",
          problem: { title: "Throttled", detail: "Too many concurrent requests" },
          correlationId: null,
          httpStatus: 429,
          retryAfterSeconds: 5,
        }}
      />,
    );

    expect(screen.getByText("Too many requests")).toBeInTheDocument();
    expect(screen.getByText("Too many concurrent requests")).toBeInTheDocument();
    expect(screen.getByText(/5 seconds/)).toBeInTheDocument();
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
