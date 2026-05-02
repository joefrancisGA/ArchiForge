import { describe, expect, it } from "vitest";

import { ApiRequestError } from "./api-request-error";
import { isApiNotFoundFailure, toApiLoadFailure, uiFailureFromMessage } from "./api-load-failure";

describe("toApiLoadFailure", () => {
  it("maps ApiRequestError to state", () => {
    const err = new ApiRequestError("m", {
      problem: { title: "T" },
      correlationId: "c",
      httpStatus: 400,
    });

    expect(toApiLoadFailure(err)).toEqual({
      message: "m",
      problem: { title: "T" },
      correlationId: "c",
      httpStatus: 400,
      retryAfterSeconds: null,
    });
  });

  it("maps retryAfterSeconds from ApiRequestError when present", () => {
    const err = new ApiRequestError("rate", {
      problem: null,
      correlationId: null,
      httpStatus: 429,
      retryAfterSeconds: 12,
    });

    expect(toApiLoadFailure(err)).toEqual({
      message: "rate",
      problem: null,
      correlationId: null,
      httpStatus: 429,
      retryAfterSeconds: 12,
    });
  });

  it("maps generic Error to message-only state", () => {
    expect(toApiLoadFailure(new Error("oops"))).toEqual({
      message: "oops",
      problem: null,
      correlationId: null,
      httpStatus: null,
      retryAfterSeconds: null,
    });
  });

  it("maps unknown to generic message", () => {
    expect(toApiLoadFailure(42)).toEqual({
      message: "An unexpected error occurred.",
      problem: null,
      correlationId: null,
      httpStatus: null,
      retryAfterSeconds: null,
    });
  });
});

describe("isApiNotFoundFailure", () => {
  it("is true for 404 httpStatus", () => {
    expect(
      isApiNotFoundFailure({
        message: "m",
        problem: null,
        correlationId: null,
        httpStatus: 404,
        retryAfterSeconds: null,
      }),
    ).toBe(true);
  });

  it("is true for problem.status 404", () => {
    expect(
      isApiNotFoundFailure({
        message: "m",
        problem: { title: "Missing", status: 404 },
        correlationId: null,
        httpStatus: null,
        retryAfterSeconds: null,
      }),
    ).toBe(true);
  });

  it("is false for other errors", () => {
    expect(
      isApiNotFoundFailure({
        message: "m",
        problem: { title: "Bad", status: 400 },
        correlationId: null,
        httpStatus: 400,
        retryAfterSeconds: null,
      }),
    ).toBe(false);
    expect(isApiNotFoundFailure(null)).toBe(false);
  });
});

describe("uiFailureFromMessage", () => {
  it("trims message", () => {
    expect(uiFailureFromMessage("  hello  ")).toMatchObject({
      message: "hello",
      problem: null,
      correlationId: null,
      httpStatus: null,
      retryAfterSeconds: null,
    });
  });

  it("uses default when message empty after trim", () => {
    expect(uiFailureFromMessage("")).toMatchObject({
      message: "Something went wrong.",
    });
  });
});
