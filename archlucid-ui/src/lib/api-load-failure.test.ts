import { describe, expect, it } from "vitest";

import { ApiRequestError } from "./api-request-error";
import { toApiLoadFailure, uiFailureFromMessage } from "./api-load-failure";

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
    });
  });

  it("maps generic Error to message-only state", () => {
    expect(toApiLoadFailure(new Error("oops"))).toEqual({
      message: "oops",
      problem: null,
      correlationId: null,
    });
  });

  it("maps unknown to generic message", () => {
    expect(toApiLoadFailure(42)).toEqual({
      message: "An unexpected error occurred.",
      problem: null,
      correlationId: null,
    });
  });
});

describe("uiFailureFromMessage", () => {
  it("trims message", () => {
    expect(uiFailureFromMessage("  hello  ")).toMatchObject({
      message: "hello",
      problem: null,
      correlationId: null,
    });
  });

  it("uses default when message empty after trim", () => {
    expect(uiFailureFromMessage("")).toMatchObject({
      message: "Something went wrong.",
    });
  });
});
