import { describe, expect, it } from "vitest";

import type { ApiProblemDetails } from "./api-problem";
import { operatorCopyForProblem } from "./api-problem-copy";

describe("operatorCopyForProblem", () => {
  it("uses fallback when problem is null", () => {
    const copy = operatorCopyForProblem(null, "Network down");

    expect(copy).toEqual({
      heading: "Request failed",
      body: "Network down",
    });
  });

  it("trims fallback and uses default when empty", () => {
    const copy = operatorCopyForProblem(null, "   ");

    expect(copy.body).toBe("Request failed.");
  });

  it("maps known errorCode to heading", () => {
    const problem: ApiProblemDetails = {
      errorCode: "RUN_NOT_FOUND",
      title: "Not Found",
      detail: "No such run",
    };

    const copy = operatorCopyForProblem(problem, "fallback");

    expect(copy.heading).toBe("Run not found");
    expect(copy.body).toBe("No such run");
  });

  it("uses title as heading when errorCode unknown", () => {
    const problem: ApiProblemDetails = { title: "Custom", detail: "D" };

    expect(operatorCopyForProblem(problem, "f")).toMatchObject({
      heading: "Custom",
      body: "D",
    });
  });

  it("includes supportHint when present", () => {
    const problem: ApiProblemDetails = {
      title: "T",
      detail: "D",
      supportHint: "Try again later",
    };

    expect(operatorCopyForProblem(problem, "f")).toEqual({
      heading: "T",
      body: "D",
      hint: "Try again later",
    });
  });

  it("uses title as body when detail missing", () => {
    const problem: ApiProblemDetails = { title: "Only title" };

    expect(operatorCopyForProblem(problem, "fallback")).toMatchObject({
      body: "Only title",
    });
  });

  it("falls back to fallbackMessage when problem lacks title and detail", () => {
    const problem: ApiProblemDetails = { errorCode: "INTERNAL_ERROR" };

    const copy = operatorCopyForProblem(problem, "Use this");

    expect(copy.body).toBe("Use this");
  });
});
