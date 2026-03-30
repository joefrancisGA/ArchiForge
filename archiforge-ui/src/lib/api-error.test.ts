import { describe, expect, it } from "vitest";

import { readApiFailureMessage } from "./api-error";

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
