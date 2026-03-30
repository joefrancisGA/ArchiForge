/**
 * Maps failed HTTP responses to a single operator-facing string.
 * Prefers ASP.NET Core ProblemDetails (`title` / `detail`) when JSON is returned.
 */
export async function readApiFailureMessage(response: Response): Promise<string> {
  const statusLine = `${response.status} ${response.statusText}`.trim();

  let text: string;

  try {
    text = await response.text();
  } catch {
    return `Request failed (${statusLine})`;
  }

  if (!text) {
    return `Request failed (${statusLine})`;
  }

  const contentType = response.headers.get("content-type") ?? "";
  const looksJson =
    contentType.includes("application/json") ||
    contentType.includes("application/problem+json");

  if (looksJson) {
    try {
      const body = JSON.parse(text) as Record<string, unknown>;
      const title = typeof body.title === "string" ? body.title.trim() : "";
      const detail = typeof body.detail === "string" ? body.detail.trim() : "";

      if (title && detail) {
        return `${title}: ${detail}`;
      }

      if (detail) {
        return detail;
      }

      if (title) {
        return title;
      }

      const error = typeof body.error === "string" ? body.error.trim() : "";

      if (error) {
        return error;
      }
    } catch {
      /* fall through */
    }
  }

  return `Request failed (${statusLine})`;
}
