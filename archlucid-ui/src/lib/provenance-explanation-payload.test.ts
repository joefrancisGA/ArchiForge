import { describe, expect, it } from "vitest";

import { parseProvenanceExplanationPayload } from "./provenance-explanation-payload";

describe("parseProvenanceExplanationPayload", () => {
  it("parses coordinator 501 stub and RFC7807-compatible fields", () => {
    expect(
      parseProvenanceExplanationPayload({
        message: "Explanation feature pending",
        detail: "",
        title: "",
      }),
    ).toEqual({
      message: "Explanation feature pending",
      detail: "",
      title: "",
    });

    expect(
      parseProvenanceExplanationPayload({ detail: "x", title: "y" }),
    ).toEqual({ message: null, detail: "x", title: "y" });
  });

  it("returns nulls for non-objects", () => {
    expect(parseProvenanceExplanationPayload(null)).toEqual({
      message: null,
      detail: null,
      title: null,
    });

    expect(parseProvenanceExplanationPayload("x")).toEqual({
      message: null,
      detail: null,
      title: null,
    });
  });
});
