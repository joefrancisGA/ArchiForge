import { describe, expect, it } from "vitest";

import { ApiRequestError, isApiRequestError } from "./api-request-error";

describe("ApiRequestError", () => {
  it("exposes problem, correlationId, and httpStatus", () => {
    const err = new ApiRequestError("msg", {
      problem: { title: "T", detail: "D" },
      correlationId: "cid-1",
      httpStatus: 404,
    });

    expect(err.message).toBe("msg");
    expect(err.name).toBe("ApiRequestError");
    expect(err.problem).toEqual({ title: "T", detail: "D" });
    expect(err.correlationId).toBe("cid-1");
    expect(err.httpStatus).toBe(404);
  });
});

describe("isApiRequestError", () => {
  it("narrows ApiRequestError instances", () => {
    const err = new ApiRequestError("x", {
      problem: null,
      correlationId: null,
      httpStatus: 500,
    });

    expect(isApiRequestError(err)).toBe(true);
  });

  it("returns false for plain Error", () => {
    expect(isApiRequestError(new Error("x"))).toBe(false);
  });

  it("returns false for non-errors", () => {
    expect(isApiRequestError(null)).toBe(false);
    expect(isApiRequestError("x")).toBe(false);
  });
});
