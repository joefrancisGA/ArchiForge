import { describe, expect, it } from "vitest";

import { tryParseApiProblemDetails } from "./api-problem";

describe("tryParseApiProblemDetails", () => {
  it("returns null for empty or whitespace body", () => {
    expect(tryParseApiProblemDetails("", "application/json")).toBeNull();
    expect(tryParseApiProblemDetails("   ", "application/problem+json")).toBeNull();
  });

  it("returns null when content-type is not JSON and body does not look like JSON", () => {
    expect(tryParseApiProblemDetails("plain error", "text/plain")).toBeNull();
  });

  it("parses JSON with empty content-type when body starts with {", () => {
    const problem = tryParseApiProblemDetails(
      JSON.stringify({ title: "Bad", detail: "Things broke" }),
      null,
    );

    expect(problem).toEqual({
      title: "Bad",
      detail: "Things broke",
    });
  });

  it("returns null for invalid JSON", () => {
    expect(tryParseApiProblemDetails("{not json", "application/json")).toBeNull();
  });

  it("returns null for JSON array", () => {
    expect(tryParseApiProblemDetails('["a"]', "application/json")).toBeNull();
  });

  it("returns null when no title, detail, type, or errorCode", () => {
    expect(
      tryParseApiProblemDetails(JSON.stringify({ status: 500, instance: "/x" }), "application/json"),
    ).toBeNull();
  });

  it("accepts problem with only type", () => {
    const problem = tryParseApiProblemDetails(
      JSON.stringify({ type: "https://example.com/probs/out" }),
      "application/problem+json",
    );

    expect(problem).toEqual({ type: "https://example.com/probs/out" });
  });

  it("reads errorCode and supportHint from root", () => {
    const problem = tryParseApiProblemDetails(
      JSON.stringify({
        title: "Not found",
        errorCode: "RUN_NOT_FOUND",
        supportHint: "Check run id",
      }),
      "application/json",
    );

    expect(problem).toMatchObject({
      title: "Not found",
      errorCode: "RUN_NOT_FOUND",
      supportHint: "Check run id",
    });
  });

  it("reads correlationId from root", () => {
    const problem = tryParseApiProblemDetails(
      JSON.stringify({
        title: "Error",
        detail: "x",
        correlationId: "cid-from-api",
      }),
      "application/problem+json",
    );

    expect(problem).toMatchObject({ correlationId: "cid-from-api" });
  });

  it("reads errorCode and supportHint from extensions object", () => {
    const problem = tryParseApiProblemDetails(
      JSON.stringify({
        title: "Error",
        extensions: { errorCode: "VALIDATION_FAILED", supportHint: "Fix fields" },
      }),
      "application/json",
    );

    expect(problem).toMatchObject({
      title: "Error",
      errorCode: "VALIDATION_FAILED",
      supportHint: "Fix fields",
    });
  });

  it("prefers root errorCode over extensions when both present", () => {
    const problem = tryParseApiProblemDetails(
      JSON.stringify({
        title: "T",
        errorCode: "ROOT",
        extensions: { errorCode: "EXT" },
      }),
      "application/json",
    );

    expect(problem?.errorCode).toBe("ROOT");
  });

  it("ignores extensions when it is not a plain object", () => {
    const problem = tryParseApiProblemDetails(
      JSON.stringify({ title: "T", extensions: [] }),
      "application/json",
    );

    expect(problem).toEqual({ title: "T" });
  });

  it("includes numeric status when finite", () => {
    const problem = tryParseApiProblemDetails(
      JSON.stringify({ title: "T", status: 422 }),
      "application/json",
    );

    expect(problem).toEqual({ title: "T", status: 422 });
  });

  it("omits non-finite status", () => {
    const problem = tryParseApiProblemDetails(
      JSON.stringify({ title: "T", status: Number.NaN }),
      "application/json",
    );

    expect(problem).toEqual({ title: "T" });
  });
});
