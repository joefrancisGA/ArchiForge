import { describe, expect, it } from "vitest";

import {
  buildApiRequestErrorFromParts,
  formatApiFailureMessage,
  readApiFailureMessage,
} from "./api-error";
import { CORRELATION_ID_HEADER } from "./correlation";
import { isApiRequestError } from "./api-request-error";

describe("formatApiFailureMessage", () => {
  it("joins title and detail when both present", () => {
    expect(
      formatApiFailureMessage(
        { title: "A", detail: "B" },
        400,
        "Bad Request",
        "",
      ),
    ).toBe("A: B");
  });

  it("falls back to status line when problem and body empty", () => {
    expect(formatApiFailureMessage(null, 502, "Bad Gateway", "")).toBe(
      "Request failed (502 Bad Gateway)",
    );
  });
});

describe("buildApiRequestErrorFromParts", () => {
  it("attaches correlation id from response header", () => {
    const bodyText = JSON.stringify({ title: "E", detail: "F" });
    const response = new Response(bodyText, {
      status: 500,
      headers: {
        "content-type": "application/problem+json",
        [CORRELATION_ID_HEADER]: "corr-xyz",
      },
    });

    const err = buildApiRequestErrorFromParts(response, bodyText);

    expect(isApiRequestError(err)).toBe(true);
    if (isApiRequestError(err)) {
      expect(err.correlationId).toBe("corr-xyz");
      expect(err.httpStatus).toBe(500);
      expect(err.problem?.title).toBe("E");
    }
  });

  it("falls back to correlationId in JSON body when header missing", () => {
    const bodyText = JSON.stringify({
      title: "Upstream API unreachable",
      status: 502,
      detail: "fetch failed",
      correlationId: "proxy-body-cid",
    });
    const response = new Response(bodyText, {
      status: 502,
      headers: { "content-type": "application/json" },
    });

    const err = buildApiRequestErrorFromParts(response, bodyText);

    expect(isApiRequestError(err)).toBe(true);
    if (isApiRequestError(err)) {
      expect(err.correlationId).toBe("proxy-body-cid");
    }
  });
});

describe("readApiFailureMessage", () => {
  it("prefers ProblemDetails title and detail", async () => {
    const response = new Response(
      JSON.stringify({ title: "Not Found", detail: "Run does not exist." }),
      {
        status: 404,
        headers: { "content-type": "application/problem+json" },
      },
    );

    await expect(readApiFailureMessage(response)).resolves.toBe(
      "Not Found: Run does not exist.",
    );
  });

  it("uses detail only when title missing", async () => {
    const response = new Response(JSON.stringify({ detail: "Validation failed." }), {
      status: 422,
      headers: { "content-type": "application/json" },
    });

    await expect(readApiFailureMessage(response)).resolves.toBe("Validation failed.");
  });

  it("falls back to status when body empty", async () => {
    const response = new Response(null, { status: 503, statusText: "Service Unavailable" });

    await expect(readApiFailureMessage(response)).resolves.toBe("Request failed (503 Service Unavailable)");
  });
});
